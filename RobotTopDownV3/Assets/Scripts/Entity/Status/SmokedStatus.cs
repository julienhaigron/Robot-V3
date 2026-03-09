using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Smoked", menuName = "ScriptableObject/Status/Smoked")]
public class SmokedStatus : AEntityStatus
{

	public override void ApplyStatusEffect ( Entity _entity )
	{
		base.ApplyStatusEffect(_entity);

		/*Dictionary<WeaponEquipmentData.DamageType, int> damage = new();
		damage.Add(WeaponEquipmentData.DamageType.Feu, damageAmount);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damage, entityTargeted = _entity });*/
	}

	/*public override void ApplyStatus ( Tile _tile )
	{
		base.ApplyStatus(_tile);
	}*/
}
