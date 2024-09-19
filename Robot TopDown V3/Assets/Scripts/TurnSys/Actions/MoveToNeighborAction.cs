using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MoveToNeighborAction : AEntityAction
{
	public Entity targetEntiy;
	public Tile targetTile;
	public MoveActionMode mode;

	public Tile finalTargetTile;

	public enum MoveActionMode { Coordinate, Entity }

	public override void Init ( EntityActionData _data, Entity _performingEntity, Tile _positionAtActionStart )
	{
		base.Init(_data, _performingEntity, _positionAtActionStart);

		if (mode == MoveActionMode.Coordinate)
			positionAtActionEnd = targetTile;
		else if (mode == MoveActionMode.Entity)
			positionAtActionEnd = targetEntiy.Displacement.Coordinates.GetTile();
	}

	public override EntityActionEnum Prepare ( Entity.EntityState _state )
	{
		performingEntity.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);

		switch (mode)
		{
			case MoveActionMode.Coordinate:
				finalTargetTile = targetTile;
				break;
			case MoveActionMode.Entity:
				finalTargetTile = targetEntiy.Displacement.Coordinates.GetTile();
				break;
		}

		return type;
	}

	public override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (finalTargetTile != null/* && finalTargetTile.GetEntity(false) == null*/)
			performingEntity.Displacement.MoveToTile(finalTargetTile, EndPerform);
		else
			DG.Tweening.DOVirtual.DelayedCall(.5f, () => EndPerform());
	}

	public override void EndPerform ()
	{
		base.EndPerform();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (_tile.IsObstacle() || GridManager.Instance.GetDistance(TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntity), _tile) != 1)
			return false;

		return true;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		if (mode == MoveActionMode.Coordinate)
			targetTile = _tile;
		else if (mode == MoveActionMode.Entity)
			targetEntiy = _tile.GetEntity(true);

		positionAtActionEnd = _tile;

		base.RegisterInteraction(_tile);
	}

	public override bool CheckConflict ( AEntityAction _otherAction , bool _isCheck = true )
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

		if (_otherAction is MoveToNeighborAction && (_otherAction as MoveToNeighborAction).finalTargetTile == finalTargetTile)
		{
			hasConflict = true;

			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				//finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);
				//(_otherAction as MoveAction).performingEntity.Displacement.Coordinates.GetTile().SetEntity((_otherAction as MoveAction).performingEntity, _isThisTurn: false);
				(_otherAction as MoveToNeighborAction).finalTargetTile = null;
			}
			else
			{
				//(_otherAction as MoveAction).finalTargetTile.SetEntity(_otherAction.performingEntity, _isThisTurn: false);				
				finalTargetTile = null;
			}
		}
		else if (_otherAction is MoveToTargetAction && (_otherAction as MoveToTargetAction).thisActionDestination == finalTargetTile)
		{
			hasConflict = true;

			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				(_otherAction as MoveToTargetAction).thisActionDestination = null;
			}
			else
			{
				finalTargetTile = null;
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
		else if (finalTargetTile != null && GridManager.Instance.GetDistanceBetween(performingEntity.Displacement.Coordinates.GetTile(), finalTargetTile, false) > 1)
		{
			hasConflict = true;
			RefreshDestinatedTile();
		}

		if (hasConflict == false)
		{
			finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);
		}

		return hasConflict;
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (finalTargetTile == null)
			return false;

		Entity entityOnDestination = finalTargetTile.GetEntity(_isThisTurn: false);

		return (entityOnDestination != null && entityOnDestination != performingEntity) || finalTargetTile.IsObstacle();
	}

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTile == null)
			return;

		List<Tile> pathToTile = GridManager.Instance.GetPath(performingEntity.Displacement.Coordinates.GetTile(), finalTargetTile, _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count < 2 || pathToTile[^2] == performingEntity.Displacement.Coordinates.GetTile())
		{
			finalTargetTile = null;
			return;
		}

		finalTargetTile = pathToTile[^2];
	}

	public override void Display ()
	{
		Arrow arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as Arrow;
		Vector3 startPos = supposedPositionAtActionStart.transform.position;
		Vector3 destination = positionAtActionEnd.transform.position;
		Vector3 position = Vector3.Lerp(startPos, destination, .5f);
		arrow.transform.position = position;
		arrow.transform.LookAt(positionAtActionEnd.transform);

		PlayerController.Instance.arrows.Add(arrow);
	}
}
