using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/Equipment/WeaponData", order = 1)]
public class WeaponEquipmentData : EntityEquipmentData
{
    public Weapon prefab;

    //public int accuracy;
    public SerializableDictionary<DamageType, int> baseDamages;
    public int visionConeRange;
    public enum DistanceType
	{
        Close,
        Mid,
        Long
	}
    //public SerializableDictionary<DistanceType, float> distanceAccuracyBonus;

    //animation
    public string attackAnimationSuccessId;
    public string attackAnimationFailureId;
    public bool isTwoHanded = false;
    public float singleAttackAnimationDuration;

    public enum DamageType
	{
        Tranchant,
        Perforant,
        Contendant,
        Laser,
        Plasma,
        Feu,
        Radiation,
        Electrique,
        Magnetique
	}

    public enum DamageCategory
	{
        Physic,
        Elemental
	}

	public override StatDescription[] GetDesciption ()
    {
        List<StatDescription> description = base.GetDesciption().ToList();
        string value = "";
        int count = 0;
        foreach (KeyValuePair<DamageType, int> pair in baseDamages)
		{
            value += pair.Key.ToString() + ": " + pair.Value;
            if (count + 1 < baseDamages.Keys.Count)
                value += ", ";

            count++;
        }

        description.Add(new() { ID = SecondaryStat.StatType.BaseDamage, title = "BaseDamage", floatValue = 0, stringValue = value });

		return description.ToArray();
	}

}
