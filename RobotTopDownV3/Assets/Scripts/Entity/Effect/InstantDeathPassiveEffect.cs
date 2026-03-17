using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstantDeath", menuName = "ScriptableObject/PassiveEffect/InstantDeath")]
public class InstantDeathPassiveEffect : AEntityPassiveEffect
{
	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		List<Entity> entitiesAffected = new();
		switch (_effectContainer.targetType)
		{
			case TargetType.Self:
				entitiesAffected.Add(_entity);
				break;
			case TargetType.OtherEntity:
				entitiesAffected.Add(_targetEntity);
				break;
			case TargetType.ConeOnSelf:
			case TargetType.ConeOnTarget:
				Entity entityTargetted = _effectContainer.targetType == TargetType.ConeOnSelf ? _entity : _targetEntity;
				List<Tile> tilesInRange = GridManager.Instance.GetTilesInVisionRange(entityTargetted.Displacement.Coordinates.GetTile(), _effectContainer.effectRange, true);
				foreach (Tile tile in tilesInRange)
				{
					if (tile.GetEntity(true) != null)
						entitiesAffected.Add(tile.GetEntity(true));
				}
				break;
		}
		foreach (Entity targetEntity in entitiesAffected)
		{
			targetEntity.Equipment.InstantDeath();
		}
	}
}
