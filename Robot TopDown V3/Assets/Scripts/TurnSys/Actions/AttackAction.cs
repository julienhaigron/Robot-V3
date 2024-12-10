using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAction : AEntityAction
{
	public string attackingWeaponId;
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
		if(targetedEntity == null)
		{
			base.Perform(_state);
			EndPerform();
		}

		//if enemy is in weapon range
		bool isEnemyInWeaponRange = performingEntity.AI.IsEntityInWeaponRange(targetedEntity, out Weapon _attackingWeapon);
		if (isEnemyInWeaponRange)
		{
			// => shoot
			bool isAttackRollSuccessful = performingEntity.Equipment.AttackRoll(targetedEntity);
			if (isAttackRollSuccessful)
			{
				int damageAmout = _attackingWeapon.Data.damage;
				Debug.Log("shoot entity " + damageAmout + " damages");
				targetedEntity.Equipment.TakeDamage(damageAmout);
				base.Perform(_state);
				//TODO : shoot success anim
				EndPerform();
			}
			else
			{
				//TODO : shoot but failed anim
				Debug.Log("shoot failed");
				base.Perform(_state);
				EndPerform();
			}
		}
		else
		{
			// => find new target or wait (or move to prevvious target if in sight?)
			Debug.Log("target not in range");
			base.Perform(_state);
			EndPerform();
		} 
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
