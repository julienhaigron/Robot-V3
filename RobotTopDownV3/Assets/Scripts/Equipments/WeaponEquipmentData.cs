using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/Equipment/WeaponData", order = 1)]
public class WeaponEquipmentData : EntityEquipmentData
{
    public Weapon prefab;

    //public int accuracy;
    public SerializableDictionary<DamageType, int> baseDamages;
    public DamageCategory damageCategory;
    //public int range;
    public int visionConeRange;
    public enum DistanceType
	{
        Close,
        Mid,
        Long
	}
    //public SerializableDictionary<DistanceType, float> distanceAccuracyBonus;

    //animation
    public string attackAnimationSuccessId;
    public string attackAnimationFailureId;
    public bool isTwoHanded = false;

    public enum DamageType
	{
        Tranchant,
        Perforant,
        Contendant,
        Laser,
        Plasma,
        Feu,
        Radiation,
        Electrique,
        Magnetique
	}

    public enum DamageCategory
	{
        Physic,
        Elemental
	}

}
