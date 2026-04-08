using UnityEngine;

public class FixedSpawnPoint : MonoBehaviour
{
    public static Vector3 Pos;
    public static Quaternion Rot;
    public static bool IsReady = false;

    private void Awake()
    {
        Pos = transform.position;
        Rot = transform.rotation;
        IsReady = true;
        Debug.Log("Spawner bereit an Position: " + Pos);
    }
}