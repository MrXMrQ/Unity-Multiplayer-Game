using Unity.Netcode;
using UnityEngine;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    [Header("Weapon Settings")]
    public WeaponData weapon;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponName;

    /// <summary>
    /// Initializes the UI text with the weapon's name from the data object.
    /// </summary>
    private void Start()
    {
        if (weapon != null && weaponName != null)
        {
            weaponName.text = weapon.weaponName;
        }
    }

    /// <summary>
    /// Finds the local player object and triggers the weapon change and shop closing logic.
    /// Called by the UI Button's OnClick event.
    /// </summary>
    public void Buy()
    {
        // Get the local player's network object
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (localPlayer != null)
        {
            // Equip the new weapon
            if (localPlayer.TryGetComponent<PlayerShooting>(out var shootingScript))
            {
                shootingScript.ChangeWeapon(weapon);
            }

            // Close the shop menu for the local player
            if (localPlayer.TryGetComponent<ShopManager>(out var shop))
            {
                shop.CloseShop();
            }
        }
    }
}