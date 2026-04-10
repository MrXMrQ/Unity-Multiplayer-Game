using Unity.Netcode;
using UnityEngine;

public class WallScript : NetworkBehaviour
{
    // Die NetworkVariable synchronisiert die Farbe für alle (auch Nachzügler)
    public NetworkVariable<Color> wallColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Wenn die Variable sich ändert oder das Objekt spawnt, Farbe setzen
        ApplyColor(wallColor.Value);
        wallColor.OnValueChanged += (oldVal, newVal) => ApplyColor(newVal);
    }

    private void ApplyColor(Color color)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}