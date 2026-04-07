using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NeuronalMembrane", menuName = "ScriptableObject/Equipment/NeuronalMembrane", order = 1)]
public class NeuronalMembraneEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public int equipmentSlotAvailable = 2;
    [Min(0), BoxGroup(GroupID = "Stat")]
    public int visionRange = 8;

    [BoxGroup(GroupID = "AI")]
    public EntityCapacityAsset.EntityCapacityType[] visionTypes;
}
