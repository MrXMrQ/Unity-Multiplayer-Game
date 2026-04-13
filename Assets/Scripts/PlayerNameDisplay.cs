using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections; // Wichtig für FixedString

public class PlayNameDisplay : NetworkBehaviour
{
    [Header("UI Referenz")]
    public TextMeshProUGUI nameTagText;

    // NetworkVariable für den Namen (FixedString ist notwendig für Netcode)
    private NetworkVariable<FixedString32Bytes> networkPlayerName = new NetworkVariable<FixedString32Bytes>(
        "Spieler",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // 1. Wenn ich der Besitzer bin, sende meinen Namen an den Server
        if (IsOwner)
        {
            string localName = PlayMenuControl.LocalPlayerName;
            UpdateNameServerRpc(localName);
        }

        // 2. Den Text initial setzen
        nameTagText.text = networkPlayerName.Value.ToString();

        // 3. Auf Änderungen reagieren (falls jemand den Namen mitten im Spiel ändert)
        networkPlayerName.OnValueChanged += (oldValue, newValue) =>
        {
            nameTagText.text = newValue.ToString();
        };
    }

    [ServerRpc]
    private void UpdateNameServerRpc(string newName)
    {
        // Der Server schreibt den Namen in die NetworkVariable, damit alle ihn sehen
        networkPlayerName.Value = newName;
    }

    void LateUpdate()
    {
        // Damit der Name immer zur Kamera schaut (Billboarding)
        if (Camera.main != null)
        {
            nameTagText.transform.parent.LookAt(nameTagText.transform.parent.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }
}