using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/WeaponData", order = 1)]
public class WeaponEquipmentData : EntityEquipmentData
{
    public int accuracy;
    public int damage;
    public int range;
    public int visionConeRange;
    public enum DistanceType
	{
        Close,
        Mid,
        Long
	}
    public SerializableDictionary<DistanceType, int> distanceAccuracyBonus;

    //animation
    public string attackAnimationSuccessId;
    public string attackAnimationFailureId;
    public bool isTwoHanded = false;

}
