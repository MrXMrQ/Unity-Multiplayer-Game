using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseManager : NetworkBehaviour
{
    /// <summary>
    /// Static state to track if the game is paused for the local player.
    /// </summary>
    public static bool IsLocalPaused { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;

    /// <summary>
    /// Initializes the pause state and UI visibility when the network object spawns.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            this.enabled = false;
            return;
        }

        IsLocalPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Reagiere auf Escape, um das Menü zu öffnen/schließen
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggles the pause menu state and updates cursor and UI visibility.
    /// </summary>
    public void TogglePause()
    {
        if (!IsOwner) return;

        IsLocalPaused = !IsLocalPaused;
        ApplyPauseState();
    }

    /// <summary>
    /// Explicitly closes the pause menu and returns to the game.
    /// </summary>
    public void ResumeGame()
    {
        if (!IsOwner) return;

        IsLocalPaused = false;
        ApplyPauseState();
    }

    /// <summary>
    /// Shuts down the network connection and exits the application.
    /// </summary>
    public void QuitGame()
    {
        if (!IsOwner) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>
    /// Updates the UI panel and delegates cursor state management.
    /// </summary>
    private void ApplyPauseState()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(IsLocalPaused);
        }

        UpdateCursorState();
    }

    /// <summary>
    /// Manages cursor locking and visibility based on the current pause state.
    /// </summary>
    private void UpdateCursorState()
    {
        if (IsLocalPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Only relock the cursor if the shop is not currently open
            if (!ShopManager.IsLocalShopOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            ClearUIFocus();
        }
    }

    /// <summary>
    /// Deselects any UI elements to prevent navigation issues after closing the menu.
    /// </summary>
    private void ClearUIFocus()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}