using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstantDeath", menuName = "ScriptableObject/PassiveEffect/InstantDeath")]
public class InstantDeathPassiveEffect : AEntityPassiveEffect
{
	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		_targetEntity.Equipment.InstantDeath();
		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}

	/*OLD :
	 
	switch (_effectContainer.targetType)
		{
			case EntityActionData.TargetType.Self:
				entitiesAffected.Add(_entity);
				break;
			case EntityActionData.TargetType.OtherEntity:
			case EntityActionData.TargetType.Tile:
				if (_effectContainer.aoeType == EntityActionData.AOEType.Noone)
					entitiesAffected.Add(_targetEntity);
				else
				{
					Tile from = _effectContainer.centerType == EntityActionData.AOECenterType.Self ? _entity.Displacement.Coordinates.GetTile() : _targetEntity.Displacement.Coordinates.GetTile();
					List<Tile> tilesInRange = GridManager.Instance.GetTilesInAoERange
						(_effectContainer.aoeType, _entity, from, null, _effectContainer.effectRange.x, _effectContainer.effectRange.y, 0, true);
					foreach (Tile tile in tilesInRange)
					{
						if (tile.GetEntity(true) != null)
							entitiesAffected.Add(tile.GetEntity(true));
					}
				}
				break;
		}
		foreach (Entity targetEntity in entitiesAffected)
		{
			targetEntity.Equipment.InstantDeath();
		}
	 
	 */
}
