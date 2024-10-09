using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAction : AEntityAction
{

	private Entity targetedEntity;

	public override void Prepare ( Entity.EntityState _state )
	{
		if (performingEntity.AI.TargetedEntity != null)
			targetedEntity = performingEntity.AI.TargetedEntity;
		else
		{
			//TODO : handle this situation
			Debug.Log("ERROR : no available target");
		}
	}

	public override bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?
		return false;
	}

	public override void Perform ( Entity.EntityState _state )
	{
		//int dist = GridManager.Instance.GetDistanceBetween(performingEntity.Displacement.Coordinates.GetTile(), targetedEntity.Displacement.Coordinates.GetTile(), true);
		
		//if enemy is in weapon range
		// => shoot
		//else 
		// => find new ennemy or wait

		Debug.Log("shoot entity");
		base.Perform(_state);
	}

	public override void Display ()
	{
		//TODO ?
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		//TODO : select only visible enemies

		Entity entity = _tile.GetEntity(true);
		return entity != null && entity.Data.faction == Entity.EntityFaction.Enemy;
	}
}
