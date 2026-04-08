using Unity.Netcode;
using UnityEngine;

public class PlayerAbility : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject ballPrefab;
    public GameObject wallPrefab;

    [Header("Cooldowns (in Sekunden)")]
    public float ballCooldown = 0.5f;
    public float wallCooldown = 3.0f;

    [Header("Settings")]
    public float throwForce = 30f;
    public float wallDistance = 4f;
    public float wallDuration = 5f;

    private float nextBallTime = 0f;
    private float nextWallTime = 0f;
    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!IsOwner || playerController == null) return;

        // --- LINKSSKLICK: BALL ---
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time >= nextBallTime)
            {
                ShootBall();
                nextBallTime = Time.time + ballCooldown;
            }
            else
            {
                float remaining = nextBallTime - Time.time;
                Debug.Log($"Ball Cooldown: {remaining:F1}s verbleibend");
            }
        }

        // --- RECHTSKLICK: WAND ---
        if (Input.GetMouseButtonDown(1))
        {
            if (Time.time >= nextWallTime)
            {
                PlaceWall();
                nextWallTime = Time.time + wallCooldown;
            }
            else
            {
                float remaining = nextWallTime - Time.time;
                Debug.Log($"Wand Cooldown: {remaining:F1}s verbleibend");
            }
        }
    }

    private void ShootBall()
    {
        Vector3 spawnPos = playerController.playerCamera.transform.position + (playerController.playerCamera.transform.forward * 1.5f);
        Vector3 shootDir = playerController.playerCamera.transform.forward;
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
    void ThrowBallServerRpc(Vector3 pos, Vector3 direction, Color playerColor)
    {
        GameObject ball = Instantiate(ballPrefab, pos, Quaternion.identity);
        ball.GetComponent<MeshRenderer>().material.color = playerColor;
        ball.GetComponent<NetworkObject>().Spawn();

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(direction * throwForce, ForceMode.Impulse);

        Destroy(ball, 5f);
    }

    [ServerRpc]
    void PlaceWallServerRpc(Vector3 pos, Quaternion rot, Color playerColor)
    {
        GameObject wall = Instantiate(wallPrefab, pos, rot);
        wall.GetComponent<MeshRenderer>().material.color = playerColor;
        wall.GetComponent<NetworkObject>().Spawn();

        Destroy(wall, wallDuration);
    }
}