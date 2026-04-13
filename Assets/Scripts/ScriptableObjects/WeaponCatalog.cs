using UnityEngine;

[CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Scriptable Objects/WeaponCatalog")]
public class WeaponCatalog : ScriptableObject
{
    public System.Collections.Generic.List<WeaponData> allWeapons;
}
