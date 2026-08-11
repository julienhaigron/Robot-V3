using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class AttackAction : AEntityAction
{
	public SingleAttackInfo[] attacksInfos;

	//for client only
	private HashSet<Tile> m_tilesInRange;
	private int m_orientation;

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
		int maxTargetAmount = Data.GetMaxTargetAmount(this, PerformingEntity, null);
		m_tilesInRange = null;

		if (maxTargetAmount > 1 && TurnManager.Instance.CurrentActionTargetTiles.Count > 1)
		{
			int currentOrientation = GridManager.Instance.GetClosestOrientation(from, TurnManager.Instance.CurrentActionTargetTiles[0]);
			m_tilesInRange = user.Equipment.GetTilesInWeaponRange(this, false, from, currentOrientation).ToHashSet();
		}
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
		if (m_tilesInRange.Contains(_tile))
		{
			Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
			m_orientation = GridManager.Instance.GetClosestOrientation(from, _tile);
			return true;
		}
		return false;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		int currentOrientation = TurnManager.Instance.GetLastRegisteredOrientationOfEntity(performingEntityID);
		if (GridManager.Instance.GetClosestOrientation(from, _tile) == currentOrientation)
		{
			base.RegisterInteraction(_tile);
		}
		else
		{
			int maxTargetAmount = Data.GetMaxTargetAmount(this, PerformingEntity, _tile.GetEntity(true));
			TurnManager.Instance.AddTargetTileInCurrentAction(_tile);

			if (_tile.TryGetPlannedItemAt(timeAtStart, out Item item))
				item.Data.OnRegisterInteraction(this, item);

			if (TurnManager.Instance.CurrentActionTargetTiles.Count == maxTargetAmount)
			{
				RotateEntityAction modAction = TurnManager.Instance.GetAction(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateEntity], performingEntityID, null, timeAtStart) as RotateEntityAction;
				TurnManager.Instance.RegisterActionAndMod(performingEntityID, TurnManager.Instance.CurrentActionSelected, modAction, TurnManager.Instance.CurrentStateTypeSelected);
				TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
			}
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
