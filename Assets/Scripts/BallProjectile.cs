using Unity.Netcode;
using UnityEngine;

public class BallProjectile : NetworkBehaviour
{
    public NetworkVariable<Color> ballColor = new NetworkVariable<Color>(Color.white);
    public ulong shooterId;

    public override void OnNetworkSpawn()
    {
        ApplyColor(ballColor.Value);
        ballColor.OnValueChanged += (oldColor, newColor) => ApplyColor(newColor);
    }
    [ClientRpc]
    public void FireBallClientRpc(Vector3 velocity)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
        }
    }

    private void ApplyColor(Color c)
    {
        GetComponent<MeshRenderer>().material.color = c;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent(out PlayerHealth health))
        {
            ulong hitPlayerId = health.OwnerClientId;

            // Logik-Fix: Wenn das getroffene Objekt kein "PlayerObject" ist (z.B. Bot),
            // dann ignorieren wir den ID-Vergleich und machen immer Schaden.
            bool isRealPlayer = health.GetComponent<NetworkObject>().IsPlayerObject;

            if (isRealPlayer && hitPlayerId == shooterId)
            {
                return; // Selbsschaden verhindern
            }

            health.TakeDamage(20);

            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
    }
}