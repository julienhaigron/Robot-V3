using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ChipsetData", menuName = "ScriptableObject/Equipment/ChipsetData", order = 1)]
public class ChipsetEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public StatBonus[] statBonuses; 
}
