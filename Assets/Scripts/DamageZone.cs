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
        // Auf Linux Dedicated Servern ist IsServer immer true, 
        // aber wir prüfen zur Sicherheit trotzdem.
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth health))
        {
            // Wir erzwingen eine Prüfung der NetworkVariable
            health.TakeDamage(damageAmount);
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