using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/WeaponData", order = 1)]
public class WeaponEquipmentData : EntityEquipmentData
{
    public float accuracy;
    public int damage;
    public int range;
    public int visionConeRange;

}
