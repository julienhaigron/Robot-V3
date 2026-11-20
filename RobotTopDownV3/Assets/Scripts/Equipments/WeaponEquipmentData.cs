using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObject/Equipment/WeaponData", order = 1)]
public class WeaponEquipmentData : EntityEquipmentData
{
    public Weapon prefab;

    //public int accuracy;
    //public SerializableDictionary<DamageType, int> baseDamages;
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
    public float singleAttackAnimationDuration = .3f;
    [Tooltip("Garde de l'unité au repos avec cette arme : l'Idle des états « Guard <nom> R » et « Guard <nom> L » du contrôleur (Axe, Boxing, Torch, Throw), en miroir dans l'autre main. Vide = repos neutre. L'unité revient au repos neutre pour marcher.")]
    public string guardAnimation = "";
    [Tooltip("Rotation du buste en garde, en degrés, l'arme tenue dans la main principale : elle tourne le buste et les bras sans bouger la tête ni les jambes. Dans l'autre main, l'angle opposé.")]
    [Range(-60f, 60f)] public float guardTorsoYaw = 0f;
    [Tooltip("Pose de visée pendant les tirs : les états « Aim <nom> R » et « Aim <nom> L » du calque d'attaque (Cannon), en miroir dans l'autre main. L'IK oriente ensuite le bras vers la cible. Vide = le bras se lève par IK seulement.")]
    public string aimAnimation = "";

    public enum DamageType
	{
        Slash,
        Piercing,
        Bludgeoning,
        Laser,
        Plasma,
        Fire,
        Radiation,
        Electric,
        Magnetic
	}

    public enum DamageCategory
	{
        Physic,
        Elemental
	}

	/*public override StatDescription[] GetDesciption ()
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

        description.Add(new() { ID = SecondaryStat.StatType.BaseDamage, floatValue = 0, stringValue = value });

		return description.ToArray();
	}*/

}
