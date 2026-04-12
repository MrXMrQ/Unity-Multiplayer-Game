using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public int damage;
    public int magazineSize;
    public float fireRate;
    public float tracerSpeed;
    public float reloadSpeed;
    public int shootRange;
    public GameObject weaponPrefab;
    public GameObject tracerPrefab;
    public GameObject hitEffectPrefab;
}
