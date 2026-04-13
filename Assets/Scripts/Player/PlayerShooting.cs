using Unity.Netcode;
using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform weaponSpawnPoint;
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private float debugRayDuration = 2f;

    [Header("Player HUD")]
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI magazineSizeText;
    [SerializeField] private Bar reloadCooldown;

    [Header("Catalog")]
    [SerializeField] private WeaponCatalog catalog;

    private NetworkVariable<int> activeWeaponIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private PlayerController playerController;
    private WeaponData activeWeapon;
    private GameObject currentWeaponInstance;
    private Transform shootPoint;
    private float lastShootTime = 0f;
    private int currentAmmo;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;

    /// <summary>
    /// Initializes the shooting system, UI visibility, and weapon synchronization.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<PlayerController>();

        activeWeaponIndex.OnValueChanged += (oldIdx, newIdx) =>
        {
            EquipWeapon(newIdx);
        };

        EquipWeapon(activeWeaponIndex.Value);

        if (IsOwner)
        {
            if (playerHUD != null) playerHUD.SetActive(true);
            UpdateAmmoUI();

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

    private void Update()
    {
        if (!IsOwner || PauseManager.IsLocalPaused || ShopMenu.IsLocalShopOpen) return;

        HandleInput();
    }

    /// <summary>
    /// Processes player input for shooting and reloading.
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && CanReload())
        {
            reloadCoroutine = StartCoroutine(Reload());
        }

        if (Input.GetMouseButton(0) && CanShoot())
        {
            Shoot();
        }
    }

    private bool CanShoot()
    {
        return activeWeapon != null &&
               !isReloading &&
               currentAmmo > 0 &&
               Time.time >= lastShootTime + 1f / activeWeapon.fireRate;
    }

    private bool CanReload()
    {
        return activeWeapon != null &&
               currentAmmo < activeWeapon.magazineSize &&
               !isReloading;
    }

    /// <summary>
    /// Executes the shooting logic locally and requests verification from the server.
    /// </summary>
    private void Shoot()
    {
        lastShootTime = Time.time;
        currentAmmo--;
        UpdateAmmoUI();

        Vector3 target = GetTargetPoint();
        SpawnTracer(target);

        RequestShootServerRpc(
            playerController.playerCamera.transform.position,
            playerController.playerCamera.transform.forward
        );
    }

    /// <summary>
    /// Handles the reload timer and updates the reload progress bar.
    /// </summary>
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
        UpdateAmmoUI();

        if (reloadCooldown != null) reloadCooldown.SetValue(100);
        isReloading = false;

        yield return new WaitForSeconds(0.2f);
        if (!isReloading && reloadCooldown != null) reloadCooldown.SetValue(0);
    }

    /// <summary>
    /// Calculates the point where the bullet should hit using a raycast from the camera.
    /// </summary>
    /// <returns>The world space position of the hit point or the maximum range point.</returns>
    private Vector3 GetTargetPoint()
    {
        Ray ray = new Ray(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, activeWeapon.shootRange))
        {
            return hit.point;
        }
        return ray.GetPoint(activeWeapon.shootRange);
    }

    /// <summary>
    /// Spawns a visual bullet tracer that moves towards the target point.
    /// </summary>
    /// <param name="targetPoint">The destination for the tracer.</param>
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

    /// <summary>
    /// Instantiates the weapon model and sets up local shoot points and ammo.
    /// </summary>
    /// <param name="index">The index of the weapon in the catalog.</param>
    private void EquipWeapon(int index)
    {
        if (catalog == null || index < 0 || index >= catalog.allWeapons.Count) return;

        activeWeapon = catalog.allWeapons[index];

        if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

        if (activeWeapon.weaponPrefab != null && weaponSpawnPoint != null)
        {
            currentWeaponInstance = Instantiate(activeWeapon.weaponPrefab, weaponSpawnPoint);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            shootPoint = currentWeaponInstance.transform.Find("shootPoint");
            if (shootPoint == null) Debug.LogWarning($"Missing 'shootPoint' in {activeWeapon.weaponName} prefab!");
        }

        currentAmmo = activeWeapon.magazineSize;

        if (IsOwner)
        {
            UpdateAmmoUI();
            isReloading = false;
            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
            if (reloadCooldown != null) reloadCooldown.SetValue(0);
        }
    }

    /// <summary>
    /// Public interface to change the active weapon, usually called from UI menus.
    /// </summary>
    /// <param name="newWeaponData">The data object of the weapon to equip.</param>
    public void ChangeWeapon(WeaponData newWeaponData)
    {
        if (!IsOwner) return;

        int index = catalog.allWeapons.IndexOf(newWeaponData);
        if (index != -1) activeWeaponIndex.Value = index;
    }

    private void UpdateAmmoUI()
    {
        if (currentAmmoText != null) currentAmmoText.text = $"{currentAmmo}";
        if (magazineSizeText != null && activeWeapon != null) magazineSizeText.text = $"{activeWeapon.magazineSize}";
    }

    /// <summary>
    /// Validates the shot on the server and applies damage to hit players.
    /// </summary>
    [ServerRpc]
    private void RequestShootServerRpc(Vector3 camPos, Vector3 camForward, ServerRpcParams rpcParams = default)
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
    private void SpawnHitEffectClientRpc(Vector3 point, Vector3 normal)
    {
        if (activeWeapon != null && activeWeapon.hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(activeWeapon.hitEffectPrefab, point, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
    }

    [ClientRpc]
    private void SpawnTracerClientRpc(Vector3 targetPoint, ulong shooterId)
    {
        if (NetworkManager.Singleton.LocalClientId != shooterId)
        {
            SpawnTracer(targetPoint);
        }
    }
}