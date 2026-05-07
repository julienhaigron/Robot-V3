using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PushOrPull", menuName = "ScriptableObject/PassiveEffect/PushOrPull")]
public class PushOrPullPassiveEffect : AEntityPassiveEffect
{
	public int movementStrength = 0;

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
				List<Tile> tilesInRange = GridManager.Instance.GetTilesInVisionRange(entityTargetted.Displacement.Coordinates.GetTile(), _effectContainer.effectRange, false, true);
				foreach (Tile tile in tilesInRange)
				{
					if (tile.GetEntity(true) != null)
						entitiesAffected.Add(tile.GetEntity(true));
				}
				break;
		}

		foreach (Entity targetEntity in entitiesAffected)
		{
			int direction = GridManager.Instance.GetClosestOrientation(_targetEntity.Displacement.Coordinates.GetTile(), _entity.Displacement.Coordinates.GetTile());
			if (movementStrength > 0)
				direction = (direction + 3) % 5;
			Tile destination = _targetEntity.Displacement.Coordinates.GetTile().Neighbors[direction];
			for (int i = 0; i < Mathf.Abs(movementStrength) - 1; i++)
			{
				destination = destination.Neighbors[direction];
			}
			TurnManager.InPlayEvent movementEvent = new();
			TurnManager.Instance.AddGameEvent(movementEvent);
			_targetEntity.Displacement.MoveToTile(destination.coordinates.ID, movementEvent.EndEvent, false);
		}

	}
}
