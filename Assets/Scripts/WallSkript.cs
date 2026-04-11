using Unity.Netcode;
using UnityEngine;

public class WallScript : NetworkBehaviour
{
    public GameObject hitParticlePrefab;

    public NetworkVariable<Color> wallColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        ApplyColor(wallColor.Value);
        wallColor.OnValueChanged += (oldVal, newVal) => ApplyColor(newVal);
    }

    private void ApplyColor(Color color)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null) renderer.material.color = color;
    }

    // ÄNDERUNG: Wir übergeben die Farbe direkt als Parameter an den RPC
    [ClientRpc]
    public void SpawnPlaceEffectClientRpc(Vector3 pos, Vector3 normal, Color effectColor)
    {
        if (hitParticlePrefab != null)
        {
            GameObject effect = Instantiate(hitParticlePrefab, pos, Quaternion.LookRotation(normal));

            ParticleSystem[] children = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in children)
            {
                var main = ps.main;
                // Wir nutzen hier den Parameter "effectColor", NICHT die NetworkVariable
                main.startColor = effectColor;
                ps.Play();
            }

            Destroy(effect, 2f);
        }
    }
}