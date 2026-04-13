using Unity.Netcode;
using UnityEngine;
using System.Collections;
using TMPro;

public class Shooting : NetworkBehaviour
{
    [Header("Settings")]
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

    [Header("Catalog")]
    public WeaponCatalog catalog;
    private NetworkVariable<int> activeWeaponIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private WeaponData activeWeapon;
    private GameObject currentWeaponInstance;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<PlayerController>();

        // Event abonnieren: Wenn sich der Index ändert, wird das Modell bei ALLEN aktualisiert
        activeWeaponIndex.OnValueChanged += (oldIdx, newIdx) =>
        {
            EquipWeapon(newIdx);
        };

        // Initiale Waffe laden (für den Start)
        EquipWeapon(activeWeaponIndex.Value);

        if (IsOwner)
        {
            if (playerHUD != null) playerHUD.SetActive(true);

            if (reloadCooldown != null)
            {
                reloadCooldown.SetMaxValue(100);
                reloadCooldown.SetValue(0);
            }
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

        // reload
        if (Input.GetKeyDown(KeyCode.R) && activeWeapon != null && currentAmmo < activeWeapon.magazineSize && !isReloading)
        {
            StartCoroutine(Reload());
        }

        // shoot
        if (Input.GetMouseButton(0) && activeWeapon != null)
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

        currentAmmo = activeWeapon.magazineSize;
        currentAmmoText.text = $"{currentAmmo}";

        if (reloadCooldown != null) reloadCooldown.SetValue(100);

        isReloading = false;
        // Kurze Verzögerung bevor der Balken verschwindet
        yield return new WaitForSeconds(0.2f);
        if (!isReloading && reloadCooldown != null) reloadCooldown.SetValue(0);
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
        if (activeWeapon != null && activeWeapon.tracerPrefab != null && shootPoint != null)
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

    private void EquipWeapon(int index)
    {
        if (catalog == null || index < 0 || index >= catalog.allWeapons.Count) return;

        activeWeapon = catalog.allWeapons[index];

        // Altes Modell löschen
        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        // Neues Modell spawnen
        if (activeWeapon.weaponPrefab != null && weaponSpawnPoint != null)
        {
            currentWeaponInstance = Instantiate(activeWeapon.weaponPrefab, weaponSpawnPoint);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            // Suche den Shootpoint im neuen Modell
            shootPoint = currentWeaponInstance.transform.Find("shootPoint");
            if (shootPoint == null) Debug.LogWarning($"Kein 'shootPoint' im Prefab von {activeWeapon.weaponName} gefunden!");
        }

        // Munition und UI zurücksetzen
        currentAmmo = activeWeapon.magazineSize;
        if (IsOwner)
        {
            if (currentAmmoText != null) currentAmmoText.text = $"{currentAmmo}";
            if (magazineSizeText != null) magazineSizeText.text = $"{activeWeapon.magazineSize}";
            isReloading = false;
            StopAllCoroutines(); // Laufende Reloads abbrechen
            if (reloadCooldown != null) reloadCooldown.SetValue(0);
        }
    }

    // Wird vom ShopPanel aufgerufen
    public void ChangeWeapon(WeaponData newWeaponData)
    {
        if (!IsOwner) return;

        int index = catalog.allWeapons.IndexOf(newWeaponData);
        if (index != -1)
        {
            activeWeaponIndex.Value = index;
        }
        else
        {
            Debug.LogError("Waffe nicht im Katalog gefunden!");
        }
    }

    [ServerRpc]
    void RequestShootServerRpc(Vector3 camPos, Vector3 camForward, ServerRpcParams rpcParams = default)
    {
        ulong shooterId = rpcParams.Receive.SenderClientId;

        // Wir nutzen hier die activeWeapon des Servers (durch den synchronisierten Index)
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
        if (activeWeapon != null && activeWeapon.hitEffectPrefab != null)
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