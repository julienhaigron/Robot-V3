using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "ScriptableObject/Status/BurnStatus")]
public class BurnStatus : AEntityStatus
{
    public int damageAmount = 1;

	public override void ApplyStatus ( Entity _entity )
	{
		base.ApplyStatus(_entity);

		Dictionary<WeaponEquipmentData.DamageType, int> damage = new();
		damage.Add(WeaponEquipmentData.DamageType.Feu, damageAmount);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damage, entityTargeted = _entity });
	}
}
