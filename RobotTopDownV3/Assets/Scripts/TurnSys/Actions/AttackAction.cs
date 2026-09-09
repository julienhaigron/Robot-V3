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
		public int hittedTileID;
		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref isAttackSuccessfull);
			serializer.SerializeValue(ref areStatusesSuccess);
			serializer.SerializeValue(ref statusIds);
			serializer.SerializeValue(ref damages);
			serializer.SerializeValue(ref damageTypes);
			serializer.SerializeValue(ref hittedTileID);
		}
	}

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref attacksInfos);
	}

	public Entity GetTargetEntityAt ( int _attackIndex )
	{
		int index = _attackIndex * ActiveLifetime;

		return targetedEntityIDs != null && index >= 0 && index < targetedEntityIDs.Length
			? GameManager.Instance.GetEntityFromID(targetedEntityIDs[index])
			: null;
	}

	public Tile GetTargetTileAt ( int _attackIndex )
	{
		int index = _attackIndex * ActiveLifetime;

		return targetTileIDs != null && index >= 0 && index < targetTileIDs.Length
			? GridManager.Instance.Tiles[targetTileIDs[index]]
			: null;
	}

	public EntityActionData.PFCResultType GetExchangeResultAgainst ( Entity _entity )
	{
		if (_entity == null || TurnManager.Instance == null)
			return EntityActionData.PFCResultType.Failure;

		AEntityAction otherAction = TurnManager.Instance.GetActionPerformedAtTick(_entity.ID);

		return otherAction == null
			? EntityActionData.PFCResultType.Failure
			: EntityActionData.PFC(Data, otherAction.Data);
	}

	public bool DoesWinExchangeAgainst ( Entity _entity )
	{
		return GetExchangeResultAgainst(_entity) == EntityActionData.PFCResultType.FirstWins;
	}

	public Tile GetExchangeTileOf ( Entity _entity )
	{
		if (_entity == null)
			return null;

		EntityDisplacementPlugin displacement = _entity.Displacement;

		if (DoesWinExchangeAgainst(_entity))
			return displacement.DidMoveThisTick ? displacement.PreviousCoordinates.GetTile() : displacement.Coordinates.GetTile();

		return GridManager.Instance.Tiles[TurnManager.Instance.GetEntityPositionAtEndOfTick(_entity.ID, displacement.Coordinates.ID)];
	}

	public override void ConflictCheckPrewarm ()
	{
		base.ConflictCheckPrewarm();

		int maxTarget = targetTileIDs == null || actualDuration <= 0 ? 0 : targetTileIDs.Length / actualDuration;
		attacksInfos = new SingleAttackInfo[maxTarget];
		for (int i = 0; i < attacksInfos.Length; i++)
			attacksInfos[i] = new();
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (m_didCheckConflict)
			return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };

		if (Data.targetType == EntityActionData.TargetType.Tile)
			m_didCheckConflict = true;

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	private void LogUseWeapon ()
	{
		List<string> targetNames = new();
		if (targetedEntityIDs != null)
		{
			foreach (int targetID in targetedEntityIDs)
			{
				Entity target = GameManager.Instance.GetEntityFromID(targetID);
				if (target != null && !targetNames.Contains(target.Data.name))
					targetNames.Add(target.Data.name);
			}
		}

		LocalizationManager localization = LocalizationManager.Instance;
		LogConsole.AddLog(TagLogMessage(string.Format(localization.Get(LocalizationKey.log_use_weapon), PerformingEntity.Data.name
				, targetNames.Count == 0 ? localization.Get(LocalizationKey.log_use_weapon_ground) : string.Join(", ", targetNames)
				, Data.GetLocalizedName()))
			, LogConsole.LogEventType.UseWeapon
			, new LogConsole.LogDetails("useweapon_" + LogConsole.Instance.LogsDetails.Keys.Count, Data.GetLocalizedName(), Data.GetDescription()));
	}

	//An AoE names who took the hit: a bare total says nothing about who was in the blast, and nothing at all
	//when it caught noone - in which case no line is emitted.
	private void LogDamages ( Dictionary<WeaponEquipmentData.DamageType, int> _damages, LogConsole.LogDetails _details, Entity _targetEntity, int _attackIndex )
	{
		int totalDamage = 0;
		foreach (int value in _damages.Values)
			totalDamage += value;

		LocalizationManager localization = LocalizationManager.Instance;

		if (Data.aoeType == EntityActionData.AOEType.Noone)
		{
			LogConsole.AddLog(string.Format(localization.Get(LocalizationKey.log_damages), totalDamage), LogConsole.LogEventType.Damage, _details);
			return;
		}

		foreach (Entity caughtEntity in GetEntitiesCaughtInAoEAt(_attackIndex))
		{
			LogConsole.AddLog(string.Format(localization.Get(LocalizationKey.log_damages_on_target), caughtEntity.Data.name, totalDamage)
				, LogConsole.LogEventType.Damage
				, _details);
		}
	}

	//Mirrors Weapon.GetEntityCaughtOn without its outline and target-list side effects, so the blast can be
	//reported from Prepare, where every other line of this attack is emitted.
	private List<Entity> GetEntitiesCaughtInAoEAt ( int _attackIndex )
	{
		List<Entity> caught = new();
		Tile aoeCenter = GetTargetTileAt(_attackIndex);
		if (aoeCenter == null)
			return caught;

		HashSet<Tile> zone = PerformingEntity.Equipment.GetTilesInAoERange(this, aoeCenter).ToHashSet();

		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity.Equipment.IsDead || caught.Contains(entity))
					continue;

				if (zone.Contains(GetExchangeTileOf(entity)))
					caught.Add(entity);
			}
		}

		return caught;
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (targetedEntityIDs != null || (Data.aoeType != EntityActionData.AOEType.Noone && targetTileIDs != null))
		{
			//targetedEntityID = PerformingEntity.AI.TargetedEntity.ID;
			LogUseWeapon();

			for (int attackCount = 0; attackCount < attacksInfos.Length; attackCount++)
			{
				SingleAttackInfo attackInfo = attacksInfos[attackCount];

				Entity targetEntity = GetTargetEntityAt(attackCount);
				Tile coverHitted = null;
				if (Data.aoeType != EntityActionData.AOEType.Noone)
				{
					attackInfo.isAttackSuccessfull = true;

					Tile shooterTile = PerformingEntity.Displacement.Coordinates.GetTile();
					Tile aoeCenter = GridManager.Instance.Tiles[targetTileIDs[attackCount * ActiveLifetime]];
					bool isBlockedByWall = GridManager.Instance.IsThereBlockingWallBetween(shooterTile, aoeCenter, true, out coverHitted);

					if (isBlockedByWall)
						targetTileIDs[attackCount * ActiveLifetime] = coverHitted.coordinates.ID;
				}
				else if (targetEntity == null)
					attackInfo.isAttackSuccessfull = true;
				else
					attackInfo.isAttackSuccessfull = PerformingEntity.Equipment.AttackRoll(this, attackInfo, targetEntity, out coverHitted);

				attackInfo.hittedTileID = coverHitted == null ? -1 : coverHitted.coordinates.ID;

				if (attackInfo.isAttackSuccessfull && targetEntity != null)
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

				Dictionary<WeaponEquipmentData.DamageType, int> damagesDealt =
					PerformingEntity.Equipment.Weapons[linkedEquipmentId].GetDamages(PerformingEntity, targetEntity, this
						, GetExchangeResultAgainst(targetEntity), out LogConsole.LogDetails damageDetails);

				if (attackInfo.isAttackSuccessfull)
					LogDamages(damagesDealt, damageDetails, targetEntity, attackCount);

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
			m_tilesInRange = user.Equipment.GetTilesInWeaponRange(this, false, from, m_orientations.Count == 0 ? currentOrientation : m_orientations[^1], true).ToHashSet();
		else
		{
			m_tilesInRange = new();
			for (int i = 0; i < 6; i++)
			{
				foreach (Tile tile in user.Equipment.GetTilesInWeaponRange(this, false, from, i, true))
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
	}

	#endregion
}
