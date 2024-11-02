using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateWeaponAction : AEntityAction
{
	public string rotatingWeaponID;
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
			Entity target = performingEntity.AI.GetClosestEnemyInWeaponRange(out string _weaponID, true);
			if (target != null)
			{
				base.Perform(_state);
				performingEntity.Equipment.AimAtTile(_weaponID, target.Displacement.Coordinates.GetTile(), EndPerform);
			}
			else
			{
				base.Perform(_state);
				EndPerform();
			}
			return;
		}

		/*//if enemy is in weapon range
		//bool isEnemyInWeaponRange = performingEntity.AI.IsEntityInWeaponRange(targetedEntity, out Weapon _attackingWeapon);
		bool isEnemyInWeaponRange = performingEntity.AI.IsEntityInWeaponPossibleRange(targetedEntity, out string weaponID, true);
		if (isEnemyInWeaponRange)
		{
			// => do nothing
			base.Perform(_state);
			//TODO : shoot success anim
			EndPerform();
		}
		else
		{*/
			// => rotate weapon

			//performingEntity.AI.IsEntityInWeaponPossibleRange(targetedEntity, out string _weaponID, true);
			//doit appler end perform quand rotate end
			performingEntity.Equipment.AimAtTile(rotatingWeaponID, targetedEntity.Displacement.Coordinates.GetTile(), EndPerform);
		/*} */
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
