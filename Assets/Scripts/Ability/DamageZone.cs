using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class DamageZone : NetworkBehaviour
{
    [Header("Settings")]
    public int damageAmount = 25;
    public float damageInterval = 1.0f;

    // Speichert den Zeitpunkt des letzten Schadens pro Spieler (Key = ClientId)
    private Dictionary<ulong, float> lastDamageTime = new Dictionary<ulong, float>();

    private void OnTriggerStay(Collider other)
    {
        // Schadensberechnung findet NUR auf dem Server statt
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth health))
        {
            ulong clientId = health.OwnerClientId;

            // Prüfen, ob der Spieler bereits in der Liste ist
            if (!lastDamageTime.ContainsKey(clientId))
            {
                // Erster Treffer: Schaden zufügen und Zeit registrieren
                ApplyDamage(health, clientId);
            }
            else if (Time.time >= lastDamageTime[clientId] + damageInterval)
            {
                // Intervall abgelaufen: Erneut Schaden zufügen
                ApplyDamage(health, clientId);
            }
        }
    }

    private void ApplyDamage(PlayerHealth health, ulong clientId)
    {
        health.TakeDamage(damageAmount);
        lastDamageTime[clientId] = Time.time;
        Debug.Log($"Lava-Schaden an Client {clientId}. Nächster Tick in {damageInterval}s");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth health))
        {
            // Wenn der Spieler die Lava verlässt, löschen wir den Eintrag,
            // damit er beim nächsten Mal sofort wieder Schaden bekommt.
            lastDamageTime.Remove(health.OwnerClientId);
        }
    }
}