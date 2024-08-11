using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MoveAction : EntityAction
{
	public Entity targetEntiy;
	public Tile targetTile;
	public MoveActionMode mode;


	public Tile finalTargetTile;

	public enum MoveActionMode { Coordinate, Entity }

	public override void Prepare ( Entity.EntityState _state )
	{
		base.Prepare(_state);

		targetEntiy.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);

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
		if (finalTargetTile != null && finalTargetTile.GetEntity(false) == null)
			performingEntity.Displacement.MoveToTile(finalTargetTile, EndPerform);
		else
			EndPerform();
	}

	public override void EndPerform ()
	{
		base.EndPerform();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (_tile.IsObstacle() || GridManager.Instance.GetDistance(performingEntity.Displacement.Coordinates.GetTile(), _tile) > 1)
			return false;

		return base.TileInteractPredicate(_tile);
	}

	public override bool CheckConflict ( EntityAction _otherAction )
	{
		if (base.CheckConflict(_otherAction))
			return true;

		if(finalTargetTile == null)
		{
			RefreshDestinatedTile();
			if (finalTargetTile == null)
				return false;
		}

		bool hasConflict = false;
		if (_otherAction is MoveAction)
		{
			if ((_otherAction as MoveAction).finalTargetTile == finalTargetTile)
			{
				hasConflict = true;
				//TODO : make a roll to see who goes to it

				int roll = UnityEngine.Random.Range((int)0, 2);
				if (roll == 0)
				{
					//performing entity wins roll
					finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);
					(_otherAction as MoveAction).RefreshDestinatedTile();
				}
				else
				{
					(_otherAction as MoveAction).finalTargetTile.SetEntity(_otherAction.performingEntity, _isThisTurn: false);
					RefreshDestinatedTile();
				}
			}
		}
		
		if (IsDestinationOccupiedOnNextTurnAction())
		{
			hasConflict = true;
			//TODO : find new destination
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
		return finalTargetTile.GetEntity(_isThisTurn: false) != null || finalTargetTile.IsObstacle();
	}

	private void RefreshDestinatedTile ()
	{
		List<Tile> pathToTile = GridManager.Instance.GetPath(performingEntity.Displacement.Coordinates.GetTile(), finalTargetTile, _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count == 0)
			return;

		finalTargetTile = pathToTile[^1];
	}
}
