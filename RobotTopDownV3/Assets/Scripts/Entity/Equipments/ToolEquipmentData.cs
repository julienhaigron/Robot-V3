using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "ToolData", menuName = "ScriptableObject/Equipment/ToolData", order = 1)]
public class ToolEquipmentData : EntityEquipmentData
{
    public Tool prefab;

    //stat
    public int range = 0;

    //animation
    public string attackAnimationSuccessId;
    public string attackAnimationFailureId;
    public bool isTwoHanded = false;

	/*public override StatDescription[] GetDesciption ()
	{
		List<StatDescription> description = base.GetDesciption().ToList();
		*//*foreach (StatBonus bonus in statBonuses)
		{
			description.Add(new() { title = bonus.type.ToString(), floatValue = bonus.value, stringValue = bonus.})
		}*//*

		return description.ToArray();
	}*/
}
