using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerAbility : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject ballPrefab;
    public GameObject wallPrefab;

    [Header("Cooldowns (in Sekunden)")]
    public float ballCooldown = 0.5f;
    public float wallCooldown = 3.0f;
    public CooldownBar ballCooldownBar;
    public CooldownBar wallCooldownBar;

    [Header("Settings")]
    public float throwForce = 30f;
    public float wallDistance = 4f;
    public float wallDuration = 5f;

    private float nextBallTime = 0f;
    private float nextWallTime = 0f;
    private PlayerController playerController;

    private Dictionary<ulong, float> lastServerBallTime = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> lastServerWallTime = new Dictionary<ulong, float>();

    [Header("UI Setup")]
    public GameObject playerHUD; // Ziehe hier das Parent-Objekt (PlayerHUD_Canvas) rein

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // NUR für mich: HUD aktivieren und Werte setzen
            if (playerHUD != null) playerHUD.SetActive(true);

            if (ballCooldownBar != null) ballCooldownBar.SetMaxCoolDown(ballCooldown);
            if (wallCooldownBar != null) wallCooldownBar.SetMaxCoolDown(wallCooldown);
        }
        else
        {
            // FÜR ALLE ANDEREN: HUD komplett unsichtbar machen
            if (playerHUD != null) playerHUD.SetActive(false);

            // Dieses Script auf fremden Playern deaktivieren, damit es keine UI-Werte sendet
            this.enabled = false;
        }
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        ballCooldownBar.SetMaxCoolDown(ballCooldown);
        wallCooldownBar.SetMaxCoolDown(wallCooldown);
    }

    void Update()
    {
        if (!IsOwner || playerController == null || PlayerController.IsGamePaused) return;


        // LOKALER CHECK: Für direktes Feedback (damit der Client nicht unnötig RPCs schickt)
        if (Input.GetMouseButtonDown(0) && Time.time >= nextBallTime)
        {
            ShootBall();
            nextBallTime = Time.time + ballCooldown;
        }

        if (Input.GetMouseButtonDown(1) && Time.time >= nextWallTime)
        {
            PlaceWall();
            nextWallTime = Time.time + wallCooldown;
        }

        // --- DEIN WUNSCH (KORRIGIERT) ---
        // Berechnung: Zielzeit in der Zukunft MINUS aktuelle Zeit
        float currentWallCooldown = Mathf.Max(0, nextWallTime - Time.time);
        float currentBallCooldown = Mathf.Max(0, nextBallTime - Time.time);

        // Jetzt kannst du die Werte an deine CooldownBars übergeben:
        if (wallCooldownBar != null) ballCooldownBar.SetCooldown(currentBallCooldown);
        if (wallCooldownBar != null) wallCooldownBar.SetCooldown(currentWallCooldown);

        // Debug Log (zeigt jetzt z.B. 3.0 -> 0.0 an)
        Debug.Log($"Wand Cooldown: {currentWallCooldown:F1}s");
    }

    private void ShootBall()
    {
        // 1. Startpunkt: Fest am Spieler (z.B. Brusthöhe), nicht an der Kamera
        // Wir nehmen die Spielerposition und gehen 1.5m hoch
        Vector3 playerSpinePos = transform.position + Vector3.up;

        // 2. Richtung: Aber wir schießen dorthin, wo die Kamera hinschaut
        Vector3 shootDir = playerController.playerCamera.transform.forward;

        // 3. Offset: Damit wir uns nicht selbst treffen, schieben wir den Startpunkt 1m vor den Spieler
        Vector3 spawnPos = playerSpinePos + (transform.forward * 1.0f);

        ThrowBallServerRpc(spawnPos, shootDir, GetComponent<MeshRenderer>().material.color);
    }

    private void PlaceWall()
    {
        Vector3 spawnPos = transform.position + (transform.forward * wallDistance);
        // "Look at Player" Logik für richtige Ausrichtung
        Vector3 directionToPlayer = transform.position - spawnPos;
        directionToPlayer.y = 0;
        Quaternion wallRotation = Quaternion.LookRotation(directionToPlayer);

        PlaceWallServerRpc(spawnPos, wallRotation, GetComponent<MeshRenderer>().material.color);
    }

    [ServerRpc]
    void ThrowBallServerRpc(Vector3 pos, Vector3 direction, Color playerColor, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Sicherheits-Check: Falls der Client noch nicht im Dictionary ist
        if (!lastServerBallTime.ContainsKey(clientId)) lastServerBallTime[clientId] = 0f;

        // Prüfung gegen die Netzwerk-Zeit des Servers
        if (NetworkManager.Singleton.ServerTime.Time >= lastServerBallTime[clientId])
        {
            // Cooldown für diesen Client auf dem Server setzen
            lastServerBallTime[clientId] = (float)NetworkManager.Singleton.ServerTime.Time + ballCooldown;

            // --- Ball Instanziieren ---
            GameObject ball = Instantiate(ballPrefab, pos, Quaternion.identity);
            BallProjectile projectile = ball.GetComponent<BallProjectile>();
            projectile.shooterId = clientId;

            NetworkObject netObj = ball.GetComponent<NetworkObject>();
            netObj.Spawn();

            projectile.ballColor.Value = playerColor;

            // Physik-Berechnung
            Vector3 targetVelocity = direction * throwForce;

            // 1. Physik auf dem Server
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = targetVelocity;
            }

            // 2. Physik auf Clients synchronisieren
            projectile.FireBallClientRpc(targetVelocity);

            Destroy(ball, 5f);
        }
        else
        {
            Debug.LogWarning($"Server: Ball-Spam geblockt für Client {clientId}");
        }
    }

    [ServerRpc]
    void PlaceWallServerRpc(Vector3 pos, Quaternion rot, Color playerColor, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!lastServerWallTime.ContainsKey(clientId)) lastServerWallTime[clientId] = 0f;

        if (NetworkManager.Singleton.ServerTime.Time >= lastServerWallTime[clientId])
        {
            lastServerWallTime[clientId] = (float)NetworkManager.Singleton.ServerTime.Time + wallCooldown;

            // --- Wand Instanziieren ---
            GameObject wall = Instantiate(wallPrefab, pos, rot);

            // WICHTIG: Erst spawnen, dann Variable setzen
            wall.GetComponent<NetworkObject>().Spawn();

            // Hier greifen wir auf das neue WallScript zu
            if (wall.TryGetComponent<WallScript>(out var wallScript))
            {
                wallScript.wallColor.Value = playerColor;
            }

            Destroy(wall, wallDuration);
        }
    }
}