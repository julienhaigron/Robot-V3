using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "NeuronalMembrane", menuName = "ScriptableObject/Equipment/NeuronalMembrane", order = 1)]
public class NeuronalMembraneEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public int equipmentSlotAvailable = 2;
    [Min(0), BoxGroup(GroupID = "Stat")]
    public int visionRange = 8;

    [BoxGroup(GroupID = "AI")]
    public VisionTypes visionType;

    public enum VisionTypes
    {
        Optical,
        Thermal,
        Radar
    }

	public override StatDescription[] GetDesciption ()
    {
        List<StatDescription> description = base.GetDesciption().ToList();
        description.Add(new() { ID = StatBonus.StatType.EquipmentSlot, title = "Equipment Slots", floatValue = equipmentSlotAvailable, stringValue = null });
        description.Add(new() { ID = StatBonus.StatType.VisionRange, title = "Vision Range", floatValue = visionRange, stringValue = visionRange + " C" });
        description.Add(new() { ID = StatBonus.StatType.VisionType, title = "Vision Type", floatValue = 0, stringValue = visionType.ToString() });

        return description.ToArray();
    }
}
