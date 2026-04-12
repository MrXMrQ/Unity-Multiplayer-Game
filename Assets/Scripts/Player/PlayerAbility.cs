using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerAbility : NetworkBehaviour
{
    [Header("Settings")]
    public float shootRange = 100f;
    public int damageValue = 20;
    public float fireRate = 10f; // shoots per seconds
    public float tracerSpeed = 200f;
    private float lastShootTime = 0f;

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
        if (Input.GetMouseButton(0))
        {
            if (Time.time >= lastShootTime + 1f / fireRate)
            {
                lastShootTime = Time.time;

                Vector3 target = GetTargetPoint();
                SpawnTracer(target);

                RequestShootServerRpc(
                    playerController.playerCamera.transform.position,
                    playerController.playerCamera.transform.forward
                );
            }
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
        if (Input.GetKeyDown(KeyCode.R))
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
            Vector3 direction = (targetPoint - shootPoint.position).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Quaternion rotationCorrection = Quaternion.Euler(90, 0, 0);
            Quaternion finalRotation = lookRotation * rotationCorrection;

            GameObject tracer = Instantiate(tracerPrefab, shootPoint.position, finalRotation);

            StartCoroutine(MoveTracer(tracer, targetPoint));
        }
    }

    private IEnumerator MoveTracer(GameObject tracer, Vector3 target)
    {
        Vector3 startPos = tracer.transform.position;
        float distance = Vector3.Distance(startPos, target);
        float travelTime = distance / tracerSpeed;
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
    void RequestShootServerRpc(Vector3 camPos, Vector3 camForward, ServerRpcParams rpcParams = default)
    {
        Ray ray = new Ray(camPos, camForward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * shootRange, Color.red, debugRayDuration);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            if (hit.collider.TryGetComponent<PlayerHealth>(out var health))
            {
                health.TakeDamage(damageValue);
            }

            SpawnHitEffectClientRpc(hit.point, hit.normal);
            SpawnTracerClientRpc(hit.point, rpcParams.Receive.SenderClientId);
        }
        else
        {
            SpawnTracerClientRpc(ray.GetPoint(shootRange), rpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    void SpawnHitEffectClientRpc(Vector3 point, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, point, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
    }

    [ClientRpc]
    void SpawnTracerClientRpc(Vector3 targetPoint, ulong shooterId)
    {
        if (NetworkManager.Singleton.LocalClientId != shooterId)
        {
            SpawnTracer(targetPoint);
        }
    }
}