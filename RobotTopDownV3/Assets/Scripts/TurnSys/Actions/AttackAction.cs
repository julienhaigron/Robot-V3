using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class AttackAction : AEntityAction
{
	public string attackingWeaponId;
	public int targetedEntityID = -1;
	public Entity TargetEntity => GameManager.Instance.GetEntityFromID(targetedEntityID);
	public int targetTileID = -1;
	public Tile TargetTile => GridManager.Instance.Tiles[targetTileID];
	public bool isAttackSuccessfull = false;
	
	//damages
	public bool[] areStatusesSuccess;
	public int[] damages;
	public short[] damageTypes;
	public int pfcResult = (int)EntityActionData.PFCResultType.Failure;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attackingWeaponId);
		serializer.SerializeValue(ref targetedEntityID);
		serializer.SerializeValue(ref targetTileID);
		serializer.SerializeValue(ref isAttackSuccessfull);
		serializer.SerializeValue(ref areStatusesSuccess);
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
				/*areStatusesSuccess = new bool[statusIds.Length];
				for (int i = 0; i < statusIds.Length; i++)
				{
					areStatusesSuccess[i] = PerformingEntity.Equipment.StatusRoll(TargetEntity, GameAssets.current.game.entityStatus[(EntityStatusEnumID)statusIds[i]]);
				}*/

				Dictionary<WeaponEquipmentData.DamageType, int> damagesDealt =
					PerformingEntity.Equipment.Weapons[attackingWeaponId].GetDamages(PerformingEntity, TargetEntity, this, (EntityActionData.PFCResultType)pfcResult);

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

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		pfcResult = (int)EntityActionData.PFC(Data, _otherAction.Data);
		//no conflict ?
		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false};
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		PerformingEntity.AI.DOAllPrewarmCheck(this);
		if (targetedEntityID == -1)
		{
			//TODO : add no target feedback
			base.Perform(_state);
			EndTick();
		}

		//if enemy is in weapon range
		bool isEnemyInWeaponRange = PerformingEntity.AI.IsEntityInWeaponRange(TargetEntity, attackingWeaponId);

		if (isEnemyInWeaponRange || (Data.isAoe && targetTileID != -1))
		{
			List<Tile> tilesInWeaponRange = Data.isAoe ? PerformingEntity.Equipment.GetTilesInAoERange(this, true) : PerformingEntity.Equipment.GetTilesInWeaponRange(this, attackingWeaponId, true);
			base.Perform(_state);
			foreach (Tile tile in tilesInWeaponRange)
			{
				tile.UI.SetOutlineColor(Color.red);
			}
			PerformingEntity.Equipment.Weapons[attackingWeaponId].PerformAttack(this, () =>
			{
				foreach (Tile tile in tilesInWeaponRange)
				{
					tile.UI.ResetOutline();
				}
				EndTick();
			});
		}
		else
		{
			// => find new target or wait (or move to previous target if in sight?)
			//Debug.Log("target not in range");
			//DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndPerform());
			base.Perform(_state);
			EndTick();
		}
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (Data.targetType == EntityActionData.TargetType.Self && _tile.coordinates.ID == TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID))
			return true;

		bool attackIgnoresObstacles = (Data.type == EntityActionData.ActionType.DistanceAttack && effects.Any(e => e.enumID == EntityPassiveEffectEnumID.TrajectoryControl)) 
			|| Data.targetType == EntityActionData.TargetType.Mortar;
		Entity user = GameManager.Instance.GetEntityFromID(performingEntityID);
		Weapon attackingWeapon = user.Equipment.Weapons[linkedEquipmentId];
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> tilesInRange = attackIgnoresObstacles ? GridManager.Instance.GetTilesInVisionRange(from, attackingWeapon.Data.range, true)
			: GridManager.Instance.GetTilesInRange(from, attackingWeapon.Data.range, true, true);
		bool isInRange = tilesInRange.Contains(_tile);

		if (Data.targetType == EntityActionData.TargetType.Tile && isInRange)
			return true;
		
		Entity entity = _tile.GetEntity(true);
		return entity != null && isInRange && !entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID);
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
