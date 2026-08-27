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

	public int supposedPositionAtActionStartID; //tile
	public int positionAtActionEndID; //tile
									  //public int[] statusIds;
	public AEntityPassiveEffect.PassiveEffectContainer[] effects;
	public EntityActionData Data => GameAssets.current.game.entityActionsData[enumID];

	public int preparationDuration = 0;
	public int actualDuration = 1;
	public int cooldownDuration = 0;
	public int TotalDuration => preparationDuration + actualDuration + cooldownDuration;
	public int ActiveLifetime => lifetime - preparationDuration;

	public int lifetime = 0;
	public int timeAtStart = 0;
	public int TimeAtStartPerform => timeAtStart + preparationDuration;
	public int TimeAtEnd => timeAtStart + TotalDuration;
	protected bool m_isPerforming = false;
	public bool IsPerforming => m_isPerforming;

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
		serializer.SerializeValue(ref supposedPositionAtActionStartID);
		serializer.SerializeValue(ref positionAtActionEndID);
		serializer.SerializeValue(ref effects);
		//serializer.SerializeValue(ref statusIds);
		serializer.SerializeValue(ref preparationDuration);
		serializer.SerializeValue(ref actualDuration);
		serializer.SerializeValue(ref cooldownDuration);
		serializer.SerializeValue(ref lifetime);
		serializer.SerializeValue(ref timeAtStart);
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
		TurnManager.Instance.AddTargetTileInCurrentAction(_tile);

		if (_tile.TryGetPlannedItemAt(timeAtStart, out Item item))
			item.Data.OnRegisterInteraction(this, item);

		if (TurnManager.Instance.CurrentActionTargetTiles.Count == (maxTargetAmount * Data.tokenDuration))
		{
			TurnManager.Instance.RegisterAction(performingEntityID, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.CurrentStateTypeSelected);
			TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
		}
	}

	public virtual void ConflictCheckPrewarm ()
	{

	}

	public struct ActionConflictResultInfo
	{
		public bool isFirstActionConflicted;
		public bool isSecondActionConflicted;
	}

	public abstract ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true );

	/// <summary>
	/// called at the beginning of each performing tick
	/// </summary>
	/// <param name="_state"></param>
	public abstract void Prepare ( Entity.EntityState _state );

	//returns true if action fully performed
	public bool PerformTick ( Entity.EntityState _state )
	{
		if (lifetime == 0)
			OnStartPerform(_state);

		//if (lifetime == preparationDuration + 1)
		if (IsPerformingAtTick(timeAtStart + lifetime) || Data.isModAction)
		{
			if(enumID != EntityActionEnumID.Unknowned && enumID != EntityActionEnumID.Wait)
				LogConsole.AddLog(performingEntityID + " performes " + ToString(), LogConsole.LogEventType.ActionResolution);
			Perform(_state);
			return true;
		}
		else
		{
			DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndTick());
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
		lifetime++;

		bool didEndAction = lifetime >= TotalDuration;
		if (didEndAction)
		{
			if (enumID != EntityActionEnumID.Unknowned && enumID != EntityActionEnumID.Wait)
				LogConsole.AddLog("end " + ToString() + " with lifetime at " + lifetime, LogConsole.LogEventType.ActionResolution);
			EndAction();
		}
		else if (enumID != EntityActionEnumID.Unknowned && enumID != EntityActionEnumID.Wait)
			LogConsole.AddLog(performingEntityID + " in " + (lifetime <= preparationDuration ? "preparation " : "cooldown ") + ToString(), LogConsole.LogEventType.ActionResolution);

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

	public virtual void OnModActionAdded ( AEntityAction _mainAction )
	{

	}

	public abstract void Display ( TurnManager.RecordedAction _recordedAction );

	public abstract void GhostDisplay ( Entity.EntityState _state );

	public override string ToString ()
	{
		return /*PerformingEntity.Data.name + "," + */enumID.ToString();
	}
}
