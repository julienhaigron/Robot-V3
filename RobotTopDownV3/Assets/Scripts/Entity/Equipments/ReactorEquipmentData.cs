using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ReactorData", menuName = "ScriptableObject/Equipment/ReactorData", order = 1)]
public class ReactorEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat"), Parsing("Energy")]
    public int energyProduced = 160;

	public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		description.Add(new() { ID = StatBonus.StatType.EnergyProduced, title = "Energy Produced", floatValue = energyProduced, stringValue = energyProduced.ToString() });

        return description.ToArray();
	}
}
