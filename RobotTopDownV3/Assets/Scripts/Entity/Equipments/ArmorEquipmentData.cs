using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ArmorData", menuName = "ScriptableObject/Equipment/ArmorData", order = 1)]
public class ArmorEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public StatBonus[] statBonuses;
}
