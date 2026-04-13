using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopManager : NetworkBehaviour
{
    /// <summary>
    /// Static state to track if the shop is currently open for the local player.
    /// </summary>
    public static bool IsLocalShopOpen { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject shopContent;
    [SerializeField] private GameObject playerHUD;

    /// <summary>
    /// Disables the shop script and UI for non-owner clients and initializes state.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (shopContent != null) shopContent.SetActive(false);
            if (playerHUD != null) playerHUD.SetActive(false);
            this.enabled = false;
            return;
        }

        // Ensure shop is closed on spawn
        IsLocalShopOpen = false;
        if (shopContent != null) shopContent.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.P) && !PauseManager.IsLocalPaused)
        {
            ToggleShop();
        }
    }

    /// <summary>
    /// Toggles the shop visibility and updates the cursor state.
    /// </summary>
    public void ToggleShop()
    {
        if (!IsOwner) return;

        IsLocalShopOpen = !IsLocalShopOpen;
        ApplyShopState();
    }

    /// <summary>
    /// Explicitly closes the shop and resets UI focus.
    /// </summary>
    public void CloseShop()
    {
        if (!IsOwner) return;

        IsLocalShopOpen = false;
        ApplyShopState();
    }

    /// <summary>
    /// Synchronizes the UI elements and cursor locking with the current shop state.
    /// </summary>
    private void ApplyShopState()
    {
        if (shopContent != null)
        {
            shopContent.SetActive(IsLocalShopOpen);
        }

        UpdateCursorState();
        ClearUIFocus();
    }

    /// <summary>
    /// Manages cursor visibility and lock state based on shop and pause menu conditions.
    /// </summary>
    private void UpdateCursorState()
    {
        if (IsLocalShopOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (!PauseManager.IsLocalPaused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    /// <summary>
    /// Deselects any UI elements to prevent unwanted input focus after closing the shop.
    /// </summary>
    private void ClearUIFocus()
    {
        if (!IsLocalShopOpen && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}