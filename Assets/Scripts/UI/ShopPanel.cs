using Unity.Netcode;
using UnityEngine;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    public WeaponData weapon;
    public TextMeshProUGUI weaponName;

    public void Start()
    {
        weaponName.text = weapon.weaponName;
    }

    public void Buy()
    {
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            // Waffe wechseln
            PlayerShooting shootingScript = localPlayer.GetComponent<PlayerShooting>();
            if (shootingScript != null) shootingScript.ChangeWeapon(weapon);

            // Shop des Spielers schliessen
            ShopMenu shop = localPlayer.GetComponent<ShopMenu>();
            if (shop != null) shop.CloseShop();
        }
    }
}