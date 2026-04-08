using Unity.Netcode;
using UnityEngine;

public class BallProjectile : NetworkBehaviour
{
    public int damageAmount = 20;
    // Wir speichern die ID des Spielers, der diesen Ball geschossen hat
    public ulong shooterId;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Ball hat etwas getroffen: {collision.gameObject.name}");
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent(out PlayerHealth health))
        {
            // Wir prüfen, ob das getroffene Objekt ein ECHTER Spieler ist 
            // und ob dieser Spieler der Schütze war.
            // Ein Bot hat oft keine NetworkObject-Ownership oder ist Server-Owned.

            bool isHitObjectActualPlayer = health.GetComponent<NetworkObject>().IsPlayerObject;

            if (isHitObjectActualPlayer && health.OwnerClientId == shooterId)
            {
                return; // Eigener Spieler getroffen -> ignorieren
            }

            // Wenn es ein Bot ist ODER ein anderer Spieler -> Schaden!
            health.TakeDamage(damageAmount);

            if (GetComponent<NetworkObject>().IsSpawned)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}