using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class DamageZone : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damageAmount = 25;
    [SerializeField] private float damageInterval = 1.0f;

    private Dictionary<ulong, float> lastDamageTime = new Dictionary<ulong, float>();

    /// <summary>
    /// Handles continuous damage application while a player is inside the trigger.
    /// This logic only executes on the server.
    /// </summary>
    /// <param name="other">The collider entering the zone.</param>
    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth health))
        {
            ulong clientId = health.OwnerClientId;

            if (!lastDamageTime.ContainsKey(clientId))
            {
                ApplyDamage(health, clientId);
            }
            else if (Time.time >= lastDamageTime[clientId] + damageInterval)
            {
                ApplyDamage(health, clientId);
            }
        }
    }

    /// <summary>
    /// Applies damage to the player and updates the last damage timestamp.
    /// </summary>
    /// <param name="health">The PlayerHealth component of the target.</param>
    /// <param name="clientId">The network ID of the client.</param>
    private void ApplyDamage(PlayerHealth health, ulong clientId)
    {
        health.TakeDamage(damageAmount);
        lastDamageTime[clientId] = Time.time;
    }

    /// <summary>
    /// Removes the player from the tracking dictionary when they exit the zone.
    /// </summary>
    /// <param name="other">The collider leaving the zone.</param>
    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth health))
        {
            lastDamageTime.Remove(health.OwnerClientId);
        }
    }
}