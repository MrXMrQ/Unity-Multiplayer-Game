using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class PlayerNameManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameTagText;
    [SerializeField] private Transform nameTagContainer;

    private NetworkVariable<FixedString32Bytes> networkPlayerName = new NetworkVariable<FixedString32Bytes>(
        "Player",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Camera mainCamera;

    /// <summary>
    /// Initializes the player name, syncs it with the server, and sets up synchronization callbacks.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        mainCamera = Camera.main;

        if (IsOwner)
        {
            string localName = MainMenuControl.LocalPlayerName;
            UpdateNameServerRpc(localName);
        }

        // Initial UI setup
        UpdateNameDisplay(networkPlayerName.Value.ToString());

        networkPlayerName.OnValueChanged += OnNameChanged;
    }

    /// <summary>
    /// Unsubscribes from name change events when the network object despawns.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        networkPlayerName.OnValueChanged -= OnNameChanged;
    }

    /// <summary>
    /// Updates the name on the server. The server then propagates the value to all clients.
    /// </summary>
    /// <param name="newName">The new name string to be validated and set.</param>
    [ServerRpc]
    private void UpdateNameServerRpc(string newName)
    {
        // Ensure the string doesn't exceed the FixedString32Bytes limit
        if (newName.Length > 31)
        {
            newName = newName.Substring(0, 31);
        }

        networkPlayerName.Value = newName;
    }

    private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        UpdateNameDisplay(newValue.ToString());
    }

    private void UpdateNameDisplay(string nameValue)
    {
        if (nameTagText != null)
        {
            nameTagText.text = nameValue;
        }
    }

    /// <summary>
    /// Handles the billboarding effect to ensure the name tag always faces the camera.
    /// </summary>
    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }

        if (nameTagContainer != null)
        {
            // Efficient billboarding rotation
            nameTagContainer.rotation = mainCamera.transform.rotation;
        }
    }
}