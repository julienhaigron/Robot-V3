using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AttackAction : AEntityAction
{
	public string attackingWeaponId;
	public int targetedEntityID = -1;
	public bool isAttackSuccessfull = false;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attackingWeaponId);
		serializer.SerializeValue(ref targetedEntityID);
		serializer.SerializeValue(ref isAttackSuccessfull);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if(targetedEntityID != -1)
		{
			Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
			targetedEntityID = performingEntity.AI.TargetedEntity.ID;
			Entity targetEntity = GameManager.Instance.GetEntityFromID((int)targetedEntityID);
			isAttackSuccessfull = performingEntity.Equipment.AttackRoll(targetEntity);
		}
		else if (targetedEntityID == -1 && GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity != null)
		{
			Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
			targetedEntityID = performingEntity.AI.TargetedEntity.ID;
			Entity targetEntity = GameManager.Instance.GetEntityFromID((int)targetedEntityID);
			isAttackSuccessfull = performingEntity.Equipment.AttackRoll(targetEntity);
		}
		else if(targetedEntityID == -1)
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
		GameManager.Instance.GetEntityFromID(performingEntityID).AI.DOAllPrewarmCheck();
		if (targetedEntityID == -1)
		{
			//TODO : add no target feedback
			base.Perform(_state);
			EndPerform();
		}

		//if enemy is in weapon range
		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		Entity targetEntity = GameManager.Instance.GetEntityFromID((int)targetedEntityID);
		bool isEnemyInWeaponRange = performingEntity.AI.IsEntityInWeaponRange(targetEntity, out Weapon _attackingWeapon);

		if (isEnemyInWeaponRange)
		{
			_attackingWeapon.PerformAttack(this, isAttackSuccessfull, EndPerform);
		}
		else
		{
			// => find new target or wait (or move to previous target if in sight?)
			//Debug.Log("target not in range");
			//DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndPerform());
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
		return entity != null && !entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).PlayerOwnerID);
	}
}
