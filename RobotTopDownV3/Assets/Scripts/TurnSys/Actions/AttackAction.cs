using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class AttackAction : AEntityAction
{
	public SingleAttackInfo[] attacksInfos; //reset at each active tick

	//for client only
	private HashSet<Tile> m_tilesInRange;
	private List<int> m_orientations = new();

	private bool m_didCheckConflict = false; //client only and used only for targetType == Tile

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

		int maxTarget = targetTileIDs.Length / actualDuration;
		attacksInfos = new SingleAttackInfo[maxTarget];
		for (int i = 0; i < attacksInfos.Length; i++)
			attacksInfos[i] = new();
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (m_didCheckConflict)
			return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };

		if (Data.targetType == EntityActionData.TargetType.Tile)
		{
			for (int i = 0; i < attacksInfos.Length; i++)
				attacksInfos[i].pfcResult = (int)EntityActionData.PFCResultType.Equal;

			m_didCheckConflict = true;
		}
		else
		{
			for (int i = 0 ; i < attacksInfos.Length; i++)
			{
				if (targetedEntityIDs[i] == _otherAction.performingEntityID)
					attacksInfos[i].pfcResult = (int)EntityActionData.PFC(Data, _otherAction.Data);
			}
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
				Entity targetEntity = GameManager.Instance.GetEntityFromID(targetedEntityIDs[attackCount * ActiveLifetime]);
				Tile coverHitted = null;
				if (Data.aoeType != EntityActionData.AOEType.Noone)
				{
					attackInfo.isAttackSuccessfull = true;

					//An area attack stopped by a full wall has to go off against that wall rather than on the far
					//side of it. Recentring targetTileIDs is enough: GetTargets, the outlines and the projectile
					//all read the blast centre from there. AttackRoll is not called for area attacks, hence the
					//dedicated check here.
					Tile shooterTile = PerformingEntity.Displacement.Coordinates.GetTile();
					Tile aoeCenter = GridManager.Instance.Tiles[targetTileIDs[attackCount * ActiveLifetime]];
					bool isBlockedByWall = GridManager.Instance.IsThereBlockingWallBetween(shooterTile, aoeCenter, true, out coverHitted);

					if (isBlockedByWall)
						targetTileIDs[attackCount * ActiveLifetime] = coverHitted.coordinates.ID;
				}
				else
					attackInfo.isAttackSuccessfull = PerformingEntity.Equipment.AttackRoll(this, attackInfo, targetEntity, out coverHitted);

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

				}

				//Computed whether or not the shot lands: a round stopped by a wall has to carry the weapon's real
				//damage to it, and the cover registration below only ever concerns a failed shot anyway. Every
				//consumer that applies this to an entity checks isAttackSuccessfull first.
				if (targetEntity != null)
				{
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
		if (targetedEntityIDs == null && Data.targetType != EntityActionData.TargetType.Tile)
		{
			Debug.LogError("No target error");
			base.Perform(_state);
			EndTick();
			return;
		}

		if (PerformingEntity.Equipment.IsDead)
		{
			base.Perform(_state);
			EndTick();
			return;
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

	#region Input

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void OnSelectActionTileInteractPredicatePrewarm ()
	{
		base.OnSelectActionTileInteractPredicatePrewarm();
		Entity user = GameManager.Instance.GetEntityFromID(performingEntityID);
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		int currentOrientation = TurnManager.Instance.GetLastRegisteredOrientationOfEntity(performingEntityID);
		int maxTargetAmount = Data.GetMaxTargetAmount(this, PerformingEntity, null);
		m_tilesInRange = null;

		if (TurnManager.Instance.CurrentActionTargetTiles.Count % maxTargetAmount != 0)
			m_tilesInRange = user.Equipment.GetTilesInWeaponRange(this, false, from, m_orientations.Count == 0 ? currentOrientation : m_orientations[^1]).ToHashSet();
		else
		{
			m_tilesInRange = new();
			for (int i = 0; i < 6; i++)
			{
				foreach (Tile tile in user.Equipment.GetTilesInWeaponRange(this, false, from, i))
					m_tilesInRange.Add(tile);
			}
		}
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (m_tilesInRange.Contains(_tile)
			&& (Data.targetType == EntityActionData.TargetType.Tile || _tile.TryGetEntity(true, out Entity entity)))
			return true;
		else
			return false;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		int currentOrientation = TurnManager.Instance.GetLastRegisteredOrientationOfEntity(performingEntityID);
		int targetOrientation = GridManager.Instance.GetClosestOrientation(from, _tile);
		if (currentOrientation == targetOrientation && m_orientations.Count == 0)
		{
			base.RegisterInteraction(_tile);
			return;
		}

		int maxTargetAmount = Data.GetMaxTargetAmount(this, PerformingEntity, _tile.GetEntity(true));
		if (TurnManager.Instance.CurrentActionTargetTiles == null || TurnManager.Instance.CurrentActionTargetTiles.Count % maxTargetAmount == 0)
			m_orientations.Add(targetOrientation);

		TurnManager.Instance.AddTargetTileInCurrentAction(_tile);

		if (_tile.TryGetPlannedItemAt(timeAtStart, out Item item))
			item.Data.OnRegisterInteraction(this, item);

		if (TurnManager.Instance.CurrentActionTargetTiles.Count == maxTargetAmount * Data.tokenDuration)
		{
			RotateEntityAction modAction = TurnManager.Instance.GetAction(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], performingEntityID, null, timeAtStart) as RotateEntityAction;
			modAction.targetTileIDs = new int[1];
			modAction.targetTileIDs[0] = _tile.coordinates.ID;
			modAction.targetedOrientationID = m_orientations.ToArray();
			TurnManager.Instance.RegisterActionAndMod(performingEntityID, TurnManager.Instance.CurrentActionSelected, modAction, TurnManager.Instance.CurrentStateTypeSelected);
			TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
		}
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

	#endregion
}
