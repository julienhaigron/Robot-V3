using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class TurnShieldAction : SpecialAction
{
	public int targetedOrientation = 0;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetedOrientation);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		targetedOrientation = GridManager.Instance.GetClosestOrientation(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], _tile);

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
		//todo : apply effect
		GameManager.Instance.GetEntityFromID(performingEntityID).Equipment.Tools[linkedEquipmentId].PerformAction(this, EndTick);
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
		return _tile != null && GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)].Neighbors.Contains(_tile);
	}
}
