using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageOnDeath", menuName = "ScriptableObject/PassiveEffect/DamageOnDeath")]
public class DamageOnDeathPassiveEffect : AEntityPassiveEffect
{
	public int explosionRange = 1;
	public SerializableDictionary<WeaponEquipmentData.DamageType, int> damages = new();

	/*public override void ApplyEffect ( Entity _entity, Entity _targetEntity )
	{


	}*/

	public override void OnDeathTrigger ( Entity _deadEntity )
	{
		base.OnDeathTrigger(_deadEntity);
		List<Tile> tilesInRange = GridManager.Instance.GetTilesInVisionRange(_deadEntity.Displacement.Coordinates.GetTile(), 1, explosionRange, false, true, true);

		foreach(Tile tile in tilesInRange)
		{
			Entity victim = tile.GetCurrentEntity();
			if (victim == null || victim == _deadEntity || victim.Equipment.IsDead)
				continue;

			victim.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
			{
				critical = false,
				damages = victim.Equipment.GetDamagesAfterResistances(damages),
				entityAttacker = _deadEntity,
				entityTargeted = victim,
				hitNormal = Vector3.zero,
				hitPos = Vector3.zero
			});
		}
	}
}
