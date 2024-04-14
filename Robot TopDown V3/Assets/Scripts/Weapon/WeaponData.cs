using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/WeaponData", order = 1)]
public class WeaponData : ScriptableObject
{
    public string saveKey;

    public float accuracy;
    public float damage;
    public int range;
    public int visionCone;

}
