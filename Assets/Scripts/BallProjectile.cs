using Unity.Netcode;
using UnityEngine;

public class BallProjectile : NetworkBehaviour
{
    public NetworkVariable<Color> ballColor = new NetworkVariable<Color>(Color.white);
    public ulong shooterId;

    [Header("Effekt Einstellungen")]
    public GameObject hitParticlePrefab;

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
        // Nur der Server entscheidet über Effekte und Zerstörung
        if (!IsServer) return;

        // --- 1. TREFFER-DATEN SAMMELN ---
        ContactPoint contact = collision.contacts[0];
        Vector3 hitPos = contact.point;
        Vector3 hitNormal = contact.normal;

        // --- 2. PARTIKEL AUF ALLEN CLIENTS TRIGGERN ---
        SpawnHitEffectClientRpc(hitPos, hitNormal);

        // --- 3. SCHADENS-LOGIK ---
        if (collision.gameObject.TryGetComponent(out PlayerHealth health))
        {
            ulong hitPlayerId = health.OwnerClientId;
            bool isRealPlayer = health.GetComponent<NetworkObject>().IsPlayerObject;

            // Selbstschaden verhindern (nur bei echten Spielern)
            if (!(isRealPlayer && hitPlayerId == shooterId))
            {
                health.TakeDamage(20);
            }
        }

        // --- 4. BALL DESPAWNEN (Immer bei Kollision) ---
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    private void SpawnHitEffectClientRpc(Vector3 pos, Vector3 normal)
    {
        if (hitParticlePrefab != null)
        {
            // Effekt instanziieren und zur Oberfläche ausrichten
            GameObject effect = Instantiate(hitParticlePrefab, pos, Quaternion.LookRotation(normal));

            // Farbe an alle Kinder (MainSparks, DustPuff) weitergeben
            ParticleSystem[] children = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in children)
            {
                var main = ps.main;
                main.startColor = ballColor.Value;
                ps.Play();
            }

            // Lokal nach 2 Sekunden löschen
            Destroy(effect, 2f);
        }
    }
}