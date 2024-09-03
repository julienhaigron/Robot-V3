using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MoveAction : AEntityAction
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
		else if(mode == MoveActionMode.Entity)
			positionAtActionEnd = targetEntiy.Displacement.Coordinates.GetTile();
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		base.Prepare(_state);

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
		else if(mode == MoveActionMode.Entity)
			targetEntiy = _tile.GetEntity(true);

		positionAtActionEnd = _tile;
	}

	public override bool CheckConflict ( AEntityAction _otherAction )
	{
		if(finalTargetTile == null)
		{
			return false;
		}

		bool hasConflict = false;
		if (_otherAction is MoveAction)
		{
			if ((_otherAction as MoveAction).finalTargetTile == finalTargetTile)
			{
				hasConflict = true;

				int roll = UnityEngine.Random.Range((int)0, 2);
				if (roll == 0)
				{
					//performing entity wins roll
					finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);
					(_otherAction as MoveAction).finalTargetTile = null;
					(_otherAction as MoveAction).performingEntity.Displacement.Coordinates.GetTile().SetEntity((_otherAction as MoveAction).performingEntity, _isThisTurn: false);
				}
				else
				{
					(_otherAction as MoveAction).finalTargetTile.SetEntity(_otherAction.performingEntity, _isThisTurn: false);
					finalTargetTile = null;
					performingEntity.Displacement.Coordinates.GetTile().SetEntity(performingEntity, _isThisTurn: false);
				}
			}
		}
		
		if (IsDestinationOccupiedOnNextTurnAction())
		{
			hasConflict = true;
			RefreshDestinatedTile();

			if (finalTargetTile != null)
				finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);
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

		if (pathToTile == null || pathToTile.Count == 0)
		{
			finalTargetTile = null;
			return;
		}

		finalTargetTile = pathToTile[^1];
	}

	public override void Display ()
	{
		PoolElement arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData);
		Vector3 startPos = positionAtActionStart.transform.position;
		Vector3 destination = positionAtActionEnd.transform.position;
		Vector3 position = Vector3.Lerp(startPos, destination, .5f);
		arrow.transform.position = position;
		arrow.transform.LookAt(positionAtActionEnd.transform);

		PlayerController.Instance.arrows.Add(arrow as Arrow);
	}
}
