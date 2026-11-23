using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "NeuronalMembrane", menuName = "ScriptableObject/Equipment/NeuronalMembrane", order = 1)]
public class NeuronalMembraneEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat"), Parsing("Equipment slot")]
    public int equipmentSlotAvailable = 2;
    /*[Min(0), BoxGroup(GroupID = "Stat"), Parsing("Vision range")]
    public int visionRange = 8;*/

    [BoxGroup(GroupID = "AI"), Parsing("Vision type")]
    public VisionTypes visionType;

    public enum VisionTypes
    {
        Optic,
        Thermic,
        Radar
    }

    public static SecondaryStat.StatType GetCamoStat ( VisionTypes _vision )
    {
        return _vision switch
        {
            VisionTypes.Thermic => SecondaryStat.StatType.ThermicCamo,
            VisionTypes.Radar => SecondaryStat.StatType.RadarCamo,
            _ => SecondaryStat.StatType.VisualCamo
        };
    }

    public static SecondaryStat.StatType GetPerceptionStat ( VisionTypes _vision )
    {
        return _vision switch
        {
            VisionTypes.Thermic => SecondaryStat.StatType.ThermicPerception,
            VisionTypes.Radar => SecondaryStat.StatType.RadarPerception,
            _ => SecondaryStat.StatType.VisualPerception
        };
    }

	public override StatDescription[] GetDesciption ()
    {
        List<StatDescription> description = base.GetDesciption().ToList();
        description.Add(new() { ID = SecondaryStat.StatType.EquipmentSlot, floatValue = equipmentSlotAvailable, stringValue = null });
        //description.Add(new() { ID = SecondaryStat.StatType.VisionRange, floatValue = visionRange, stringValue = visionRange + " C" });
        description.Add(new() { ID = SecondaryStat.StatType.VisionType, floatValue = 0, stringValue = visionType.GetLocalizedTitle() });

        return description.ToArray();
    }

	public override void OnParse ( ImportedData _data )
	{
		base.OnParse(_data);

	}
}
