using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public abstract class AEntityAction : INetworkSerializable
{
	public Action onPerform;
	public Action<int, bool> onEndTick;

	public EntityActionEnumID enumID;
	public int performingEntityID; //entity
	public Entity PerformingEntity => GameManager.Instance.GetEntityFromID(performingEntityID);
	public string linkedEquipmentId;

	public int[] targetedEntityIDs;
	public int[] targetTileIDs;
	public int[] designatedEntityIDs = Array.Empty<int>();
	public int[] designatedItemIDs = Array.Empty<int>();
	public bool HasDesignatedEntities => designatedEntityIDs != null && designatedEntityIDs.Length > 0;
	public bool HasDesignatedItem => designatedItemIDs != null && designatedItemIDs.Length > 0;
	public bool HasDesignatedTargets => HasDesignatedEntities || HasDesignatedItem;
	public Item DesignatedItem => HasDesignatedItem ? GameManager.Instance.GetItemFromID(designatedItemIDs[0]) : null;

	public int supposedPositionAtActionStartID; //tile
	public int positionAtActionEndID; //tile
	public AEntityPassiveEffect.PassiveEffectContainer[] effects;
	public EntityActionData Data => GameAssets.current.game.entityActionsData[enumID];
	public virtual bool IsPlayedFirst => Data.isPlayedFirst;

	public int preparationDuration = 0;
	public int actualDuration = 1;
	public int cooldownDuration = 0;
	public int TotalDuration => preparationDuration + actualDuration + cooldownDuration;
	public int ActiveLifetime => lifetime - preparationDuration;

	public int lifetime = 0;
	public bool doesFreeActionOwnFacing;
	public int orientationAtPerform = -1;
	public float tickTimeReserve;
	public float TickTimeLeft => TurnManager.GetTickTimeLeft(tickTimeReserve);
	public bool wasReplacedByAI;

	public string TagLogMessage ( string _message )
	{
		return wasReplacedByAI ? "<b>" + _message + "</b>" : _message;
	}
	public int timeAtStart = 0;
	public int TimeAtStartPerform => timeAtStart + preparationDuration;
	public bool IsLastPerformTick => lifetime >= preparationDuration + actualDuration - 1;
	public int TimeAtEnd => timeAtStart + TotalDuration;
	protected bool m_isPerforming = false;
	public bool IsPerforming => m_isPerforming;
	private bool m_isWaitingForEndTick;
	private VisualProjectile m_movementTrail;

	public bool IsPerformingAtTick ( int _tick )
	{
		return _tick >= timeAtStart + preparationDuration && _tick < timeAtStart + preparationDuration + actualDuration;
	}

	public virtual void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
	{
		serializer.SerializeValue(ref enumID);
		serializer.SerializeValue(ref performingEntityID);
		serializer.SerializeValue(ref linkedEquipmentId);
		serializer.SerializeValue(ref targetedEntityIDs);
		serializer.SerializeValue(ref targetTileIDs);
		serializer.SerializeValue(ref designatedEntityIDs);
		serializer.SerializeValue(ref designatedItemIDs);
		serializer.SerializeValue(ref supposedPositionAtActionStartID);
		serializer.SerializeValue(ref positionAtActionEndID);
		serializer.SerializeValue(ref effects);
		serializer.SerializeValue(ref preparationDuration);
		serializer.SerializeValue(ref actualDuration);
		serializer.SerializeValue(ref cooldownDuration);
		serializer.SerializeValue(ref lifetime);
		serializer.SerializeValue(ref doesFreeActionOwnFacing);
		serializer.SerializeValue(ref orientationAtPerform);
		serializer.SerializeValue(ref wasReplacedByAI);
		serializer.SerializeValue(ref timeAtStart);
		serializer.SerializeValue(ref m_isPerforming);
	}

	public virtual void Init ( EntityActionData _data, string _linkedEquipmentID, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
		Entity performingEntity = GameManager.Instance.GetEntityFromID(_performingEntityID);
		enumID = _data.enumID;
		performingEntityID = _performingEntityID;
		linkedEquipmentId = _linkedEquipmentID;
		supposedPositionAtActionStartID = _positionAtActionStartID;
		positionAtActionEndID = _positionAtActionStartID;

		effects = performingEntity.KnownedPassiveEffectsPerAction == null || !performingEntity.KnownedPassiveEffectsPerAction.ContainsKey(enumID)
			? null : performingEntity.KnownedPassiveEffectsPerAction[enumID].ToArray();

		preparationDuration = _data.GetTokenPreparationCost(this, performingEntity, null);
		actualDuration = _data.tokenDuration;
		cooldownDuration = _data.GetTokenCooldownCost(this, performingEntity, null);

		timeAtStart = _timeAtStart;
	}

	public virtual void OnSelectActionTileInteractPredicatePrewarm ()
	{

	}

	public abstract bool TileInteractPredicate ( Tile _tile );

	public virtual void RegisterInteraction ( Tile _tile )
	{
		int maxTargetAmount = Data.GetMaxTargetAmount(this, PerformingEntity, _tile.GetEntity(true));
		TurnManager.Instance.AddTargetTileInCurrentAction(_tile, this);

		if (_tile.TryGetPlannedItemAt(timeAtStart, out Item item))
			item.Data.OnRegisterInteraction(this, item);

		if (TurnManager.Instance.CurrentActionTargetTiles.Count == (maxTargetAmount * Data.tokenDuration))
		{
			TurnManager.Instance.RegisterAction(performingEntityID, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.GetDefaultStateOf(performingEntityID));
			TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
		}
	}

	public virtual void SetResolvedTargets ( int[] _targetTileIDs, int[] _targetedEntityIDs )
	{
		targetTileIDs = _targetTileIDs;
		targetedEntityIDs = _targetedEntityIDs;
	}

	public bool IsMissingTarget ()
	{
		return Data.DoesNeedATargetTile() && (targetTileIDs == null || targetTileIDs.Length == 0);
	}

	public bool IsDesignated ( int _entityID )
	{
		return HasDesignatedEntities && Array.IndexOf(designatedEntityIDs, _entityID) >= 0;
	}

	public bool IsDesignated ( Item _item )
	{
		return _item != null && HasDesignatedItem && designatedItemIDs[0] == _item.ID;
	}

	public void ToggleDesignatedItem ( Item _item )
	{
		designatedEntityIDs = Array.Empty<int>();
		designatedItemIDs = IsDesignated(_item) ? Array.Empty<int>() : new[] { _item.ID };
	}

	public bool RemoveDesignatedItem ( Item _item )
	{
		if (!IsDesignated(_item))
			return false;

		designatedItemIDs = Array.Empty<int>();
		return true;
	}

	public List<Tile> GetDesignatedTargetTiles ()
	{
		List<Tile> tiles = new();
		foreach (int entityID in designatedEntityIDs)
		{
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			if (entity != null && !entity.Equipment.IsDead)
				tiles.Add(entity.Displacement.Coordinates.GetTile());
		}

		Item item = DesignatedItem;
		if (item != null && item.Health > 0)
			tiles.Add(item.CurrentPosition);
		return tiles;
	}

	public List<string> GetDesignatedTargetNames ()
	{
		List<string> names = new();
		foreach (int entityID in designatedEntityIDs)
		{
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			if (entity != null)
				names.Add(entity.Data.name);
		}

		Item item = DesignatedItem;
		if (item != null)
			names.Add(item.Data.GetLocalizedName());
		return names;
	}

	public void ToggleDesignatedTarget ( int _entityID, int _maxTargetAmount )
	{
		designatedItemIDs = Array.Empty<int>();
		List<int> designated = new(designatedEntityIDs ?? Array.Empty<int>());
		if (!designated.Remove(_entityID))
		{
			if (designated.Count >= Mathf.Max(1, _maxTargetAmount))
				designated.RemoveAt(0);
			designated.Add(_entityID);
		}

		designatedEntityIDs = designated.ToArray();
	}

	public bool RemoveDesignatedTarget ( int _entityID )
	{
		if (!IsDesignated(_entityID))
			return false;

		List<int> designated = new(designatedEntityIDs);
		designated.Remove(_entityID);
		designatedEntityIDs = designated.ToArray();
		return true;
	}

	public void DisplayAoEPreviewOnHoveredTile ()
	{
		Tile hoveredTile = PlayerController.Instance == null ? null : PlayerController.Instance.HoveredTile;
		if (hoveredTile == null || Data.aoeType == EntityActionData.AOEType.Noone || PerformingEntity == null)
			return;

		foreach (Tile tile in PerformingEntity.Equipment.GetTilesInAoERange(this, hoveredTile, true, _usePlannedPose: true))
			PlayerController.Instance.AddAoEPreviewOutline(tile);
	}

	public virtual void ConflictCheckPrewarm ()
	{

	}

	public struct ActionConflictResultInfo
	{
		public bool isFirstActionConflicted;
		public bool isSecondActionConflicted;
	}

	public virtual ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		return default;
	}

	/// <summary>
	/// CheckForConflicts only ever pairs actions coming from two different entities, so an action with nobody
	/// to be paired with this tick is never checked and never books its target tiles. Such an action must still
	/// validate itself, otherwise it moves onto tiles no one ever looked at. Returns true if it is in conflict.
	/// </summary>
	public virtual bool ConflictCheckAlone ( bool _isCheck = true )
	{
		return false;
	}

	/// <summary>
	/// Does this action take the performing entity off _tileID during this tick? Only real movement does, so the
	/// default is no. Each movement action answers for itself because each has its own cancellation rules, and
	/// because positionAtActionEndID is not maintained by every action type: inferring the answer from it made an
	/// attacking, motionless unit look like it was leaving, and the next unit walked straight through it.
	/// </summary>
	public virtual bool DoesLeaveTileThisTick ( int _tileID )
	{
		return false;
	}

	/// <summary>
	/// called at the beginning of each performing tick
	/// </summary>
	/// <param name="_state"></param>
	public virtual void Prepare ( Entity.EntityState _state )
	{

	}

	//returns true if action fully performed
	public bool PerformTick ( Entity.EntityState _state )
	{
		m_isWaitingForEndTick = true;
		if (lifetime == 0)
			OnStartPerform(_state);
		PerformingEntity.StartActionTick(this);

		if (IsPerformingAtTick(TurnManager.currentTick) || Data.isModAction)
		{
			if(enumID != EntityActionEnumID.Unknowned && enumID != EntityActionEnumID.Wait)
				LogConsole.AddLog(PerformingEntity.Data.name + " performes " + ToString(), LogConsole.LogEventType.ActionResolution);
			Perform(_state);
			return true;
		}
		else
		{
			DG.Tweening.DOVirtual.DelayedCall(TickTimeLeft, () => EndTick());
			return false;
		}
	}

	protected virtual void Perform ( Entity.EntityState _state )
	{
		onPerform?.Invoke();
	}

	public virtual void OnStartPerform ( Entity.EntityState _state )
	{
		m_isPerforming = true;
		PerformingEntity.StartPerformAction(this, _state);
	}

	protected virtual void EndTick ()
	{
		if (!m_isPerforming || !m_isWaitingForEndTick)
			return;

		m_isWaitingForEndTick = false;
		lifetime++;

		bool didEndAction = lifetime >= TotalDuration;
		if (didEndAction)
		{
			if (enumID != EntityActionEnumID.Unknowned && enumID != EntityActionEnumID.Wait)
				LogConsole.AddLog("end " + ToString() + " with lifetime at " + lifetime, LogConsole.LogEventType.ActionResolution);
			EndAction();
		}
		else if (enumID != EntityActionEnumID.Unknowned && enumID != EntityActionEnumID.Wait)
			LogConsole.AddLog(PerformingEntity.Data.name + " in " + (lifetime <= preparationDuration ? "preparation " : "cooldown ") + ToString(), LogConsole.LogEventType.ActionResolution);

		PerformingEntity.EndTick();
		onEndTick?.Invoke(performingEntityID, didEndAction);
	}

	protected virtual void EndAction ()
	{
		m_isPerforming = false;
		PerformingEntity.EndPerformAction();
	}

	public virtual void CancelAction ()
	{

	}

	protected void AttachMovementTrail ()
	{
		DetachMovementTrail();
		Entity performer = PerformingEntity;
		if (performer != null && performer.Skin != null && (performer.IsAlliedTo(GameManager.Instance.PlayerID) || performer.IsVisible))
			m_movementTrail = VisualProjectile.AttachTo(Data.movementTrailPool, performer.Skin.Center);
	}

	protected void DetachMovementTrail ()
	{
		if (m_movementTrail == null)
			return;

		m_movementTrail.Detach();
		m_movementTrail = null;
	}

	public virtual void OnModActionAdded ( AEntityAction _mainAction )
	{

	}

	public virtual void Display ( TurnManager.RecordedAction _recordedAction )
	{

	}

	public virtual void GhostDisplay ( Entity.EntityState _state )
	{

	}

	protected static bool WinsTieBreak ( EntityActionData.PFCResultType _result = EntityActionData.PFCResultType.Equal )
	{
		return _result == EntityActionData.PFCResultType.FirstWins
			|| (_result != EntityActionData.PFCResultType.SecondWins && UnityEngine.Random.Range(0, 2) == 0);
	}

	protected List<string> GetTargetedEntityNames ()
	{
		List<string> targetNames = new();
		if (targetedEntityIDs == null)
			return targetNames;

		foreach (int targetID in targetedEntityIDs)
		{
			Entity target = GameManager.Instance.GetEntityFromID(targetID);
			if (target != null && !targetNames.Contains(target.Data.name))
				targetNames.Add(target.Data.name);
		}

		return targetNames;
	}

	public override string ToString ()
	{
		return enumID.ToString();
	}
}
