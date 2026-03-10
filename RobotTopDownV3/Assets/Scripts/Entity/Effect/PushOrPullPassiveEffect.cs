using UnityEngine;

[CreateAssetMenu(fileName = "PushOrPull", menuName = "ScriptableObject/PassiveEffect/PushOrPull")]
public class PushOrPullPassiveEffect : AEntityPassiveEffect
{
	public int movementStrength = 0;

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity )
	{
		int direction = GridManager.Instance.GetClosestOrientation(_targetEntity.Displacement.Coordinates.GetTile(), _entity.Displacement.Coordinates.GetTile());
		if (movementStrength > 0)
			direction = (direction + 3) % 5;
		Tile destination = _targetEntity.Displacement.Coordinates.GetTile().Neighbors[direction];
		for(int i = 0; i < Mathf.Abs(movementStrength) - 1; i++)
		{
			destination = destination.Neighbors[direction];
		}

		_targetEntity.Displacement.MoveToTile(destination.coordinates.ID, null, true);
	}
}
