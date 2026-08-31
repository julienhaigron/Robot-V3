using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "OccultorData", menuName = "ScriptableObject/Equipment/OccultorData", order = 1)]
public class OccultorEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat"), Parsing("Radar camo")]
    public float soundCamo = .3f;
    [BoxGroup(GroupID = "Stat"), Parsing("Optic camo")]
    public float visualCamo = .2f;
    [BoxGroup(GroupID = "Stat"), Parsing("Thermic camo")]
    public float thermicCamo = .2f;
    [BoxGroup(GroupID = "Stat")]
    public SecondaryStat[] statBonuses;

	public override StatDescription[] GetDesciption ()
    {
        List<StatDescription> description = base.GetDesciption().ToList();
        description.Add(new() { ID = SecondaryStat.StatType.RadarCamo, floatValue = soundCamo, stringValue = (soundCamo *100f) + " %" });
        description.Add(new() { ID = SecondaryStat.StatType.VisualCamo, floatValue = visualCamo, stringValue = (visualCamo * 100f) + " %" });
        description.Add(new() { ID = SecondaryStat.StatType.ThermicCamo, floatValue = thermicCamo, stringValue = (thermicCamo * 100f) + " %" });
        foreach (SecondaryStat bonus in statBonuses)
            description.Add(bonus.GetDescription());

        return description.ToArray();
	}
}
