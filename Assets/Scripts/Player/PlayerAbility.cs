using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerAbility : NetworkBehaviour
{
    [Header("Settings")]
    public float shootRange = 100f;
    public int damageValue = 20;
    public float fireRate = 0.4f;
    private float nextFireTime = 0f;

    [Header("Visuals")]
    public GameObject hitEffectPrefab;
    public GameObject tracerPrefab;
    public Transform shootPoint;

    [Header("Debug")]
    public bool showDebugRay = true;
    public Color debugRayColor = Color.green;
    public float debugRayDuration = 0.5f;

    [Header("Player HUD")]
    public GameObject playerHUD;

    private PlayerController playerController;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<PlayerController>();
        if (IsOwner)
        {
            if (playerHUD != null) playerHUD.SetActive(true);
        }
        else
        {
            if (playerHUD != null) playerHUD.SetActive(false);
            this.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner || PlayerController.IsGamePaused) return;

        // shoot
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            SpawnTracer(GetTargetPoint());
            RequestShootServerRpc();
        }

        // first ability
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("First ability");
        }

        // second ability
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("second ability");
        }

        // third ability
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("ultimate ability");
        }
    }

    private Vector3 GetTargetPoint()
    {
        Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            return hit.point;
        }
        return ray.GetPoint(shootRange);
    }

    private void SpawnTracer(Vector3 targetPoint)
    {
        if (tracerPrefab != null && shootPoint != null)
        {
            GameObject tracer = Instantiate(tracerPrefab, shootPoint.position, Quaternion.identity);
            StartCoroutine(MoveTracer(tracer, targetPoint));
        }
    }

    private IEnumerator MoveTracer(GameObject tracer, Vector3 target)
    {
        float speed = 200f; // Sehr hohe Geschwindigkeit für den Tracer-Effekt
        Vector3 startPos = tracer.transform.position;
        float distance = Vector3.Distance(startPos, target);
        float travelTime = distance / speed;
        float elapsed = 0;

        while (elapsed < travelTime)
        {
            if (tracer == null) yield break;
            tracer.transform.position = Vector3.Lerp(startPos, target, elapsed / travelTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(tracer);
    }

    [ServerRpc]
    void RequestShootServerRpc(ServerRpcParams rpcParams = default)
    {
        Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * shootRange, Color.red, debugRayDuration);

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            if (hit.collider.TryGetComponent<PlayerHealth>(out var health))
            {
                health.TakeDamage(damageValue);
            }

            // Hit Effect für alle (Einschlag)
            SpawnHitEffectClientRpc(hit.point, hit.normal);

            // Tracer für alle anderen Spieler (damit sie sehen, wer schießt)
            SpawnTracerClientRpc(hit.point, rpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    void SpawnHitEffectClientRpc(Vector3 point, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, point, Quaternion.LookRotation(normal));
        }
    }

    [ClientRpc]
    void SpawnTracerClientRpc(Vector3 targetPoint, ulong shooterId)
    {
        // Der Schütze hat seinen Tracer bereits lokal gespawnt (für 0 Latenz)
        if (NetworkManager.Singleton.LocalClientId != shooterId)
        {
            SpawnTracer(targetPoint);
        }
    }
}