using Unity.Netcode;
using UnityEngine;

public class DamageZone : NetworkBehaviour
{
    [Header("Settings")]
    public int damageAmount = 25;
    public float damageInterval = 1.0f; // Alle wieviele Sekunden bekommt man Schaden?

    // Wir speichern, wann welcher Spieler das letzte Mal Schaden bekommen hat
    private System.Collections.Generic.Dictionary<ulong, float> lastDamageTime = new System.Collections.Generic.Dictionary<ulong, float>();

    private void OnTriggerStay(Collider other)
    {
        // WICHTIG: Nur der Server darf Leben abziehen
        if (!IsServer) return;

        // Prüfen, ob das getroffene Objekt ein Spieler ist
        if (other.TryGetComponent(out PlayerHealth health))
        {
            ulong clientId = health.OwnerClientId;

            // Cooldown-Check für kontinuierlichen Schaden (damit man nicht instant stirbt)
            if (!lastDamageTime.ContainsKey(clientId) || Time.time >= lastDamageTime[clientId] + damageInterval)
            {
                health.TakeDamage(damageAmount);
                lastDamageTime[clientId] = Time.time;

                Debug.Log($"Lava fügt Spieler {clientId} Schaden zu. Restleben: {health.currentHealth.Value}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        // Wenn der Spieler die Lava verlässt, löschen wir ihn aus der Liste
        if (other.TryGetComponent(out PlayerHealth health))
        {
            lastDamageTime.Remove(health.OwnerClientId);
        }
    }
}