using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AttackAction : AEntityAction
{
	public string attackingWeaponId;
	private int targetedEntityID = -1;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attackingWeaponId);
		serializer.SerializeValue(ref targetedEntityID);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity != null)
			targetedEntityID = GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity.ID;
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
		if(targetedEntityID == -1)
		{
			base.Perform(_state);
			EndPerform();
		}

		//if enemy is in weapon range
		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		Entity targetEntity = GameManager.Instance.GetEntityFromID((int)targetedEntityID);
		bool isEnemyInWeaponRange = performingEntity.AI.IsEntityInWeaponRange(targetEntity, out Weapon _attackingWeapon);
		if (isEnemyInWeaponRange)
		{
			// => shoot
			bool isAttackRollSuccessful = performingEntity.Equipment.AttackRoll(targetEntity);
			if (isAttackRollSuccessful)
			{
				int damageAmout = _attackingWeapon.Data.damage;
				Debug.Log("shoot entity " + damageAmout + " damages");
				targetEntity.Equipment.TakeDamage(damageAmout);
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
