using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;

public class MainMenuControl : MonoBehaviour
{
    [Header("UI Elemente")]
    public TMP_InputField ipInputField;
    public Button hostButton;
    public Button joinButton;

    [Header("Einstellungen")]
    public string gameSceneName = "GameScene";
    public TMP_InputField nameInputField;

    // Die statische Variable, auf die das Player-Script zugreift
    public static string LocalPlayerName = "Player";

    void Awake()
    {
        if (Display.displays.Length > 1)
        {
            Display.displays[0].Activate();
        }

        PlayerPrefs.DeleteKey("UnitySelectMonitor");
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
    }

    // Speichert den Namen aus dem InputField in die statische Variable
    public void SaveName()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            LocalPlayerName = nameInputField.text;
            Debug.Log(LocalPlayerName);
            Debug.Log($"Name gespeichert: {LocalPlayerName}");
        }
    }

    private void Start()
    {
        // --- AUTOMATISCHER SERVER START (Headless) ---
        if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("Dedicated Server erkannt!");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = "0.0.0.0";
            }

            if (NetworkManager.Singleton.StartServer())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            return;
        }

        // --- HOST BUTTON ---
        hostButton.onClick.AddListener(() =>
        {
            SaveName(); // WICHTIG: Erst Namen sichern
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host gestartet mit Name: " + LocalPlayerName);
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        });

        // --- JOIN BUTTON ---
        joinButton.onClick.AddListener(() =>
        {
            SaveName(); // WICHTIG: Erst Namen sichern
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            string targetIp = ipInputField.text;
            if (string.IsNullOrEmpty(targetIp)) targetIp = "127.0.0.1";

            transport.ConnectionData.Address = targetIp;

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client gestartet als: " + LocalPlayerName);
            }
        });
    }
}