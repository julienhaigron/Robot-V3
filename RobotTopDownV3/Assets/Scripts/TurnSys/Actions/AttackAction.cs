using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AttackAction : AEntityAction
{
	public string attackingWeaponId;
	public int targetedEntityID = -1;
	public Entity TargetEntity => GameManager.Instance.GetEntityFromID(targetedEntityID);
	public int targetTileID = -1;
	public Tile TargetTile => GridManager.Instance.Tiles[targetTileID];
	public bool isAttackSuccessfull = false;
	
	//damages
	public bool[] areEffectsSuccess;
	public int[] damages;
	public short[] damageTypes;
	public int pfcResult = (int)EntityActionData.PFCResultType.Failure;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attackingWeaponId);
		serializer.SerializeValue(ref targetedEntityID);
		serializer.SerializeValue(ref isAttackSuccessfull);
		serializer.SerializeValue(ref areEffectsSuccess);
		serializer.SerializeValue(ref damages);
		serializer.SerializeValue(ref damageTypes);
		serializer.SerializeValue(ref pfcResult);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (targetedEntityID != -1 || (targetedEntityID == -1 && PerformingEntity.AI.TargetedEntity != null)
			|| (Data.isAoe && targetTileID != -1))
		{
			targetedEntityID = PerformingEntity.AI.TargetedEntity.ID;
			isAttackSuccessfull = Data.isAoe ? true : PerformingEntity.Equipment.AttackRoll(this);

			if (isAttackSuccessfull)
			{
				areEffectsSuccess = new bool[effectsIds.Length];
				for (int i = 0; i < effectsIds.Length; i++)
				{
					areEffectsSuccess[i] = PerformingEntity.Equipment.EffectRoll(TargetEntity, GameAssets.current.game.entityEffects[(AEntityEffect.EntityEffectEnumID)effectsIds[i]]);
				}

				Dictionary<WeaponEquipmentData.DamageType, int> damagesDealt =
					PerformingEntity.Equipment.Weapons[attackingWeaponId].GetDamages(PerformingEntity, TargetEntity, Data, (EntityActionData.PFCResultType)pfcResult);

				List<int> tmpDamages = new();
				List<short> tmpDamageTypes = new();
				foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in damagesDealt)
				{
					tmpDamages.Add(pair.Value);
					tmpDamageTypes.Add((short)pair.Key);
				}
				damages = tmpDamages.ToArray();
				damageTypes = tmpDamageTypes.ToArray();
			}
		}
		else if (targetedEntityID == -1)
		{
			//TODO : handle this situation
			Debug.Log("ERROR : no available target");
		}
	}

	public override bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		pfcResult = (int)EntityActionData.PFC(Data, _otherAction.Data);
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

		if (isEnemyInWeaponRange || (Data.isAoe && targetTileID != -1))
		{
			List<Tile> tilesInWeaponRange = Data.isAoe ? PerformingEntity.Equipment.GetTilesInAoERange(this, true) : PerformingEntity.Equipment.GetTilesInWeaponRange(_attackingWeapon.Data.name, true);
			base.Perform(_state);
			foreach (Tile tile in tilesInWeaponRange)
			{
				tile.UI.SetOutlineColor(Color.red);
			}
			_attackingWeapon.PerformAttack(this, () =>
			{
				foreach (Tile tile in tilesInWeaponRange)
				{
					tile.UI.ResetOutline();
				}
				EndPerform();
			});
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
		//TODO : should also check if unit is in supposed weapon range (counting orientation)

		Entity entity = _tile.GetEntity(true);
		return entity != null && ((Data.targetType == EntityActionData.TargetType.Self) == entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID));
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		if (_tile.GetEntity(true))
			targetedEntityID = _tile.GetEntity(true).ID;
		targetTileID = _tile.coordinates.ID;
		base.RegisterInteraction(_tile);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
