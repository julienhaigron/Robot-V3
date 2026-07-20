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
		List<Tile> tilesInRange = GridManager.Instance.GetTilesInVisionRange(_deadEntity.Displacement.Coordinates.GetTile(), explosionRange, false, true);

		foreach(Tile tile in tilesInRange)
		{
			if (tile.GetEntity(true) != null)
				tile.GetEntity(true).Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
				{
					critical = false,
					damages = damages,
					entityAttacker = _deadEntity,
					entityTargeted = tile.GetEntity(true),
					hitNormal = Vector3.zero,
					hitPos = Vector3.zero
				});
		}
	}
}
