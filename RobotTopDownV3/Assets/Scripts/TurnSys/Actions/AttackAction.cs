using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AttackAction : AEntityAction
{
	public string attackingWeaponId;
	public int targetedEntityID = -1;
	public Entity TargetEntity => GameManager.Instance.GetEntityFromID(targetedEntityID);
	public bool isAttackSuccessfull = false;
	public bool[] areEffectsSuccess;
	public int[] damages;
	public short[] damageTypes;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attackingWeaponId);
		serializer.SerializeValue(ref targetedEntityID);
		serializer.SerializeValue(ref isAttackSuccessfull);
		serializer.SerializeValue(ref areEffectsSuccess);
		serializer.SerializeValue(ref damages);
		serializer.SerializeValue(ref damageTypes);
	}

	//todo :
	//better attack roll
	// => add vision angle calculus to it
	// => add PFC system
	// => add entity state (stunned, burning, frozen, whtvr)
	// => add damage channels

	public override void Prepare ( Entity.EntityState _state )
	{		
		if(targetedEntityID != -1 || (targetedEntityID == -1 && PerformingEntity.AI.TargetedEntity != null))
		{
			targetedEntityID = PerformingEntity.AI.TargetedEntity.ID;
			isAttackSuccessfull = PerformingEntity.Equipment.AttackRoll(this);

			if (isAttackSuccessfull)
			{
				areEffectsSuccess = new bool[effectsIds.Length];
				for (int i = 0; i < effectsIds.Length; i++)
				{
					areEffectsSuccess[i] = PerformingEntity.Equipment.EffectRoll(TargetEntity, GameAssets.current.game.entityEffects[(AEntityEffect.EntityEffectEnumID)effectsIds[i]]);
				}

				Dictionary<WeaponEquipmentData.DamageType, int> damagesDealt = PerformingEntity.Equipment.Weapons[attackingWeaponId].GetDamages(PerformingEntity, TargetEntity);

				List<int> tmpDamages = new();
				List<short> tmpDamageTypes = new();
				foreach(KeyValuePair<WeaponEquipmentData.DamageType, int> pair in damagesDealt)
				{
					tmpDamages.Add(pair.Value);
					tmpDamageTypes.Add((short)pair.Key);
				}
				damages = tmpDamages.ToArray();
				damageTypes = tmpDamageTypes.ToArray();
			}
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
		PerformingEntity.AI.DOAllPrewarmCheck();
		if (targetedEntityID == -1)
		{
			//TODO : add no target feedback
			base.Perform(_state);
			EndPerform();
		}

		//if enemy is in weapon range
		bool isEnemyInWeaponRange = PerformingEntity.AI.IsEntityInWeaponRange(TargetEntity, out Weapon _attackingWeapon);

		if (isEnemyInWeaponRange)
		{
			base.Perform(_state);
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

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		//TODO : select only visible enemies

		Entity entity = _tile.GetEntity(true);
		return entity != null && !entity.IsAlliedTo(PerformingEntity.OwnerID);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
