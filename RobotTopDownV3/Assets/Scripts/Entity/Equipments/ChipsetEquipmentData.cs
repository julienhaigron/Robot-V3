using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "ChipsetData", menuName = "ScriptableObject/Equipment/ChipsetData", order = 1)]
public class ChipsetEquipmentData : EntityEquipmentData
{
	[BoxGroup(GroupID = "Stat"), Parsing("ConditionalStat")]
	public ConditionalStatBonus[] statBonuses;


	[System.Serializable, ShowOdinSerializedPropertiesInInspector]
	public class ConditionalStatBonus
	{
		public Condition.ConditionType conditionType;
		public SecondaryStat bonus;
	}

	public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		foreach (ConditionalStatBonus bonus in statBonuses)
		{
			description.Add(bonus.bonus.GetDescription());
		}

		return description.ToArray();
	}
}
