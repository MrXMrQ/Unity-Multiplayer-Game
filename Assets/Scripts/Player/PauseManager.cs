using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseManager : NetworkBehaviour
{
    public static bool IsLocalPaused { get; private set; }

    [Header("UI Referenzen")]
    public GameObject pauseMenuPanel;

    private void Start()
    {
        if (!IsOwner) return;

        IsLocalPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void TogglePause()
    {
        if (!IsOwner) return;

        IsLocalPaused = !IsLocalPaused;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(IsLocalPaused);

        SetCursorState(IsLocalPaused);
    }

    public void ResumeGame()
    {
        if (!IsOwner) return;

        IsLocalPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        SetCursorState(false);
    }

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

    private void SetCursorState(bool paused)
    {
        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}