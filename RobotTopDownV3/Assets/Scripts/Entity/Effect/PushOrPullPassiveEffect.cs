using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PushOrPull", menuName = "ScriptableObject/PassiveEffect/PushOrPull")]
public class PushOrPullPassiveEffect : AEntityPassiveEffect
{
	public int movementStrength = 0;

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		int direction = GridManager.Instance.GetClosestOrientation(_targetEntity.Displacement.Coordinates.GetTile(), _entity.Displacement.Coordinates.GetTile());
		if (movementStrength > 0)
			direction = (direction + 3) % 5;

		Tile destination = _targetEntity.Displacement.Coordinates.GetTile().Neighbors[direction];
		if (destination == null)
			return;
		
		for (int i = 0; i < Mathf.Abs(movementStrength) - 1; i++)
		{
			if (destination.Neighbors[direction] == null)
				break;
			destination = destination.Neighbors[direction];
			if (destination.GroundType == TileGroundType.Void)
				break;
		}
		TurnManager.RecordedEvent movementEvent = new();
		TurnManager.Instance.AddGameEvent(movementEvent);
		_targetEntity.Displacement.MoveToTile(destination.coordinates.ID, movementEvent.EndEvent, false);

		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}
}
