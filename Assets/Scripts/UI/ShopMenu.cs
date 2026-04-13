using Unity.Netcode;
using UnityEngine;

public class ShopMenu : NetworkBehaviour
{
    [Header("UI Referenzen")]
    public GameObject shopContent;
    public GameObject playerHUD;
    public PlayerController playerController;

    private bool isShopOpen = false;

    // Diese Methode braucht der PlayerController, um zu wissen, ob der Shop offen ist
    public bool IsShopActive() { return isShopOpen; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (shopContent != null) shopContent.SetActive(false);
            if (playerHUD != null) playerHUD.SetActive(false);
            this.enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        isShopOpen = !isShopOpen;
        ApplyShopState();
    }

    public void CloseShop()
    {
        isShopOpen = false;
        ApplyShopState();
    }

    private void ApplyShopState()
    {
        if (shopContent != null) shopContent.SetActive(isShopOpen);

        if (isShopOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerController.IsGamePaused = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Fokus zurück zum Spiel
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

            PlayerController.IsGamePaused = false;
        }
    }
}