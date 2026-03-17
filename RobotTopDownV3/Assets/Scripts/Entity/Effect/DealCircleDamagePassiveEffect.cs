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
		switch (_effectContainer.targetType)
		{
			case TargetType.ConeOnSelf:
			case TargetType.ConeOnTarget:
				Entity entityTargetted = _effectContainer.targetType == TargetType.ConeOnSelf ? _entity : _targetEntity;
				List <Tile> tilesInRange = GridManager.Instance.GetTilesInVisionRange(entityTargetted.Displacement.Coordinates.GetTile(), explosionRange, true);
				foreach (Tile tile in tilesInRange)
				{
					if (tile.GetEntity(true) != null)
						tile.GetEntity(true).Equipment.TakeDamage(damageCallback);
				}
				break;
			case TargetType.Self:
				_entity.Equipment.TakeDamage(damageCallback);
				break;
			case TargetType.OtherEntity:
				_targetEntity.Equipment.TakeDamage(damageCallback);
				break;
		}
	}
}
