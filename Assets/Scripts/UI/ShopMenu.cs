using Unity.Netcode;
using UnityEngine;

public class ShopMenu : NetworkBehaviour
{
    // Eigene statische Variable für den Shop-Zustand
    public static bool IsLocalShopOpen { get; private set; }

    [Header("UI Referenzen")]
    public GameObject shopContent;
    public GameObject playerHUD;

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
        // Öffne Shop nur, wenn das normale Pause-Menü NICHT offen ist
        if (Input.GetKeyDown(KeyCode.P) && !PauseManager.IsLocalPaused)
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        if (!IsOwner) return;

        IsLocalShopOpen = !IsLocalShopOpen;
        ApplyShopState();
    }

    public void CloseShop()
    {
        if (!IsOwner) return;

        IsLocalShopOpen = false;
        ApplyShopState();
    }

    private void ApplyShopState()
    {
        if (shopContent != null) shopContent.SetActive(IsLocalShopOpen);

        if (IsLocalShopOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Cursor nur sperren, wenn das Pause-Menü nicht auch gerade offen ist
            if (!PauseManager.IsLocalPaused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }
}