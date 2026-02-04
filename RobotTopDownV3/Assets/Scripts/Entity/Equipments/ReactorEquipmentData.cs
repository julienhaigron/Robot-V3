using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ReactorData", menuName = "ScriptableObject/Equipment/ReactorData", order = 1)]
public class ReactorEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public int energyProduced = 160;
}
