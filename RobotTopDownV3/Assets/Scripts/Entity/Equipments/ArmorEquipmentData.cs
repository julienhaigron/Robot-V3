using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "ArmorData", menuName = "ScriptableObject/Equipment/ArmorData", order = 1)]
public class ArmorEquipmentData : EntityEquipmentData
{
    [BoxGroup(GroupID = "Stat")]
    public StatBonus[] statBonuses;

	public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		foreach (StatBonus bonus in statBonuses)
			description.Add(bonus.GetDescription());

		return description.ToArray();
	}
}
