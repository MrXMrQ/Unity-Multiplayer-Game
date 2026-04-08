using Unity.Netcode;
using UnityEngine;

public class BotSpawner : NetworkBehaviour
{
    public GameObject botPrefab;

    public override void OnNetworkSpawn()
    {
        // Nur der Server spawnt den Bot beim Start
        if (IsServer)
        {
            SpawnBot();
        }
    }

    void SpawnBot()
    {
        GameObject bot = Instantiate(botPrefab, transform.position + Vector3.right * 3, Quaternion.identity);
        bot.GetComponent<NetworkObject>().Spawn();

        // Optional: Gib dem Bot eine feste Farbe, damit er wie ein Gegner aussieht
        if (bot.TryGetComponent(out MeshRenderer ren))
        {
            ren.material.color = Color.red;
        }
    }
}