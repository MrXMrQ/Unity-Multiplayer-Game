using Unity.Netcode;
using UnityEngine;
using System.Collections;
using TMPro;

public class Shooting : NetworkBehaviour
{
    [Header("Settings")]
    public WeaponData activeWeapon;
    public Transform weaponSpawnPoint;
    private float lastShootTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private Transform shootPoint;

    [Header("Debug")]
    public bool showDebugRay = true;
    public float debugRayDuration = 2f;

    [Header("Player HUD")]
    public GameObject playerHUD;
    private PlayerController playerController;
    public TextMeshProUGUI currentAmmoText;
    public TextMeshProUGUI magazineSizeText;
    public Bar reloadCooldown;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<PlayerController>();

        if (activeWeapon != null && weaponSpawnPoint != null)
        {
            currentAmmo = activeWeapon.magazineSize;
            currentAmmoText.text = $"{currentAmmo}";
            magazineSizeText.text = $"{activeWeapon.magazineSize}";

            GameObject spawnedWeapon = Instantiate(activeWeapon.weaponPrefab, weaponSpawnPoint);

            spawnedWeapon.transform.localPosition = Vector3.zero;
            spawnedWeapon.transform.localRotation = Quaternion.identity;

            shootPoint = spawnedWeapon.transform.Find("shootPoint");
        }

        if (IsOwner)
        {
            if (playerHUD != null) playerHUD.SetActive(true);
        }
        else
        {
            if (playerHUD != null) playerHUD.SetActive(false);
            this.enabled = false;
        }

        if (IsOwner && reloadCooldown != null)
        {
            reloadCooldown.SetMaxValue(100);
            reloadCooldown.SetValue(0);
        }
    }

    void Update()
    {
        if (!IsOwner || PlayerController.IsGamePaused) return;

        // reload
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < activeWeapon.magazineSize && !isReloading)
        {
            StartCoroutine(Reload());
        }

        // shoot
        if (Input.GetMouseButton(0))
        {
            if (Time.time >= lastShootTime + 1f / activeWeapon.fireRate && !isReloading)
            {
                if (currentAmmo > 0)
                {
                    Shoot();
                }
            }
        }
    }

    private void Shoot()
    {
        lastShootTime = Time.time;
        currentAmmo--;
        currentAmmoText.text = $"{currentAmmo}";

        Vector3 target = GetTargetPoint();
        SpawnTracer(target);

        RequestShootServerRpc(
            playerController.playerCamera.transform.position,
            playerController.playerCamera.transform.forward
        );
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        float duration = activeWeapon.reloadSpeed;
        float elapsed = 0f;

        if (reloadCooldown != null) reloadCooldown.SetValue(0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (reloadCooldown != null)
            {
                float progress = elapsed / duration * 100f;
                reloadCooldown.SetValue((int)progress);
            }

            yield return null;
        }

        // Werte finalisieren
        currentAmmo = activeWeapon.magazineSize;
        currentAmmoText.text = $"{currentAmmo}";

        if (reloadCooldown != null) reloadCooldown.SetValue(100);

        isReloading = false;
        reloadCooldown.SetValue(0);
    }

    private Vector3 GetTargetPoint()
    {
        Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, activeWeapon.shootRange))
        {
            return hit.point;
        }
        return ray.GetPoint(activeWeapon.shootRange);
    }

    private void SpawnTracer(Vector3 targetPoint)
    {
        if (activeWeapon.tracerPrefab != null && shootPoint != null)
        {
            Vector3 direction = (targetPoint - shootPoint.position).normalized;
            Quaternion finalRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);

            GameObject tracer = Instantiate(activeWeapon.tracerPrefab, shootPoint.position, finalRotation);
            StartCoroutine(MoveTracer(tracer, targetPoint));
        }
    }

    private IEnumerator MoveTracer(GameObject tracer, Vector3 target)
    {
        Vector3 startPos = tracer.transform.position;
        float distance = Vector3.Distance(startPos, target);
        float travelTime = distance / activeWeapon.tracerSpeed;
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
        ulong shooterId = rpcParams.Receive.SenderClientId;

        Ray ray = new Ray(camPos + (camForward * 0.5f), camForward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * activeWeapon.shootRange, Color.red, debugRayDuration);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, activeWeapon.shootRange))
        {
            if (hit.collider.TryGetComponent<PlayerHealth>(out var health))
            {
                if (health.OwnerClientId != shooterId)
                {
                    health.TakeDamage(activeWeapon.damage);
                }
                else return;
            }

            SpawnHitEffectClientRpc(hit.point, hit.normal);
            SpawnTracerClientRpc(hit.point, shooterId);
        }
        else
        {
            SpawnTracerClientRpc(ray.GetPoint(activeWeapon.shootRange), shooterId);
        }
    }

    [ClientRpc]
    void SpawnHitEffectClientRpc(Vector3 point, Vector3 normal)
    {
        if (activeWeapon.hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(activeWeapon.hitEffectPrefab, point, Quaternion.LookRotation(normal));
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