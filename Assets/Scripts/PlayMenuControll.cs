using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode.Transports.UTP;


public class PlayMenuControl : MonoBehaviour
{
    public TMP_InputField ipInputField;
    public TMP_InputField nameInputField;
    public string gameSceneName = "GameScene";
    public static string LocalPlayerName = "Player";

    public void Start()
    {
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
    }

    public void SaveName()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            LocalPlayerName = nameInputField.text;
            Debug.Log(LocalPlayerName);
            Debug.Log($"Name gespeichert: {LocalPlayerName}");
        }
    }
    public void StartHost()
    {
        SaveName();
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host gestartet mit Name: " + LocalPlayerName);
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    public void JoinGame()
    {
        SaveName();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        string targetIp = ipInputField.text;
        if (string.IsNullOrEmpty(targetIp)) targetIp = "127.0.0.1";

        transport.ConnectionData.Address = targetIp;

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client gestartet als: " + LocalPlayerName);
        }
    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}