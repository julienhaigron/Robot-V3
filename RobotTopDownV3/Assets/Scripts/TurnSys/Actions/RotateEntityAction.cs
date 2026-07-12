using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class RotateEntityAction : AEntityAction
{
	public int targetedOrientationID = -1; //0 - 5

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetedOrientationID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		targetedOrientationID = GridManager.Instance.GetClosestOrientation(PerformingEntity.Displacement.Coordinates.GetTile(), _tile);

		base.RegisterInteraction(_tile);
	}

	public override void Prepare ( Entity.EntityState _state )
	{

	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if(targetedOrientationID == -1)
		{
			//shouldnt happen
			EndTick();
		}
		else
		{
			PerformingEntity.Displacement.Rotate(targetedOrientationID, GameConfig.current.game.entityRotationDuration, EndTick);
		}

		base.Perform(_state);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (_tile.Distance == 1)
			return true;

		return false;
	}
}
