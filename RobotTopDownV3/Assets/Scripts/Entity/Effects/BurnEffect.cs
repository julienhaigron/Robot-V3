using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "ScriptableObject/Effect/BurnEffect")]
public class BurnEffect : AEntityEffect
{
    public int damageAmount = 1;

	public override void ApplyEffect ( Entity _entity )
	{
		base.ApplyEffect(_entity);

		Dictionary<WeaponEquipmentData.DamageType, int> damage = new();
		damage.Add(WeaponEquipmentData.DamageType.Feu, damageAmount);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damage, entityTargeted = _entity });
	}
}
