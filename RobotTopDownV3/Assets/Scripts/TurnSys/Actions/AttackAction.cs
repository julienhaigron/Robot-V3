using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class AttackAction : AEntityAction
{
	public SingleAttackInfo[] attacksInfos;

	public class SingleAttackInfo : INetworkSerializable
	{
		public bool isAttackSuccessfull;
		public bool[] areStatusesSuccess;
		public short[] statusIds;
		public int[] damages;
		public short[] damageTypes;
		public int pfcResult;
		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref isAttackSuccessfull);
			serializer.SerializeValue(ref areStatusesSuccess);
			serializer.SerializeValue(ref statusIds);
			serializer.SerializeValue(ref damages);
			serializer.SerializeValue(ref damageTypes);
			serializer.SerializeValue(ref pfcResult);
		}
	}

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attacksInfos);
	}

	public override void ConflictCheckPrewarm ()
	{
		base.ConflictCheckPrewarm();

		attacksInfos = new SingleAttackInfo[targetedEntityIDs.Length];
		for (int i = 0; i < attacksInfos.Length; i++)
			attacksInfos[i] = new();
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		for (int i = 0; i < targetedEntityIDs.Length; i++)
		{
			if (targetedEntityIDs[i] == _otherAction.performingEntityID)
				attacksInfos[i].pfcResult = (int)EntityActionData.PFC(Data, _otherAction.Data);
		}

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (targetedEntityIDs != null || (Data.isAoe && targetTileIDs != null))
		{
			//targetedEntityID = PerformingEntity.AI.TargetedEntity.ID;
			for (int attackCount = 0; attackCount < attacksInfos.Length; attackCount++)
			{
				SingleAttackInfo attackInfo = attacksInfos[attackCount];
				Entity targetEntity = GameManager.Instance.GetEntityFromID(targetedEntityIDs[attackCount]);
				attackInfo.isAttackSuccessfull = Data.isAoe ? true : PerformingEntity.Equipment.AttackRoll(this, attackInfo, targetEntity);
				if (Data.isAoe)
					LogConsole.AddLog("Automatic hit on targets due to AoE type", LogConsole.LogEventType.AttackResolution);

				if (attackInfo.isAttackSuccessfull)
				{
					attackInfo.statusIds = new short[Data.appliableStatus.Length];
					for (int i = 0; i < Data.appliableStatus.Length; i++)
						attackInfo.statusIds[i] = (short)Data.appliableStatus[i].enumID;
					attackInfo.areStatusesSuccess = new bool[attackInfo.statusIds.Length];
					for (int i = 0; i < attackInfo.statusIds.Length; i++)
					{
						attackInfo.areStatusesSuccess[i] = PerformingEntity.Equipment.StatusRoll(targetEntity, GameAssets.current.game.entityStatus[(EntityStatusEnumID)attackInfo.statusIds[i]]
							, this, GameAssets.current.equipments[linkedEquipmentId]);
					}

					Dictionary<WeaponEquipmentData.DamageType, int> damagesDealt =
						PerformingEntity.Equipment.Weapons[linkedEquipmentId].GetDamages(PerformingEntity, targetEntity, this, (EntityActionData.PFCResultType)attackInfo.pfcResult);

					List<int> tmpDamages = new();
					List<short> tmpDamageTypes = new();
					foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in damagesDealt)
					{
						tmpDamages.Add(pair.Value);
						tmpDamageTypes.Add((short)pair.Key);
					}
					attackInfo.damages = tmpDamages.ToArray();
					attackInfo.damageTypes = tmpDamageTypes.ToArray();
				}
			}
		}
		else if (targetedEntityIDs != null)
		{
			//TODO : handle this situation
			Debug.Log("ERROR : no available target");
		}
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		PerformingEntity.AI.DOAllPrewarmCheck(this);
		if (targetedEntityIDs == null)
		{
			Debug.LogError("No target error");
			base.Perform(_state);
			EndTick();
		}

		for (int attackCount = 0; attackCount < attacksInfos.Length; attackCount++)
		{
			//if enemy is in weapon range
			Entity targetEntity = GameManager.Instance.GetEntityFromID(targetedEntityIDs[attackCount]);
			bool isEnemyInWeaponRange = PerformingEntity.AI.IsEntityInWeaponRange(targetEntity, linkedEquipmentId);

			if (isEnemyInWeaponRange || (Data.isAoe && targetTileIDs != null))
			{
				List<Tile> tilesInWeaponRange = Data.isAoe ? PerformingEntity.Equipment.GetTilesInAoERange(this, GridManager.Instance.Tiles[targetTileIDs[attackCount]], true) : PerformingEntity.Equipment.GetTilesInWeaponRange(this, linkedEquipmentId, true);
				foreach (Tile tile in tilesInWeaponRange)
				{
					tile.UI.SetOutlineColor(Color.red);
				}
				PerformingEntity.Equipment.Weapons[linkedEquipmentId].PerformAttack(this, () =>
				{
					foreach (Tile tile in tilesInWeaponRange)
					{
						tile.UI.ResetOutline();
					}
					base.Perform(_state);
					EndTick();
				});
			}
			else
			{
				// => find new target or wait (or move to previous target if in sight?)
				//Debug.Log("target not in range");
				//DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndPerform());
				Debug.LogError("No target error");
				base.Perform(_state);
				EndTick();
			}
		}
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void OnSelectActionTileInteractPredicatePrewarm ()
	{
		base.OnSelectActionTileInteractPredicatePrewarm();

		//for all tiles overall distance calculation
		bool attackIgnoresObstacles = (Data.type == EntityActionData.ActionType.DistanceAttack && effects.Any(e => e.enumID == EntityPassiveEffectEnumID.TrajectoryControl))
			|| Data.targetType == EntityActionData.TargetType.Mortar;
		Entity user = GameManager.Instance.GetEntityFromID(performingEntityID);
		Weapon attackingWeapon = user.Equipment.Weapons[linkedEquipmentId];
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		int maxDist = Data.GetMaxRange(this, PerformingEntity, null);
		GridManager.Instance.GetTilesInVisionRange(from, maxDist, attackIgnoresObstacles, true);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (Data.targetType == EntityActionData.TargetType.Self && _tile.coordinates.ID == TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID))
			return true;

		if (Data.targetType == EntityActionData.TargetType.Tile && _tile.IsVisibleFromSelectedEntity)
			return true;

		Entity entity = _tile.GetEntity(true);
		return entity != null && _tile.IsVisibleFromSelectedEntity && !entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
