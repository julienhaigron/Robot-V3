using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MoveToTargetAction : AEntityAction
{
	public Entity targetEntiy;
	public Tile targetTile;
	public MoveActionMode mode;

	public Tile finalTargetTile;
	public Tile thisActionDestination;
	public enum MoveActionMode { Coordinate, Entity }

	public override void Init ( EntityActionData _data, Entity _performingEntity, Tile _positionAtActionStart )
	{
		base.Init(_data, _performingEntity, _positionAtActionStart);

		switch (mode)
		{
			case MoveActionMode.Coordinate:
				finalTargetTile = targetTile;
				break;
			case MoveActionMode.Entity:
				finalTargetTile = targetEntiy.Displacement.Coordinates.GetTile();
				break;
		}

		positionAtActionEnd = thisActionDestination;
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		performingEntity.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);
	}

	public override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (finalTargetTile != null/* && finalTargetTile.GetEntity(false) == null*/)
			performingEntity.Displacement.MoveToTile(thisActionDestination, EndPerform);
		else
			DG.Tweening.DOVirtual.DelayedCall(.5f, () => EndPerform());
	}

	public override void EndPerform ()
	{
		base.EndPerform();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		int maxDistance = TurnManager.Instance.RemainingActionToken[performingEntity];

		if (_tile.IsObstacle() || GridManager.Instance.GetDistance(TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntity), _tile) > maxDistance)
			return false;

		return true;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		//register all action for destination (calcuate dist, and add X actions in TurnSys (X = distance))

		List<Tile> path = GridManager.Instance.GetPath(performingEntity.Displacement.Coordinates.GetTile(), _tile, true);
		path.Reverse();
		for (int i = 0; i < path.Count-1; i++)
		{
			MoveToTargetAction action = new MoveToTargetAction();
			if (i == 0)
				action = this;
			
			if (mode == MoveActionMode.Coordinate)
				action.targetTile = _tile;
			else if (mode == MoveActionMode.Entity)
				action.targetEntiy = _tile.GetEntity(true);
			action.thisActionDestination = path[i + 1];
			action.mode = mode;

			action.Init(GameAssets.current.game.entityActionsData[EntityActionType.TargetTileMove], performingEntity, path[i]);

			TurnManager.Instance.AddAction(performingEntity, action);
		}
		TurnManager.Instance.RefreshActionDisplay(performingEntity);
	}

	public override bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (finalTargetTile == null)
		{
			//entity move action canceled
			if (performingEntity.Displacement.Coordinates.GetTile().GetEntity(false) != null)
				Debug.LogError("CRITICAL ERROR : performing entity " + performingEntity.Data.name + " cant go back to where it was. Hope this never happens"); // solution? insta kill performing entity
			else
				performingEntity.Displacement.Coordinates.GetTile().SetEntity(performingEntity, _isThisTurn: false);
			return false;
		}

		bool hasConflict = false;

		if (_otherAction is MoveToNeighborAction _otherNeighborMoveAction && _otherNeighborMoveAction.finalTargetTile == thisActionDestination)
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				_otherNeighborMoveAction.finalTargetTile = null;
			}
			else
			{
				hasConflict = true;
				thisActionDestination = null;
			}
		}
		else if (_otherAction is MoveToTargetAction _otherMoveToTargetAction && _otherMoveToTargetAction.thisActionDestination == thisActionDestination)
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				_otherMoveToTargetAction.thisActionDestination = null;
			}
			else
			{
				hasConflict = true;
				thisActionDestination = null;
			}
		}
		else if (IsDestinationOccupiedOnNextTurnAction())
		{
			if (_isCheck)
				hasConflict = true;
			else
			{
				RefreshDestinatedTile();
				if (finalTargetTile == null)
					hasConflict = true;
			}
		}
		else if (thisActionDestination != null && GridManager.Instance.GetDistanceBetween(performingEntity.Displacement.Coordinates.GetTile(), thisActionDestination, false) > 1)
		{
			//check if tile too far
			hasConflict = true;
			RefreshDestinatedTile();
		}
		else if (thisActionDestination == null)
		{
			hasConflict = true;
			RefreshDestinatedTile();
		}

		if (hasConflict == false)
		{
			thisActionDestination.SetEntity(performingEntity, _isThisTurn: false);
		}

		return hasConflict;
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (thisActionDestination == null)
			return false;

		Entity entityOnDestination = thisActionDestination.GetEntity(_isThisTurn: false);

		return (entityOnDestination != null && entityOnDestination != performingEntity) || thisActionDestination.IsObstacle();
	}

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTile == null)
			return;

		List<Tile> pathToTile = GridManager.Instance.GetPath(performingEntity.Displacement.Coordinates.GetTile(), finalTargetTile, _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count < 2)
		{
			finalTargetTile = null;
			return;
		}

		pathToTile.Reverse();
		thisActionDestination = pathToTile[1];
		positionAtActionEnd = thisActionDestination;
	}

	public override void Display ()
	{
		Arrow arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as Arrow;
		Vector3 startPos = supposedPositionAtActionStart.transform.position;
		Vector3 destination = thisActionDestination.transform.position;
		Vector3 position = Vector3.Lerp(startPos, destination, .5f);
		arrow.transform.position = position;
		arrow.transform.LookAt(thisActionDestination.transform);

		PlayerController.Instance.arrows.Add(arrow);
	}
}
