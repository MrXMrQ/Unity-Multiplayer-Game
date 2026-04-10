using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public HealthBar healthBarLocal;
    public HealthBar healthBarGlobal;

    // Die NetworkVariable synchronisiert den Wert automatisch für alle Clients
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Nur der Server darf Leben abziehen
    );

    public override void OnNetworkSpawn()
    {
        // INITIALISIERUNG
        // Die globale Bar (über dem Kopf) initialisieren wir für JEDEN
        if (healthBarGlobal != null)
        {
            healthBarGlobal.SetMaxHealth(maxHealth);
            healthBarGlobal.SetHealth(currentHealth.Value);
        }

        // Die lokale Bar (HUD) initialisieren wir NUR für den Besitzer
        if (IsOwner && healthBarLocal != null)
        {
            healthBarLocal.SetMaxHealth(maxHealth);
            healthBarLocal.SetHealth(currentHealth.Value);
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    // Diese Funktion wird vom Ball-Skript auf dem Server aufgerufen
    // Auf dem Server gerufen
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 100; // Leben direkt heilen
            healthBarLocal.SetHealth(currentHealth.Value);
            healthBarGlobal.SetHealth(currentHealth.Value);
            RespawnClientRpc(); // Dem Client sagen: "Teleportier dich!"
        }
    }

    [ClientRpc]
    void RespawnClientRpc()
    {
        if (IsOwner) // Nur der betroffene Spieler führt das aus
        {
            var controller = GetComponent<CharacterController>();

            // 1. Controller kurz ausmachen (wichtig!)
            if (controller != null) controller.enabled = false;

            // 2. Position setzen (Hol dir die Position vom SpawnManager)
            transform.position = new Vector3(0, 30, 0); // Oder dein Spawn-Punkt

            // 3. Controller wieder anmachen
            if (controller != null) controller.enabled = true;

            Debug.Log("Respawn lokal ausgeführt!");
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        // 1. GLOBAL: Diese Bar soll JEDER Spieler bei JEDEM anderen sehen
        if (healthBarGlobal != null)
        {
            healthBarGlobal.SetHealth(newHealth);
        }

        // 2. LOCAL: Diese Bar (dein HUD) wird nur für dich selbst aktualisiert
        if (IsOwner && healthBarLocal != null)
        {
            healthBarLocal.SetHealth(newHealth);
        }

        if (newHealth <= 0) Debug.Log($"{gameObject.name} ist tot!");
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