using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAttackData", menuName = "ScriptableObject/WeaponAttackData")]
public class WeaponAttackData : ScriptableObject
{
    public SerializableDictionary<WeaponEquipmentData.DamageType, short> damageBonuses;
    public WeaponEquipmentData.DamageCategory damageCategory;

}
