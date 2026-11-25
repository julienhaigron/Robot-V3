using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;

public class Entity : MonoBehaviour
{
	public Action onSelect;
	public Action onDeselect;
	public Action<AEntityAction> onStartPerformAction;
	public Action onEndPerformAction;
	public Action<AEntityAction> onStartActionTick;
	public Action onEndTick;
	public Action onNewRoundBegin;
	public Action<EntityStatusEnumID> onStatusAdded;
	public Action<EntityStatusEnumID> onStatusRemoved;
	public Action<EntityEquipmentData.StatBonusBuff> onStatBonusAdded;
	public Action<EntityEquipmentData.StatBonusBuff> onStatBonusRemoved;

	[Title("Depedencies")]
	[SerializeField] private GameObject m_skinParent;
	public GameObject SkinParent => m_skinParent;

	[SerializeField] private EntityDisplacementPlugin m_displacement;
	public EntityDisplacementPlugin Displacement => m_displacement;

	[SerializeField] private EntityEquipmentPlugin m_equipment;
	public EntityEquipmentPlugin Equipment => m_equipment;

	[SerializeField] private EntityAIPlugin m_ai;
	public EntityAIPlugin AI => m_ai;
	[SerializeField] private EntitySkinPlugin m_skin;
	public EntitySkinPlugin Skin => m_skin;

	[SerializeField] private EntityUIPlugin m_ui;
	public EntityUIPlugin UI => m_ui;

	[SerializeField] private EntitySavedData m_data;

	public EntitySavedData Data => m_data;
	public bool IsImmortal => m_data != null && m_data.FrameData != null && m_data.FrameData.isImmortal;

	private List<EntityActionEnumID> m_knownedActions = new();
	public List<EntityActionEnumID> KnownedActions => m_knownedActions;

	private List<EntityActionEnumID> m_knowedModActions = new();
	public List<EntityActionEnumID> KnownedModActions => m_knowedModActions;

	private Dictionary<EntityActionEnumID, List<string>> m_componentLinkedToAction = new();
	public Dictionary<EntityActionEnumID, List<string>> ComponentLinkedToAction => m_componentLinkedToAction;

	private Dictionary<EntityActionEnumID, List<AEntityPassiveEffect.PassiveEffectContainer>> m_knownedPassiveEffectsPerAction = new();
	public Dictionary<EntityActionEnumID, List<AEntityPassiveEffect.PassiveEffectContainer>> KnownedPassiveEffectsPerAction => m_knownedPassiveEffectsPerAction;
	private List<AEntityPassiveEffect.PassiveEffectContainer> m_allPassiveEffects = new();
	public List<AEntityPassiveEffect.PassiveEffectContainer> AllPassiveEffects => m_allPassiveEffects;

	private List<EntityState> m_knownedStates = new();

	private EntityState m_state;
	public EntityState State => m_state;

	private EntityState m_defaultState = EntityState.NoAIChange;
	public EntityState DefaultState => m_defaultState;

	private List<EntityStatusEnumID> m_status = new();
	public List<EntityStatusEnumID> Status => m_status;
	private Dictionary<AEntityStatus, int> m_remainingDurationToActiveStatuses = new();
	private readonly Dictionary<EntityStatusEnumID, GameObject> m_statusVisuals = new();
	private const float c_aboveHeadHeight = 1.4f;
	private const float c_feetHeight = .03f;
	private Dictionary<AEntityStatus, int> m_appliedDurationToActiveStatuses = new();
	private readonly Dictionary<AEntityStatus, float> m_statusDamageFactors = new();
	private List<EntityEquipmentData.StatBonusBuff> m_statBuffs = new();
	private readonly List<EntityEquipmentPlugin.TakeDamageCallback> m_pendingEchoes = new();
	private Dictionary<EntityEquipmentData.SecondaryStat.StatType, float> m_activeStatBonusBuffs = new();

	private int m_ownerID;
	public int OwnerID => m_ownerID;

	public enum MatchRoleType { None, Demolisher, Technician }
	private MatchRoleType m_matchRole = MatchRoleType.None;
	public MatchRoleType MatchRole => m_matchRole;
	public void SetMatchRole ( MatchRoleType _matchRole ) => m_matchRole = _matchRole;

	[SerializeField, ReadOnly] private EntityActionData m_lastActionPerformed;
	public EntityActionData LastActionPerformedData => m_lastActionPerformed == null ? GameConfig.current.game.defaultStartAction : m_lastActionPerformed;
	public float PreviousActionAttackModificator => LastActionPerformedData != null ? LastActionPerformedData.previousActionAttackModificator : 0f;
	private AEntityAction m_lastPerformedAction;
	public AEntityAction LastPerformedAction => m_lastPerformedAction;

	public int ID;

	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;
	private NeuronalMembraneEquipmentData.VisionTypes m_howIsUnitVisible = NeuronalMembraneEquipmentData.VisionTypes.Radar;
	public NeuronalMembraneEquipmentData.VisionTypes HowIsUnitVisible => m_howIsUnitVisible;
	public bool IsShownToLocalPlayer => IsAlliedTo(GameManager.Instance.PlayerID)
		|| (m_isVisible && m_howIsUnitVisible != NeuronalMembraneEquipmentData.VisionTypes.Radar);


	[Serializable]
	public enum EntityState
	{
		NoAIChange,
		Patroling,
		Fleeing //to add
	}

	private void Awake ()
	{
		TurnManager.onNewRoundStart += OnRoundStart;
	}

	private void OnDestroy ()
	{
		TurnManager.onNewRoundStart -= OnRoundStart;
	}

	public void Init ( EntitySavedData _data, EntityAnchor.Spawn _spawn, int _id, int _playerID )
	{
		ID = _id;
		m_ownerID = _playerID;
		m_data = _data;
		Displacement.SetSpawn(_spawn);

		m_equipment.Init(_data);
		m_ui.Init(_data);
		m_skin.Init(_data);

		m_componentLinkedToAction = GetAllActions();
		foreach (EntityActionEnumID actionID in m_componentLinkedToAction.Keys)
		{
			if (GameAssets.current.game.entityActionsData.TryGetValue(actionID, out EntityActionData actionData))
				(actionData.isModAction ? m_knowedModActions : m_knownedActions).Add(actionID);
		}

		m_ai.Init(_data);
		foreach (EntityActionEnumID actionID in m_knownedActions)
		{
			m_knownedPassiveEffectsPerAction.Add(actionID, _data.GetPassiveEffects(actionID));
		}
		foreach (EntityActionEnumID actionID in m_knowedModActions)
		{
			if (!m_knownedPassiveEffectsPerAction.ContainsKey(actionID))
				m_knownedPassiveEffectsPerAction.Add(actionID, _data.GetPassiveEffects(actionID));
		}

		foreach (GameDatas.PlayerSave.Component eq in _data.GetAllEquipments())
		{
			m_allPassiveEffects.AddRange(eq.GetData<EntityEquipmentData>().passiveEffects);
		}

		m_knownedStates.AddRange(GameAssets.current.game.states);
		m_defaultState = EntityState.NoAIChange;
	}

	public void SetDefaultState ( EntityState _state )
	{
		if (!m_knownedStates.Contains(_state))
			return;

		m_defaultState = _state;
	}

	private Dictionary<EntityActionEnumID, List<string>> GetAllActions ()
	{
		Dictionary<EntityActionEnumID, List<string>> actionsPerComponents = new();
		AddActionsOf(actionsPerComponents, m_data.FrameData.knownedActions, m_data.FrameData.name, false);
		AddActionsOf(actionsPerComponents, m_data.ReactorData.knownedActions, m_data.ReactorData.name, false);
		AddActionsOf(actionsPerComponents, m_data.NeuronalMembraneData.knownedActions, m_data.NeuronalMembraneData.name, false);
		AddActionsOf(actionsPerComponents, m_data.BrainData.knownedActions, m_data.BrainData.name, false);
		AddActionsOf(actionsPerComponents, m_data.arms, true);
		AddActionsOf(actionsPerComponents, m_data.auxiliar, false);
		AddActionsOf(actionsPerComponents, m_data.chipsets, false);

		if (!actionsPerComponents.ContainsKey(EntityActionEnumID.Wait))
			actionsPerComponents.Add(EntityActionEnumID.Wait, new() { null });

		return actionsPerComponents;
	}

	private static void AddActionsOf ( Dictionary<EntityActionEnumID, List<string>> _actionsPerComponents, GameDatas.PlayerSave.Component[] _containers, bool _isNamedByContainerID )
	{
		foreach (GameDatas.PlayerSave.Component container in _containers ?? Array.Empty<GameDatas.PlayerSave.Component>())
		{
			if (container != null && !container.isDamaged && container.TryGetData(out EntityEquipmentData equipment))
				AddActionsOf(_actionsPerComponents, equipment.knownedActions, _isNamedByContainerID ? container.ID : equipment.name, true);
		}
	}

	private static void AddActionsOf ( Dictionary<EntityActionEnumID, List<string>> _actionsPerComponents, IEnumerable<EntityActionEnumID> _actionIDs, string _componentID, bool _isFirstComponentKept )
	{
		foreach (EntityActionEnumID actionID in _actionIDs)
		{
			if (actionID == EntityActionEnumID.Unknowned)
				continue;

			if (!_actionsPerComponents.TryGetValue(actionID, out List<string> components))
				_actionsPerComponents.Add(actionID, components = new());
			else if (_isFirstComponentKept)
				continue;

			components.Add(_componentID);
		}
	}

	public void InitHangarMode ( EntitySavedData _data )
	{
		m_data = _data;
		m_equipment.Init(_data);
		m_skin.Init(_data);
		m_ui.InitHangarMode(_data);
	}

	public void QueueEchoDamage ( Entity _attacker, Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		m_pendingEchoes.Add(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			entityAttacker = _attacker,
			entityTargeted = this,
			damages = new(_damages)
		});
	}

	private void ApplyPendingEchoes ()
	{
		if (m_pendingEchoes.Count == 0)
			return;

		EntityEquipmentPlugin.TakeDamageCallback[] echoes = m_pendingEchoes.ToArray();
		m_pendingEchoes.Clear();
		foreach (EntityEquipmentPlugin.TakeDamageCallback echo in echoes)
		{
			if (m_equipment.IsDead)
				return;

			EntityEquipmentPlugin.TakeDamageCallback damageInfo = echo;
			if (damageInfo.entityAttacker == null || damageInfo.entityAttacker.Equipment.IsDead)
				damageInfo.entityAttacker = null;

			int total = 0;
			foreach (int damage in damageInfo.damages.Values)
				total += damage;
			LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_echo_damage), m_data.name, total), LogConsole.LogEventType.Status);
			m_equipment.TakeDamage(damageInfo);
		}
	}

	public void OnHitReceived ( AttackAction _action, Entity _attacker )
	{
		foreach (EntityStatusEnumID statusID in m_status.ToArray())
			GameAssets.current.game.entityStatus[statusID].OnHitReceived(_action, this, _attacker);
	}

	private void OnRoundStart ()
	{
		onNewRoundBegin?.Invoke();
		RecordPosition();
		ApplyPendingEchoes();

		foreach (EntityStatusEnumID statusID in m_status.ToArray())
		{
			AEntityStatus status = GameAssets.current.game.entityStatus[statusID];
			if (m_remainingDurationToActiveStatuses.ContainsKey(status)
				&& m_remainingDurationToActiveStatuses[status] <= 0)
			{
				RemoveStatus(statusID);
			}
			else
			{
				int remainingDuration = 0;
				if (m_remainingDurationToActiveStatuses.ContainsKey(status))
				{
					remainingDuration = m_remainingDurationToActiveStatuses[status];
					if (status.DoesConsumeDurationThisTick())
						m_remainingDurationToActiveStatuses[status]--;
				}

				status.ApplyStatusEffect(remainingDuration, this);

				if (!status.DoesConsumeDurationThisTick())
					continue;

				LogStatus(status, GetAppliedStatusDuration(status), remainingDuration);

				string tickEffect = status.GetTickEffectText(remainingDuration, this);
				if (!string.IsNullOrEmpty(tickEffect))
					LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_effect), tickEffect), LogConsole.LogEventType.Status);
			}
		}

		m_ui.RefreshStatusDurations();

		foreach (EntityEquipmentData.StatBonusBuff buff in m_statBuffs.ToArray())
		{
			buff.duration--;
			if (buff.duration <= 0)
				RemoveAdditionaryStatBonus(buff);
		}
	}

	public void StartPerformAction ( AEntityAction _action, EntityState _state )
	{
		TurnManager.Instance.TrackEntireGameAction(ID, _action.enumID);

		if (!_action.Data.isModAction)
		{
			m_lastActionPerformed = _action.Data;
			m_lastPerformedAction = _action;
		}

		m_state = _state;
		onStartPerformAction?.Invoke(_action);
	}

	public void StartActionTick ( AEntityAction _action )
	{
		onStartActionTick?.Invoke(_action);
	}

	public void EndTick ()
	{
		onEndTick?.Invoke();
	}

	public void EndPerformAction ()
	{
		onEndPerformAction?.Invoke();
	}

	public bool IsAlliedTo ( int _playerOwnerId )
	{
		return m_ownerID == _playerOwnerId;
	}

	public void Select ()
	{
		onSelect?.Invoke();
	}

	public void Deselect ()
	{
		onDeselect?.Invoke();
	}

	public void SetVisibility ( bool _isVisible, NeuronalMembraneEquipmentData.VisionTypes _visionType )
	{
		bool wasVisible = m_isVisible;
		m_isVisible = _isVisible;
		if (_isVisible)
			m_howIsUnitVisible = _visionType;
		m_ui.gameObject.SetActive(m_isVisible && (int)m_howIsUnitVisible < (int)NeuronalMembraneEquipmentData.VisionTypes.Radar);
		if (m_isVisible)
			m_skin.Show(m_howIsUnitVisible);
		else
			m_skin.Hide();

		LastSeenMarkers.Instance.OnVisibilityChanged(this, wasVisible);
	}

	public int GetRemainingStatusDuration ( EntityStatusEnumID _statusID )
	{
		return m_remainingDurationToActiveStatuses.TryGetValue(GameAssets.current.game.entityStatus[_statusID], out int duration) ? duration : 0;
	}

	private int GetAppliedStatusDuration ( AEntityStatus _status )
	{
		return m_appliedDurationToActiveStatuses.TryGetValue(_status, out int duration) ? duration : 0;
	}

	public void AddStatus ( EntityStatusEnumID _statusID, int _baseDuration, Entity _source = null )
	{
		AEntityStatus status = GameAssets.current.game.entityStatus[_statusID];
		bool isCharged = status.durationType == AEntityStatus.DurationType.Charges;
		int duration = isCharged ? _baseDuration : GetStatusDuration(_baseDuration, _source);
		if (duration <= 0 && _baseDuration > 0)
			return;

		bool wasAlreadyActive = m_status.Contains(_statusID);
		if (!wasAlreadyActive)
			m_status.Add(_statusID);
		else if (isCharged && status.doesStackCharges)
			duration += GetRemainingStatusDuration(_statusID);

		m_remainingDurationToActiveStatuses[status] = duration;
		m_appliedDurationToActiveStatuses[status] = duration;
		if (_source != null || !m_statusDamageFactors.ContainsKey(status))
			m_statusDamageFactors[status] = 1f + (_source == null ? 0f : _source.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusDamageBonus));

		if (wasAlreadyActive)
		{
			m_ui.RefreshStatusDurations();
			return;
		}

		ShowStatusVisual(_statusID, status);

		LogStatus(status, duration, duration);

		onStatusAdded?.Invoke(_statusID);
	}

	private int GetStatusDuration ( int _baseDuration, Entity _source )
	{
		float raise = _source == null ? 0f : _source.GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusAppliedDurationRaise);
		float reduction = _source == this ? 0f : GetStatBonus(EntityEquipmentData.SecondaryStat.StatType.StatusDurationReduction);
		return Mathf.Max(0, Mathf.FloorToInt(_baseDuration * (1f + raise - reduction) + .5f));
	}

	public float GetStatusDamageFactor ( EntityStatusEnumID _statusID )
	{
		return m_statusDamageFactors.TryGetValue(GameAssets.current.game.entityStatus[_statusID], out float factor) ? factor : 1f;
	}

	public void ClaimStatusCharges ( AEntityAction _action )
	{
		if (_action.hasClaimedStatusCharges)
			return;

		_action.hasClaimedStatusCharges = true;
		List<short> boostingStatusIDs = new();
		foreach (EntityStatusEnumID statusID in m_status.ToArray())
		{
			AEntityStatus status = GameAssets.current.game.entityStatus[statusID];
			if (!status.IsConsumedBy(_action) || GetRemainingStatusDuration(statusID) <= 0 || (status.isBoundAtPlanning && !_action.IsPlannedWith(statusID)))
				continue;

			boostingStatusIDs.Add((short)statusID);
			ConsumeStatusCharge(statusID);
		}

		_action.boostingStatusIDs = boostingStatusIDs.ToArray();
	}

	private void ConsumeStatusCharge ( EntityStatusEnumID _statusID )
	{
		AEntityStatus status = GameAssets.current.game.entityStatus[_statusID];
		if (!m_remainingDurationToActiveStatuses.TryGetValue(status, out int charges))
			return;

		if (charges <= 1)
		{
			RemoveStatus(_statusID);
			return;
		}

		m_remainingDurationToActiveStatuses[status] = charges - 1;
		m_ui.RefreshStatusDurations();
	}

	public bool HasChargeFor ( EntityStatusEnumID _statusID, AEntityAction _action )
	{
		if (_action == null)
			return false;
		if (_action.hasClaimedStatusCharges)
			return _action.IsBoostedBy(_statusID);
		if (!m_status.Contains(_statusID))
			return false;

		AEntityStatus status = GameAssets.current.game.entityStatus[_statusID];
		return status.isBoundAtPlanning && _action.plannedStatusIDs != null ? _action.IsPlannedWith(_statusID) : IsChargeAvailableFor(status, _action);
	}

	public short[] GetPlanningBoundCharges ( AEntityAction _action )
	{
		List<short> plannedStatusIDs = new();
		foreach (EntityStatusEnumID statusID in m_status)
		{
			AEntityStatus status = GameAssets.current.game.entityStatus[statusID];
			if (status.isBoundAtPlanning && IsChargeAvailableFor(status, _action))
				plannedStatusIDs.Add((short)statusID);
		}

		return plannedStatusIDs.ToArray();
	}

	private bool IsChargeAvailableFor ( AEntityStatus _status, AEntityAction _action )
	{
		return _status.IsConsumedBy(_action)
			&& GetRemainingStatusDuration(_status.enumID) > TurnManager.Instance.CountUnclaimedQueuedActionsBefore(ID, _action, _queued => DoesReserveCharge(_status, _queued));
	}

	private static bool DoesReserveCharge ( AEntityStatus _status, AEntityAction _queued )
	{
		return _status.IsConsumedBy(_queued) && (!_status.isBoundAtPlanning || _queued.IsPlannedWith(_status.enumID));
	}

	private void LogStatus ( AEntityStatus _status, int _duration, int _remaining )
	{
		LocalizationKey key = _status.durationType switch
		{
			AEntityStatus.DurationType.Tick => LocalizationKey.log_status_affected_ticks,
			AEntityStatus.DurationType.Round => LocalizationKey.log_status_affected_rounds,
			_ => LocalizationKey.log_status_affected_charges
		};
		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(key), m_data.name, _status.GetLocalizedName(), _remaining)
			, LogConsole.LogEventType.Status
			, new LogConsole.LogDetails(LogCard.Status(_status, m_data.name, _duration, _remaining)));
	}

	public void RemoveStatus ( EntityStatusEnumID _statusID )
	{
		GameAssets.current.game.entityStatus[_statusID].OnRemoveStatusEffect(this);
		m_status.Remove(_statusID);
		m_remainingDurationToActiveStatuses.Remove(GameAssets.current.game.entityStatus[_statusID]);
		m_appliedDurationToActiveStatuses.Remove(GameAssets.current.game.entityStatus[_statusID]);
		m_statusDamageFactors.Remove(GameAssets.current.game.entityStatus[_statusID]);
		HideStatusVisual(_statusID);

		onStatusRemoved?.Invoke(_statusID);
	}

	private void ShowStatusVisual ( EntityStatusEnumID _statusID, AEntityStatus _status )
	{
		if (_status.entityPrefab == null || m_skin == null || m_statusVisuals.ContainsKey(_statusID))
			return;

		GameObject visual = Instantiate(_status.entityPrefab, m_skin.StatusVisualParent);
		visual.transform.position = GetStatusVisualPosition(_status.entityVisualAnchor);
		m_statusVisuals.Add(_statusID, visual);
	}

	private void HideStatusVisual ( EntityStatusEnumID _statusID )
	{
		if (!m_statusVisuals.TryGetValue(_statusID, out GameObject visual))
			return;

		if (visual != null)
			Destroy(visual);
		m_statusVisuals.Remove(_statusID);
	}

	private Vector3 GetStatusVisualPosition ( AEntityStatus.EntityVisualAnchor _anchor )
	{
		Vector3 center = m_skin != null && m_skin.Center != null ? m_skin.Center.position : transform.position;
		switch (_anchor)
		{
			case AEntityStatus.EntityVisualAnchor.AboveHead:
				return center + Vector3.up * c_aboveHeadHeight;

			case AEntityStatus.EntityVisualAnchor.Feet:
				int tileID = m_displacement != null ? m_displacement.Coordinates.ID : -1;
				bool hasTile = GridManager.Instance != null && GridManager.Instance.Tiles != null && tileID >= 0 && tileID < GridManager.Instance.Tiles.Length;
				float groundHeight = hasTile ? GridManager.Instance.Tiles[tileID].transform.position.y : transform.position.y;
				return new Vector3(center.x, groundHeight + c_feetHeight, center.z);

			default:
				return center;
		}
	}

	public void AddAdditionaryStatBonus ( EntityEquipmentData.StatBonusBuff _statBuff )
	{
		m_statBuffs.Add(_statBuff);
		if (m_activeStatBonusBuffs.ContainsKey(_statBuff.statBonus.type))
			m_activeStatBonusBuffs[_statBuff.statBonus.type] += _statBuff.statBonus.value;
		else
			m_activeStatBonusBuffs.Add(_statBuff.statBonus.type, _statBuff.statBonus.value);

		onStatBonusAdded?.Invoke(_statBuff);
	}

	public void RemoveAdditionaryStatBonus ( EntityEquipmentData.StatBonusBuff _statBuff )
	{
		m_statBuffs.Remove(_statBuff);
		m_activeStatBonusBuffs[_statBuff.statBonus.type] -= _statBuff.statBonus.value;

		onStatBonusRemoved?.Invoke(_statBuff);
	}

	public float GetStatBonus ( EntityEquipmentData.SecondaryStat.StatType _type )
	{
		return GetStatBonus(_type, TurnManager.Instance.GetActionInProgressOf(ID));
	}

	public float GetStatBonus ( EntityEquipmentData.SecondaryStat.StatType _type, AEntityAction _relatedAction )
	{
		return m_data.GetStatBonusFromAll(_type) + GetAdditionaryStatBonus(_type, _relatedAction);
	}

	public float GetAdditionaryStatBonus ( EntityEquipmentData.SecondaryStat.StatType _type, AEntityAction _relatedAction )
	{
		float bonus = 0f;

		if (m_activeStatBonusBuffs.ContainsKey(_type))
			bonus += m_activeStatBonusBuffs[_type];

		foreach (ChipsetEquipmentData.ConditionalStatBonus conditionalStatBonus in m_data.GetChipsetBonuses())
		{
			if (conditionalStatBonus.bonus.type == _type && Condition.UseConditionPredicate(_relatedAction, this, null, conditionalStatBonus.conditionType))
				bonus += conditionalStatBonus.bonus.value;
		}

		return bonus;
	}

	#region Special abilities

	private const int c_positionHistoryLength = 64;
	private readonly List<int> m_positionHistory = new();
	private int m_illusionSourceID = -1;
	private int m_illusionID = -1;
	public bool IsIllusion => m_illusionSourceID >= 0;
	public Entity Illusion => m_illusionID >= 0 ? GameManager.Instance.GetEntityFromID(m_illusionID) : null;

	private void RecordPosition ()
	{
		m_positionHistory.Add(m_displacement.Coordinates.ID);
		if (m_positionHistory.Count > c_positionHistoryLength)
			m_positionHistory.RemoveAt(0);
	}

	public void Retreat ( int _ticksBack )
	{
		if (m_positionHistory.Count == 0 || m_equipment.IsDead)
			return;

		Tile formerTile = GridManager.Instance.Tiles[m_positionHistory[Mathf.Max(0, m_positionHistory.Count - _ticksBack)]];
		Tile destination = GridManager.Instance.GetNearestFreeTile(formerTile, this);
		if (destination == null || destination == m_displacement.Coordinates.GetTile())
			return;

		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_retreat), m_data.name), LogConsole.LogEventType.ActionResolution);
		m_displacement.TeleportToTile(destination.coordinates.ID, null, true);
		m_displacement.RegisterOnCurrentTile();
	}

	public void SummonIllusion ( int _ticks )
	{
		Entity current = Illusion;
		if (current != null && !current.Equipment.IsDead)
			current.Equipment.InstantDeath(EntityEquipmentPlugin.DeathCause.Vanish);

		Tile tile = GridManager.Instance.GetNearestFreeTile(m_displacement.Coordinates.GetTile(), null);
		if (tile == null || m_equipment.IsDead)
			return;

		EntitySavedData copy = m_data.ShallowCopy();
		copy.currentHp = 1;
		EntityAnchor anchor = GameManager.Instance.PlayersEntityAnchor[m_ownerID];
		int illusionID = anchor.ReserveEntityIDs(m_ownerID, 1);
		anchor.SpawnEntityDuringPlay(copy, illusionID, m_ownerID, tile.coordinates.ID);

		Entity illusion = GameManager.Instance.GetEntityFromID(illusionID);
		illusion.BecomeIllusionOf(this, _ticks);
		m_illusionID = illusionID;
		illusion.Equipment.onDeath += OnIllusionDeath;
		m_equipment.onDeath += OnIllusionSourceDeath;
		LearnAction(EntityActionEnumID.SwapWithIllusion, m_data.reactor != null ? m_data.reactor.ID : string.Empty);
		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_illusion_summoned), m_data.name), LogConsole.LogEventType.ActionResolution);
	}

	private void BecomeIllusionOf ( Entity _source, int _ticks )
	{
		m_illusionSourceID = _source.ID;
		m_knownedActions.RemoveAll(actionID => GameAssets.current.game.entityActionsData[actionID].GetMainActionType() == EntityActionData.MainActionType.Attack);
		AddStatus(EntityStatusEnumID.Illusion, _ticks);
	}

	private void OnIllusionDeath ( int _illusionID )
	{
		if (_illusionID != m_illusionID)
			return;

		GameManager.Instance.GetEntityFromID(_illusionID).Equipment.onDeath -= OnIllusionDeath;
		m_equipment.onDeath -= OnIllusionSourceDeath;
		m_illusionID = -1;
		ForgetAction(EntityActionEnumID.SwapWithIllusion);
	}

	private void OnIllusionSourceDeath ( int _sourceID )
	{
		Entity illusion = Illusion;
		if (illusion != null && !illusion.Equipment.IsDead)
			illusion.Equipment.InstantDeath(EntityEquipmentPlugin.DeathCause.Vanish);
	}

	private void LearnAction ( EntityActionEnumID _actionID, string _componentID )
	{
		if (!m_knownedActions.Contains(_actionID))
			m_knownedActions.Add(_actionID);
		m_componentLinkedToAction[_actionID] = new List<string>() { _componentID };
	}

	private void ForgetAction ( EntityActionEnumID _actionID )
	{
		m_knownedActions.Remove(_actionID);
		m_componentLinkedToAction.Remove(_actionID);
	}

#if UNITY_EDITOR
	[FoldoutGroup("Special abilities (debug)"), ShowIf("@UnityEngine.Application.isPlaying"), Button]
	private void DebugGrantStatus ( EntityStatusEnumID _status = EntityStatusEnumID.Echo, int _chargesOrTicks = 3 )
	{
		AddStatus(_status, _chargesOrTicks);
	}

	[FoldoutGroup("Special abilities (debug)"), ShowIf("@UnityEngine.Application.isPlaying"), Button]
	private void DebugRetreat ( int _ticksBack = 3 )
	{
		Retreat(_ticksBack);
	}

	[FoldoutGroup("Special abilities (debug)"), ShowIf("@UnityEngine.Application.isPlaying"), Button]
	private void DebugSummonIllusion ( int _ticks = 5 )
	{
		SummonIllusion(_ticks);
	}
#endif

	#endregion

}
