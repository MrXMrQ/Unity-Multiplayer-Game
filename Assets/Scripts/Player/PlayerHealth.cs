using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Vector3 spawnPoint = new Vector3(0, 10, 0);

    [Header("UI References")]
    [SerializeField] private Bar healthBarLocal;
    [SerializeField] private Bar healthBarGlobal;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// Initializes UI and subscribes to health change events when the network object spawns.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        UpdateVisuals(currentHealth.Value);

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    /// <summary>
    /// Unsubscribes from health change events when the network object despawns.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    /// <summary>
    /// Reduces health on the server and triggers respawn logic if health drops to zero.
    /// </summary>
    /// <param name="damage">The amount of damage to apply.</param>
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);

        if (currentHealth.Value <= 0)
        {
            ResetHealth();
            RespawnClientRpc();
        }
    }

    /// <summary>
    /// Resets the current health to the maximum value.
    /// </summary>
    private void ResetHealth()
    {
        currentHealth.Value = maxHealth;
    }

    /// <summary>
    /// Teleports the owning client to the spawn point and handles CharacterController state.
    /// </summary>
    [ClientRpc]
    private void RespawnClientRpc()
    {
        if (!IsOwner) return;

        var controller = GetComponent<CharacterController>();

        if (controller != null) controller.enabled = false;
        transform.position = spawnPoint;
        if (controller != null) controller.enabled = true;
    }

    /// <summary>
    /// Callback triggered when the currentHealth NetworkVariable changes.
    /// </summary>
    /// <param name="oldHealth">Previous health value.</param>
    /// <param name="newHealth">Updated health value.</param>
    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateVisuals(newHealth);
    }

    /// <summary>
    /// Updates the local and global health bars based on the current health value.
    /// </summary>
    /// <param name="value">The current health value to display.</param>
    private void UpdateVisuals(int value)
    {
        if (healthBarGlobal != null)
        {
            healthBarGlobal.SetMaxValue(maxHealth);
            healthBarGlobal.SetValue(value);
        }

        if (IsOwner && healthBarLocal != null)
        {
            healthBarLocal.SetMaxValue(maxHealth);
            healthBarLocal.SetValue(value);
        }
    }
}