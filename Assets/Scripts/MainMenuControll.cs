using Unity.Netcode;
using Unity.Netcode.Transports;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        // 1. Host Button Logik
        hostButton.onClick.AddListener(() =>
        {
            // Startet Server UND Client gleichzeitig
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host gestartet...");
                // Der Host wechselt für alle die Szene zum Gameplay
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        });

        // 2. Join Button Logik
        joinButton.onClick.AddListener(() =>
        {
            // Statt: var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            // Nutze den voll ausgeschriebenen Pfad:
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            string targetIp = ipInputField.text;
            if (string.IsNullOrEmpty(targetIp))
            {
                targetIp = "127.0.0.1"; // Default, falls nichts eingegeben wurde
            }

            transport.ConnectionData.Address = targetIp;

            // Startet nur den Client
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Verbinde zu: " + targetIp);
            }
        });
    }
}