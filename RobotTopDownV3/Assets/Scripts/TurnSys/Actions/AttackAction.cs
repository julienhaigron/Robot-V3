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
		public int hittedTileID;
		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref isAttackSuccessfull);
			serializer.SerializeValue(ref areStatusesSuccess);
			serializer.SerializeValue(ref statusIds);
			serializer.SerializeValue(ref damages);
			serializer.SerializeValue(ref damageTypes);
			serializer.SerializeValue(ref pfcResult);
			serializer.SerializeValue(ref hittedTileID);
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
		if (targetedEntityIDs != null || (Data.aoeType != EntityActionData.AOEType.Noone && targetTileIDs != null))
		{
			//targetedEntityID = PerformingEntity.AI.TargetedEntity.ID;
			if (Data.aoeType != EntityActionData.AOEType.Noone)
				LogConsole.AddLog("Automatic hit on targets due to AoE type", LogConsole.LogEventType.AttackRoll);
			
			for (int attackCount = 0; attackCount < attacksInfos.Length; attackCount++)
			{
				SingleAttackInfo attackInfo = attacksInfos[attackCount];
				Entity targetEntity = GameManager.Instance.GetEntityFromID(targetedEntityIDs[attackCount]);
				Tile coverHitted = null;
				attackInfo.isAttackSuccessfull = Data.aoeType != EntityActionData.AOEType.Noone ? true : PerformingEntity.Equipment.AttackRoll(this, attackInfo, targetEntity, out coverHitted);
				attackInfo.hittedTileID = coverHitted == null ? -1 : coverHitted.coordinates.ID;
				if (attackInfo.isAttackSuccessfull)
				{
					List<AEntityStatus> appliedStatuses = Data.GetAppliedStatuses(this, PerformingEntity, targetEntity);
					attackInfo.statusIds = new short[appliedStatuses.Count];
					for (int i = 0; i < appliedStatuses.Count; i++)
						attackInfo.statusIds[i] = (short)appliedStatuses[i].enumID;
					attackInfo.areStatusesSuccess = new bool[attackInfo.statusIds.Length];
					for (int i = 0; i < attackInfo.statusIds.Length; i++)
					{
						if (!appliedStatuses[i].doesNeedRoll)
							attackInfo.areStatusesSuccess[i] = true;
						else
							attackInfo.areStatusesSuccess[i] = PerformingEntity.Equipment.StatusRoll(targetEntity, GameAssets.current.game.entityStatus[(EntityStatusEnumID)attackInfo.statusIds[i]]
							, this, GameAssets.current.equipments[linkedEquipmentId]);
					}

					Dictionary<WeaponEquipmentData.DamageType, int> damagesDealt =
						PerformingEntity.Equipment.Weapons[linkedEquipmentId].GetDamages(PerformingEntity, targetEntity, this, (EntityActionData.PFCResultType)attackInfo.pfcResult);

					if (coverHitted != null)
						coverHitted.Wall.RegisterDamage(damagesDealt);

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

		/*List<Tile> tilesInWeaponRange = Data.isAoe ? PerformingEntity.Equipment.GetTilesInAoERange(this, GridManager.Instance.Tiles[targetTileIDs[attackCount]], true) : PerformingEntity.Equipment.GetTilesInWeaponRange(this, linkedEquipmentId, true);
		foreach (Tile tile in tilesInWeaponRange)
		{
			tile.UI.SetOutlineColor(Color.red);
		}*/
		PerformingEntity.Equipment.Weapons[linkedEquipmentId].PerformAttack(this, () =>
		{
			/*foreach (Tile tile in tilesInWeaponRange)
			{
				tile.UI.ResetOutline();
			}*/
			base.Perform(_state);
			EndTick();
		});

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
			|| Data.trajectoryType == EntityActionData.TrajectoryType.Mortar;
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
		if (TurnManager.Instance.CurrentActionTargetTiles == null)
			return;

		foreach (Tile tile in TurnManager.Instance.CurrentActionTargetTiles)
		{
			if (tile == null)
				continue;

			tile.UI.SetOutlineColor(Color.blue);
		}
	}
}
