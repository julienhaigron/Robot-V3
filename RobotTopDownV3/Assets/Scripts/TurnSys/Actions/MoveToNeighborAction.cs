using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class MoveToNeighborAction : AEntityAction
{
	public int targetEntityID;
	public int targetTileID;
	public MoveActionMode mode;

	public int finalTargetTileID = -1;

	public enum MoveActionMode { Coordinate, Entity }

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetEntityID);
		serializer.SerializeValue(ref targetTileID);
		serializer.SerializeValue(ref mode);
		serializer.SerializeValue(ref finalTargetTileID);
	}

	public override void Init ( EntityActionData _data, int _performingEntityID, int _positionAtActionStartID )
	{
		base.Init(_data, _performingEntityID, _positionAtActionStartID);

		if (mode == MoveActionMode.Coordinate)
			positionAtActionEndID = targetTileID;
		else if (mode == MoveActionMode.Entity)
			positionAtActionEndID = GameManager.Instance.GetEntityFromID(targetEntityID).Displacement.Coordinates.ID;
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);

		switch (mode)
		{
			case MoveActionMode.Coordinate:
				finalTargetTileID = targetTileID;
				break;
			case MoveActionMode.Entity:
				finalTargetTileID = GameManager.Instance.GetEntityFromID(targetEntityID).Displacement.Coordinates.ID;
				break;
		}
	}

	public override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (finalTargetTileID != -1/* && finalTargetTile.GetEntity(false) == null*/)
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.MoveToTile((int)finalTargetTileID, EndPerform);
		else
			DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndPerform());
	}

	public override void EndPerform ()
	{ 
		base.EndPerform();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (_tile.IsObstacle() || GridManager.Instance.GetDistanceBetween(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], _tile, true) != 1)
			return false;

		return true;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		if (mode == MoveActionMode.Coordinate)
			targetTileID = _tile.coordinates.ID;
		else if (mode == MoveActionMode.Entity)
			targetEntityID = _tile.GetEntity(true).ID;

		positionAtActionEndID = _tile.coordinates.ID;

		base.RegisterInteraction(_tile);
	}

	public override bool CheckConflict ( AEntityAction _otherAction , bool _isCheck = true )
	{
		if (finalTargetTileID == -1)
		{
			//entity move action canceled
			if (GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().GetEntity(false) != null)
				Debug.LogError("CRITICAL ERROR : performing entity " + GameManager.Instance.GetEntityFromID(performingEntityID).Data.name + " cant go back to where it was. Hope this never happens"); // solution? insta kill performing entity
			else
				GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(GameManager.Instance.GetEntityFromID(performingEntityID), _isThisTurn: false);
			return false;
		}

		bool hasConflict = false;

		if (_otherAction is MoveToNeighborAction && (_otherAction as MoveToNeighborAction).finalTargetTileID == finalTargetTileID)
		{
			hasConflict = true;

			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				//finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);
				//(_otherAction as MoveAction).performingEntity.Displacement.Coordinates.GetTile().SetEntity((_otherAction as MoveAction).performingEntity, _isThisTurn: false);
				(_otherAction as MoveToNeighborAction).finalTargetTileID = -1;
			}
			else
			{
				//(_otherAction as MoveAction).finalTargetTile.SetEntity(_otherAction.performingEntity, _isThisTurn: false);				
				finalTargetTileID = -1;
			}
		}
		else if (_otherAction is MoveToTargetAction && (_otherAction as MoveToTargetAction).thisActionDestinationID == finalTargetTileID)
		{
			hasConflict = true;

			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				(_otherAction as MoveToTargetAction).thisActionDestinationID = -1;
			}
			else
			{
				finalTargetTileID = -1;
			}
		}
		else if (IsDestinationOccupiedOnNextTurnAction())
		{
			hasConflict = true;
			RefreshDestinatedTile();

			/*if (finalTargetTile != null)
				finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);*/
		}
		//check if tile too far
		else if (finalTargetTileID != -1 && GridManager.Instance.GetDistanceBetween(GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[(int)finalTargetTileID], false) > 1)
		{
			hasConflict = true;
			RefreshDestinatedTile();
		}

		if (hasConflict == false)
		{
			GridManager.Instance.Tiles[(int)finalTargetTileID].SetEntity(GameManager.Instance.GetEntityFromID(performingEntityID), _isThisTurn: false);
		}

		return hasConflict;
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (finalTargetTileID == -1)
			return false;

		Entity entityOnDestination = GridManager.Instance.Tiles[(int)finalTargetTileID].GetEntity(_isThisTurn: false);

		return (entityOnDestination != null && entityOnDestination.ID != performingEntityID) || GridManager.Instance.Tiles[(int)finalTargetTileID].IsObstacle();
	}

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTileID == -1)
			return;

		List<Tile> pathToTile = GridManager.Instance.GetPath(GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[(int)finalTargetTileID], _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count < 2 || pathToTile[^2] == GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile())
		{
			finalTargetTileID = -1;
			return;
		}

		finalTargetTileID = pathToTile[^2].coordinates.ID;
	}

	public override void Display ()
	{
		Arrow arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as Arrow;
		Vector3 startPos = GridManager.Instance.Tiles[supposedPositionAtActionStartID].transform.position;
		Vector3 destination = GridManager.Instance.Tiles[positionAtActionEndID].transform.position;
		Vector3 position = Vector3.Lerp(startPos, destination, .5f);
		arrow.transform.position = position;
		arrow.transform.LookAt(GridManager.Instance.Tiles[positionAtActionEndID].transform);

		PlayerController.Instance.arrows.Add(arrow);
	}
}
