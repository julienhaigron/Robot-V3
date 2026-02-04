using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "OccultorData", menuName = "ScriptableObject/Equipment/OccultorData", order = 1)]
public class OccultorEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public float soundCamo = .3f;
    [BoxGroup(GroupID = "Stat")]
    public float visualCamo = .2f;
    [BoxGroup(GroupID = "Stat")]
    public StatBonus[] statBonuses;
}
