using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP; // Direktes Using für UTP

public class MainMenuControl : MonoBehaviour
{
    [Header("UI Elemente")]
    public TMP_InputField ipInputField;
    public Button hostButton;
    public Button joinButton;

    [Header("Einstellungen")]
    public string gameSceneName = "GameScene";

    private void Start()
    {
        // --- AUTOMATISCHER SERVER START (Für Ubuntu/Docker) ---
        // Diese Prüfung muss AUSSERHALB der Button-Logik stehen
        if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("Dedicated Server erkannt! Initialisiere Netzwerk...");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = "0.0.0.0";
                // Wir erzwingen das Erlauben von Remote-Verbindungen im Code
            }

            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("Server erfolgreich gestartet. Lade Szene...");
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            return; // Beende Start(), da wir auf dem Server keine Buttons brauchen
        }

        // --- HOST BUTTON (Für lokales Testen) ---
        hostButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host gestartet...");
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        });

        // --- JOIN BUTTON (Für deinen Windows Client) ---
        joinButton.onClick.AddListener(() =>
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            string targetIp = ipInputField.text;
            if (string.IsNullOrEmpty(targetIp))
            {
                targetIp = "127.0.0.1";
            }

            transport.ConnectionData.Address = targetIp;
            Debug.Log("Versuche Verbindung zu: " + targetIp);

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client gestartet...");
            }
            else
            {
                Debug.LogError("Client konnte nicht gestartet werden!");
            }
        });
    }
}