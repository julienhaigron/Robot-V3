using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DealsCircleDamage", menuName = "ScriptableObject/PassiveEffect/DealsCircleDamage")]
public class DealCircleDamagePassiveEffect : AEntityPassiveEffect
{
	public int explosionRange = 1;
	public SerializableDictionary<WeaponEquipmentData.DamageType, int> damages = new();

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		EntityEquipmentPlugin.TakeDamageCallback damageCallback = new EntityEquipmentPlugin.TakeDamageCallback()
		{
			critical = false,
			damages = damages,
			entityAttacker = _entity,
			entityTargeted = null,
			hitNormal = Vector3.zero,
			hitPos = Vector3.zero
		};
		_targetEntity.Equipment.TakeDamage(damageCallback);

		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}
}
