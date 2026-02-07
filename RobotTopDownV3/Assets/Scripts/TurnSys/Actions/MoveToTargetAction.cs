using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class MoveToTargetAction : AEntityAction
{
	public int targetEntiyID;
	public int targetTileID;
	public MoveActionMode mode;

	public int finalTargetTileID = -1;
	public int thisActionDestinationID = -1;
	public enum MoveActionMode { Coordinate, Entity }

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetEntiyID);
		serializer.SerializeValue(ref targetTileID);
		serializer.SerializeValue(ref mode);
		serializer.SerializeValue(ref finalTargetTileID);
		serializer.SerializeValue(ref thisActionDestinationID);
	}

	public override void Init ( EntityActionData _data, int _performingEntityID, int _positionAtActionStartID )
	{
		base.Init(_data, _performingEntityID, _positionAtActionStartID);

		switch (mode)
		{
			case MoveActionMode.Coordinate:
				finalTargetTileID = targetTileID;
				break;
			case MoveActionMode.Entity:
				finalTargetTileID = GameManager.Instance.GetEntityFromID(targetEntiyID).Displacement.Coordinates.ID;
				break;
		}

		positionAtActionEndID = (int)thisActionDestinationID;
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		//check here if can do movement and where to exactly
		RefreshDestinatedTile();

		GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);
	}

	public override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (thisActionDestinationID != -1/* && thisActionDestination.GetEntity(false) == null*/)
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.MoveToTile((int)thisActionDestinationID, EndPerform);
		else
			DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndPerform());
	}

	public override void EndPerform ()
	{
		base.EndPerform();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		int maxDistance = TurnManager.Instance.RemainingActionToken[performingEntityID];

		if (_tile.GetEntity(true) != null || _tile.IsObstacle() || GridManager.Instance.GetDistanceBetween(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], _tile, true) > maxDistance)
			return false;

		return true;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		//register all action for destination (calcuate dist, and add X actions in TurnSys (X = distance))
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> path = GridManager.Instance.GetPath(from, _tile, true);

		path.Reverse();
		for (int i = 0; i < path.Count-1; i++)
		{
			MoveToTargetAction action = new MoveToTargetAction();
			/*if (i == 0)
				action = this;*/
			
			if (mode == MoveActionMode.Coordinate)
				action.targetTileID = _tile.coordinates.ID;
			else if (mode == MoveActionMode.Entity)
				action.targetEntiyID = _tile.GetEntity(true).ID;
			action.thisActionDestinationID = path[i + 1].coordinates.ID;
			action.mode = mode;

			action.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.TargetTileMove], performingEntityID, path[i].coordinates.ID);

			TurnManager.Instance.AddAction(performingEntityID, action, TurnManager.Instance.CurrentStateTypeSelected);
		}
		TurnManager.Instance.RefreshActionDisplay(performingEntityID);
	}

	public override bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		if (finalTargetTileID == -1)
		{
			//entity move action canceled
			if (performingEntity.Displacement.Coordinates.GetTile().GetEntity(false) != null)
				Debug.LogError("CRITICAL ERROR : performing entity " + performingEntity.Data.name + " cant go back to where it was. Hope this never happens"); // solution? insta kill performing entity
			else
				performingEntity.Displacement.Coordinates.GetTile().SetEntity(performingEntity, _isThisTurn: false);
			return false;
		}

		bool hasConflict = false;

		if (_otherAction is MoveToNeighborAction _otherNeighborMoveAction && _otherNeighborMoveAction.finalTargetTileID == thisActionDestinationID)
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				_otherNeighborMoveAction.finalTargetTileID = -1;
			}
			else
			{
				hasConflict = true;
				thisActionDestinationID = -1;
			}
		}
		else if (_otherAction is MoveToTargetAction _otherMoveToTargetAction && _otherMoveToTargetAction.thisActionDestinationID == thisActionDestinationID)
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				_otherMoveToTargetAction.thisActionDestinationID = -1;
			}
			else
			{
				hasConflict = true;
				thisActionDestinationID = -1;
			}
		}
		else if (IsDestinationOccupiedOnNextTurnAction())
		{
			if (_isCheck)
				hasConflict = true;
			else
			{
				RefreshDestinatedTile();
				if (finalTargetTileID == -1)
					hasConflict = true;
			}
		}
		else if (thisActionDestinationID != -1 && GridManager.Instance.GetDistanceBetween(performingEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[(int)thisActionDestinationID], false) > 1)
		{
			//check if tile too far
			hasConflict = true;
			RefreshDestinatedTile();
		}
		else if (thisActionDestinationID == -1)
		{
			hasConflict = true;
			RefreshDestinatedTile();
		}

		if (hasConflict == false)
		{
			GridManager.Instance.Tiles[(int)thisActionDestinationID].SetEntity(performingEntity, _isThisTurn: false);
		}

		return hasConflict;
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (thisActionDestinationID == -1)
			return false;

		Entity entityOnDestination = GridManager.Instance.Tiles[(int)thisActionDestinationID].GetEntity(_isThisTurn: false);

		return (entityOnDestination != null && entityOnDestination.ID != performingEntityID) || GridManager.Instance.Tiles[(int)thisActionDestinationID].IsObstacle();
	}

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTileID == -1)
			return;

		List<Tile> pathToTile = GridManager.Instance.GetPath(GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[(int)finalTargetTileID], _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count < 2)
		{
			finalTargetTileID = -1;
			return;
		}

		pathToTile.Reverse();
		thisActionDestinationID = pathToTile[1].coordinates.ID;
		positionAtActionEndID = pathToTile[1].coordinates.ID;
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
		Vector3 startPos = GridManager.Instance.Tiles[supposedPositionAtActionStartID].transform.position;
		Vector3 destination = GridManager.Instance.Tiles[(int)thisActionDestinationID].transform.position;
		Vector3 position = Vector3.Lerp(startPos, destination, .5f);
		arrow.Init(_recordedAction);
		arrow.transform.position = position;
		arrow.transform.LookAt(GridManager.Instance.Tiles[(int)thisActionDestinationID].transform);

		PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, false);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		Tile from = GridManager.Instance.Tiles[(int)TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> pathToTile = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[(int)positionAtActionEndID], _isThisTurn: false);
		pathToTile.Reverse();

		for(int i = 0; i < pathToTile.Count - 1; i++)
		{
			ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
			Vector3 startPos = pathToTile[i].transform.position;
			Vector3 destination = pathToTile[i+1].transform.position;
			Vector3 position = Vector3.Lerp(startPos, destination, .5f);
			arrow.SetMaterial(GameAssets.current.ui.ghostEntityStateMaterials[_state]);
			arrow.transform.position = position;
			arrow.transform.LookAt(pathToTile[i + 1].transform);

			PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, true);
		}
	}
}
