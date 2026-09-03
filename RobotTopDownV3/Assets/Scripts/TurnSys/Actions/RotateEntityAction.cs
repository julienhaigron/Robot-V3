using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class RotateEntityAction : AEntityAction
{
	public int[] targetedOrientationID; //0 - 5

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetedOrientationID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		targetTileIDs = new int[1];
		targetTileIDs[0] = _tile.coordinates.ID;
		int orientation = GridManager.Instance.GetClosestOrientation(PerformingEntity.Displacement.Coordinates.GetTile(), _tile);
		targetedOrientationID = new int[1]{ orientation };

		base.RegisterInteraction(_tile);
	}

	public override void SetResolvedTargets ( int[] _targetTileIDs, int[] _targetedEntityIDs )
	{
		base.SetResolvedTargets(_targetTileIDs, _targetedEntityIDs);

		if (targetedOrientationID != null && targetedOrientationID.Length > 0)
			return;

		if (_targetTileIDs == null || _targetTileIDs.Length == 0)
			return;

		Tile from = PerformingEntity.Displacement.Coordinates.GetTile();
		targetedOrientationID = new int[1] { GridManager.Instance.GetClosestOrientation(from, GridManager.Instance.Tiles[_targetTileIDs[0]]) };
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
		if(targetedOrientationID == null || targetedOrientationID.Length < lifetime)
		{
			//shouldnt happen
			EndTick();
		}
		else
		{
			PerformingEntity.Displacement.Rotate(targetedOrientationID[lifetime], GameConfig.current.game.entityRotationDuration, EndTick);
		}

		base.Perform(_state);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//display rotation change on ground
		
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (targetTileIDs == null || targetTileIDs.Length == 0)
			return;

		Vector3 previousPosition = GridManager.Instance.Tiles[supposedPositionAtActionStartID].transform.position;
		RotationActionDisplay display = ObjectsPooling.GetElement(GameAssets.current.game.rotationHandlePoolData) as RotationActionDisplay;
		Vector3 startPos = previousPosition;
		Vector3 destination = GridManager.Instance.Tiles[targetTileIDs[0]].transform.position;
		display.Init(this, _state);
		display.transform.position = startPos;
		display.transform.LookAt(destination);

		PlayerController.Instance.AddRotationActionDisplay(display, performingEntityID);
	}

	public override void OnSelectActionTileInteractPredicatePrewarm ()
	{
		base.OnSelectActionTileInteractPredicatePrewarm();
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		GridManager.Instance.BFS(from, 1, null, true, false);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (_tile.Distance == 1)
			return true;

		return false;
	}
}
