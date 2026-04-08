using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;

    // Die NetworkVariable synchronisiert den Wert automatisch für alle Clients
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Nur der Server darf Leben abziehen
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // Wir abonnieren eine Funktion, die aufgerufen wird, wenn sich die Leben ändern
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    // Diese Funktion wird vom Ball-Skript auf dem Server aufgerufen
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;
        Debug.Log($"Server: Spieler {OwnerClientId} hat {damage} Schaden erlitten.");

        // WICHTIG: Der Server muss hier prüfen, ob der Spieler sterben muss
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        // Debug für den Client, damit du siehst, was ankommt
        Debug.Log($"Client Check - Altes Leben: {oldHealth}, Neues Leben: {newHealth}");

        if (newHealth <= 0)
        {
            // Visuelles Feedback oder Sound hier abspielen
            Debug.Log("Ich bin tot!");
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        Debug.Log($"Server: Spieler {OwnerClientId} wird respawnt.");

        // 1. CharacterController kurz ausschalten (WICHTIG für Teleport)
        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        // 2. Position setzen
        transform.position = FixedSpawnPoint.Pos;
        transform.rotation = FixedSpawnPoint.Rot;

        // 3. Leben erst NACH dem Teleport zurücksetzen
        currentHealth.Value = maxHealth;

        // 4. Controller wieder an (ein Frame später wäre ideal, aber meist reicht das hier)
        if (controller != null) controller.enabled = true;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }
}