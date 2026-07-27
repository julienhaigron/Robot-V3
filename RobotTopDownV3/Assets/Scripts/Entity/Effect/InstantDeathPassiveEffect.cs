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
			case TargetType.CircleOnSelf:
			case TargetType.CircleOnTarget:
				Entity entityTargetted = _effectContainer.targetType == TargetType.CircleOnSelf ? _entity : _targetEntity;
				List<Tile> tilesInRange = GridManager.Instance.GetTilesInVisionRange(entityTargetted.Displacement.Coordinates.GetTile(), _effectContainer.effectRange.y, false, true);
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

		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}
}
