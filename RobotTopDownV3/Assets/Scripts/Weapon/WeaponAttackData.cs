using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAttackData", menuName = "Scriptable Objects/WeaponAttackData")]
public class WeaponAttackData : ScriptableObject
{
    public SerializableDictionary<WeaponEquipmentData.DamageType, short> damageBonuses;
    public WeaponEquipmentData.DamageCategory damageCategory;

}
