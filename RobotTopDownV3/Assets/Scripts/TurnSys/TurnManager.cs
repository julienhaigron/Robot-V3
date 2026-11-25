using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;
using System.Linq;
using System;
using DG.Tweening;

public class TurnManager : Singleton<TurnManager>
{
	public static Action<RecordedAction> onActionAdded;
	public static Action<RecordedAction> onActionRemoved;
	public static Action<AEntityAction> onActionSelected;
	public static Action<Entity.EntityState> onStateSelected;
	public static Action onStartInputPhase;
	public static Action onEndInputPhase;
	public static Action onEndPlayPhase;
	public static Action onNewRoundStart;
	private int m_roundCount = 0;
	public int RoundCount => m_roundCount;
	public static Action onEndLevel;
	public static Action onActionTargetsChanged;
	public static Action<AEntityAction> onDesignatedTargetsChanged;
	public static Action<RecordedAction> onFacingOrdersChanged;
	
	public static int currentTick = 0;
	public bool IsLastTickOfRound => currentTick >= GameConfig.current.game.actionTokenPerRound - 1;
#if UNITY_EDITOR
	public static bool isMatchEndDisabled;
#endif

	private float m_currentTickStartTime;
	private float m_tickPlayStartTime;
	private Tween m_tickEndDelay;
	private const float c_minActionTimeShare = .25f;
	private const float c_tickEndTolerance = .02f;
	public static float TickDuration => GameConfig.current.game.actionDuration;
	public float CurrentTickElapsedTime => currentPhase == TurnPhase.Playing ? Time.time - m_currentTickStartTime : 0f;

	[SerializeField] private NetworkedTurnSystem m_networkedTurnSystem;

	[SerializeField] private SerializableDictionary<int, Queue<RecordedAction>> m_recordedActionInput = new(); //all actions this turn
	public SerializableDictionary<int, Queue<RecordedAction>> RecordedActions => m_recordedActionInput;
	[SerializeField] private SerializableDictionary<int, Queue<RecordedAction>> m_actionsToPlay = new(); //this tick actions
	public SerializableDictionary<int, Queue<RecordedAction>> ActionsToPlay => m_actionsToPlay;
	private SerializableDictionary<int, Tuple<RecordedAction, bool>> m_actionsBeingDone = new(); //current actions running
	public SerializableDictionary<int, Tuple<RecordedAction, bool>> ActionsBeingDone => m_actionsBeingDone;

	private SerializableDictionary<int, int> m_remainingActionToken = new();
	public SerializableDictionary<int, int> RemainingActionToken => m_remainingActionToken;

	private readonly Dictionary<int, Entity.EntityState> m_stateOfLastPlayedAction = new();
	private readonly HashSet<int> m_entitiesHoldingFacingOrder = new();
	private FacingPicker m_facingPicker;

	private List<RecordedAction> m_recordedConflict;

	private AEntityAction m_currentEntityAction;
	public AEntityAction CurrentActionSelected => m_currentEntityAction;
	private EntityActionEnumID m_currentActionTypeSelected;
	private AEntityAction m_currentEntityModAction;
	private string m_currentEquipmentLinkedToActionTypeSelected;

	public Entity.EntityState CurrentStateTypeSelected => PlayerController.Instance == null || PlayerController.Instance.SelectedEntity == null
		? Entity.EntityState.NoAIChange
		: PlayerController.Instance.SelectedEntity.DefaultState;

	private List<Tile> m_currentActionTargetTiles = new();
	private bool m_isShowingPickTargets;
	public List<Tile> CurrentActionTargetTiles => m_currentActionTargetTiles;
	private AEntityAction m_designationPendingAction;
	private AEntityAction m_designationReachAction;
	private HashSet<Tile> m_designationReach = new();

	public enum TurnPhase { Recording, Calculating, Playing, Off }
	public TurnPhase currentPhase = TurnPhase.Off;
	public bool hasModActionSelected = false;

	//prevision
	private Dictionary<int, TrackedEntityEvents> m_trackedEventsPerEntity = new();
	public Dictionary<int, TrackedEntityEvents> TrackedEventsPerEntity => m_trackedEventsPerEntity;
	[Serializable]
	public class TrackedEntityEvents
	{
		public int firstTimeEntityMoved;
		public int firstTimeEntityAttacked;
		public int traveledTileCountThisTurn;
		public HashSet<EntityActionEnumID> usedActionThisTurn = new();

		public void ResetAllValues ()
		{
			firstTimeEntityMoved = -1;
			firstTimeEntityAttacked = -1;
			traveledTileCountThisTurn = 0;
			usedActionThisTurn.Clear();
		}
	}

	private Dictionary<int, EntireGameTrackedEvents> m_entireGameTrackedEventsPerEntity = new();
	[Serializable]
	public class EntireGameTrackedEvents
	{
		public HashSet<EntityActionEnumID> usedActions = new();
	}

	private List<RecordedEvent> m_inPlayEventBeingDone = new();
	public IReadOnlyList<RecordedEvent> InPlayEvents => m_inPlayEventBeingDone;

	public class RecordedEvent
	{
		public readonly string label;
		public readonly int startTick;
		public readonly float startTime;

		public Action<RecordedEvent> onEventFinished;

		public RecordedEvent ( string _label = null )
		{
			label = string.IsNullOrEmpty(_label) ? "Unnamed event" : _label;
			startTick = currentTick;
			startTime = Time.time;
		}

		public void EndEvent ()
		{
			onEventFinished?.Invoke(this);
		}
	}

	[Serializable]
	public class RecordedAction : INetworkSerializable
	{
		public int timeAtStart;
		public int performingEntityID;
		public string linkedEquipmentID;
		public Entity.EntityState entityState;
		public EntityActionEnumID type;
		public AEntityAction action;
		public EntityActionEnumID freeActionType;
		public AEntityAction freeAction;

		public bool HasFreeAction => freeAction != null && freeActionType != EntityActionEnumID.Wait && freeActionType != EntityActionEnumID.Unknowned;

		public RecordedAction ()
		{

		}

		public RecordedAction ( AEntityAction _action, Entity.EntityState _state, AEntityAction _freeAction = null )
		{
			timeAtStart = _action.timeAtStart;
			performingEntityID = _action.performingEntityID;
			type = _action.enumID;
			action = _action;
			entityState = _state;
			freeAction = _freeAction;
			freeActionType = _freeAction == null ? EntityActionEnumID.Wait : _freeAction.enumID;
		}

		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref timeAtStart);
			serializer.SerializeValue(ref performingEntityID);
			serializer.SerializeValue(ref linkedEquipmentID);
			serializer.SerializeValue(ref entityState);
			serializer.SerializeValue(ref type);

			if (serializer.IsWriter)
			{
				action.NetworkSerialize(serializer);
			}
			else
			{
				action = Instance.GetAction(GameAssets.current.game.entityActionsData[type], performingEntityID, linkedEquipmentID, timeAtStart);

				if (action == null)
				{
					Debug.LogError("ERROR : action is null when " + (serializer.IsWriter ? "writing" : "reading") + " with type " + type);
				}
				action.NetworkSerialize(serializer);
			}

			serializer.SerializeValue(ref freeActionType);

			bool hasFreeAction = freeAction != null;
			serializer.SerializeValue(ref hasFreeAction);

			if (!hasFreeAction)
			{
				if (serializer.IsReader)
					freeAction = null;

				return;
			}

			if (serializer.IsReader)
			{
				freeAction = Instance.GetAction(GameAssets.current.game.entityActionsData[freeActionType]
					, performingEntityID, linkedEquipmentID, timeAtStart);

				if (freeAction == null)
				{
					Debug.LogError("ERROR : freeAction is null when reading with type " + freeActionType);
					return;
				}
			}

			freeAction.NetworkSerialize(serializer);
		}

		public void AddActionMod ( AEntityAction _actionMod )
		{
			_actionMod.OnModActionAdded(action);
			freeActionType = _actionMod.enumID;
			freeAction = _actionMod;
		}
	}

	public struct RecordedEntityActionsContainer : INetworkSerializable
	{
		public int entityId;
		public RecordedAction[] actions;
		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref entityId);
			serializer.SerializeValue(ref actions);
		}
	}

	public override void Awake ()
	{
		base.Awake();
		EntityAnchor.onEntityAdded += OnEntityAdded;
		PlayerController.onEntitySelected += OnEntitySelected;
		EntityActionDisplay.onSelect += OnActionDisplaySelected;
	}

	private void OnDestroy ()
	{
		EntityAnchor.onEntityAdded -= OnEntityAdded;
		PlayerController.onEntitySelected -= OnEntitySelected;
		EntityActionDisplay.onSelect -= OnActionDisplaySelected;
	}

	#region Callbacks

	private void OnEntitySelected ( int? _selectedEntity )
	{
		EndDesignationWindow();
		CloseFacingPicker(false);
		PlayerController.Instance.ClearHoverPreviews();

		if (EntityActionDisplay.SelectedDisplay != null)
			EntityActionDisplay.SelectedDisplay.Deselect();

		hasModActionSelected = false;
		m_currentEntityModAction = null;

		if (_selectedEntity.HasValue)
		{
			Entity selectedEntity = GameManager.Instance.GetEntityFromID(_selectedEntity.Value);
			SetCurrentActionSelected(selectedEntity.AI.GetMovementAction().enumID, null, true);
			SetCurrentStateSelected(GetDefaultStateOf(selectedEntity));
		}
		RefreshActionDisplay(_selectedEntity, false);
	}

	public void OnEntityAdded ( Entity _entity )
	{
		_entity.Equipment.onDeath += OnEntityDeath;
		m_trackedEventsPerEntity.Add(_entity.ID, new TrackedEntityEvents()
		{
			firstTimeEntityMoved = -1,
			firstTimeEntityAttacked = -1,
			traveledTileCountThisTurn = 0
		});

		m_entireGameTrackedEventsPerEntity.Add(_entity.ID, new EntireGameTrackedEvents());
	}

	private void OnActionDisplaySelected ( EntityActionDisplay _selectedDisplay, bool _isModAction )
	{
		EndDesignationWindow();
		CloseFacingPicker(false);
		PlayerController.Instance.ClearHoverPreviews();

		if (_selectedDisplay != null && _isModAction && _selectedDisplay.RecordedAction.freeAction is RotateEntityAction { isFacingOrder: true })
		{
			hasModActionSelected = true;
			if (TryOpenFacingPicker(_selectedDisplay.RecordedAction.action.PerformingEntity, _selectedDisplay.RecordedAction))
				return;
		}

		if (_selectedDisplay != null)
		{
			hasModActionSelected = _isModAction;
			m_currentActionTargetTiles.Clear();
			AEntityAction action = _isModAction ? _selectedDisplay.RecordedAction.freeAction : _selectedDisplay.RecordedAction.action;
			if (action == null)
			{
				hasModActionSelected = false;
				RefreshActionDisplay(_selectedDisplay.RecordedAction.action.performingEntityID, false, _selectedDisplay.RecordedAction.action.TimeAtEnd);
				return;
			}

			if (action.targetTileIDs != null)
			{
				foreach (int tileID in action.targetTileIDs)
				{
					if (tileID < 0)
						continue;

					m_currentActionTargetTiles.Add(GridManager.Instance.Tiles[tileID]);
				}
			}

			if (action.Data.GetMainActionType() != EntityActionData.MainActionType.Movement)
			{
				m_currentEntityAction = action;
				m_currentActionTypeSelected = action.enumID;
				m_currentEquipmentLinkedToActionTypeSelected = action.linkedEquipmentId;
				action.OnSelectActionTileInteractPredicatePrewarm();
				onActionSelected?.Invoke(action);
			}

			RefreshActionDisplay(action.performingEntityID, false, action.TimeAtEnd);

			ApplyTargetOutlines(action);
		}
		else
		{
			hasModActionSelected = false;
			PlayerController.Instance.ClearTargetOutlines();
			if (PlayerController.Instance.SelectedEntity != null)
			{
				SetCurrentActionSelected(PlayerController.Instance.SelectedEntity.AI.GetMovementAction().enumID, null, true);
				SetCurrentStateSelected(GetDefaultStateOf(PlayerController.Instance.SelectedEntity));
			}
		}
	}

	#endregion

	public void Init ()
	{
		currentPhase = TurnPhase.Off;
		currentTick = 0;
		m_roundCount = 0;
		m_trackedEventsPerEntity.Clear();
		m_entireGameTrackedEventsPerEntity.Clear();
		m_stateOfLastPlayedAction.Clear();
		m_entitiesHoldingFacingOrder.Clear();
		CloseFacingPicker(false);
		m_tickEndDelay?.Kill();
		m_tickEndDelay = null;
	}

	public static float GetTickTimeLeft ( float _reserve = 0f )
	{
		float elapsed = Instance != null && Instance.currentPhase == TurnPhase.Playing ? Time.time - Instance.m_tickPlayStartTime : 0f;
		return Mathf.Max(TickDuration * c_minActionTimeShare, TickDuration - elapsed - _reserve);
	}

	public void TrackEntireGameAction ( int _entityID, EntityActionEnumID _actionType )
	{
		if (!m_entireGameTrackedEventsPerEntity.ContainsKey(_entityID))
			m_entireGameTrackedEventsPerEntity.Add(_entityID, new EntireGameTrackedEvents());

		m_entireGameTrackedEventsPerEntity[_entityID].usedActions.Add(_actionType);
	}

	public bool DidUseActionThisGame ( int _entityID, EntityActionEnumID _actionType )
	{
		return m_entireGameTrackedEventsPerEntity.ContainsKey(_entityID)
			&& m_entireGameTrackedEventsPerEntity[_entityID].usedActions.Contains(_actionType);
	}

	#region Input phase

	public Entity.EntityState GetDefaultStateOf ( Entity _entity )
	{
		return _entity == null ? Entity.EntityState.NoAIChange : _entity.DefaultState;
	}

	public Entity.EntityState GetDefaultStateOf ( int _entityID )
	{
		return GetDefaultStateOf(GameManager.Instance.GetEntityFromID(_entityID));
	}

	public void SetCurrentStateSelected ( Entity.EntityState _state )
	{
		Entity selectedEntity = PlayerController.Instance.SelectedEntity;
		if (selectedEntity != null)
			selectedEntity.SetDefaultState(_state);

		onStateSelected?.Invoke(_state);

		RefreshActionDisplay(selectedEntity != null ? selectedEntity.ID : null, false);
	}

	public void SetCurrentActionSelected ( EntityActionEnumID _action, string _linkedEquipmentID, bool _isResetingAction )
	{
		SelectAction(_action, _linkedEquipmentID, _isResetingAction, false);
	}

	public void SetCurrentModActionSelected ( EntityActionEnumID _action, string _linkedEquipmentID, bool _isResetingAction )
	{
		SelectAction(_action, _linkedEquipmentID, _isResetingAction, true);
	}

	private void SelectAction ( EntityActionEnumID _action, string _linkedEquipmentID, bool _isResetingAction, bool _isModAction )
	{
		if (PlayerController.Instance.SelectedEntity == null)
			return;

		int performingEntityID = PlayerController.Instance.SelectedEntity.ID;

		if (_isModAction || GameAssets.current.game.entityActionsData[_action].GetMainActionType() != EntityActionData.MainActionType.Movement)
			EndDesignationWindow();

		bool wasPickingFacing = IsFacingPickerOpen;
		CloseFacingPicker(false);
		hasModActionSelected = _isModAction;
		if (!_isModAction)
			m_currentActionTypeSelected = _action;
		m_currentEquipmentLinkedToActionTypeSelected = _linkedEquipmentID;

		if (_isResetingAction)
		{
			ClearPickTargetOutlines();
			m_currentActionTargetTiles.Clear();
			m_currentEntityAction = GetAction(_action, performingEntityID, _linkedEquipmentID, GetNextActionStartTime(performingEntityID));

			m_currentEntityAction.OnSelectActionTileInteractPredicatePrewarm();
			onActionSelected?.Invoke(m_currentEntityAction);
		}

		if (wasPickingFacing)
			RefreshActionDisplay(performingEntityID, false);
	}

	private int GetNextActionStartTime ( int _entityID )
	{
		RecordedAction lastRecordedAction = GetLastRecordedAction(_entityID);
		return lastRecordedAction == null ? currentTick : lastRecordedAction.action.TimeAtEnd;
	}

	public void AddTargetTileInCurrentAction ( Tile _tile, AEntityAction _action = null )
	{
		m_currentActionTargetTiles.Add(_tile);
		RefreshPickTargetOutlines(_action ?? m_currentEntityAction, _tile);
	}

	private void RefreshPickTargetOutlines ( AEntityAction _action, Tile _lastTile )
	{
		if (_action == null || EntityActionDisplay.SelectedDisplay != null || _action.Data.DoesResolveItsOwnTarget() || PlayerController.Instance == null)
			return;

		int targetCount = _action.Data.GetMaxTargetAmount(_action, _action.PerformingEntity, _lastTile != null ? _lastTile.GetEntity(true) : null) * _action.Data.tokenDuration;
		if (m_currentActionTargetTiles.Count >= targetCount)
		{
			ClearPickTargetOutlines();
			return;
		}

		ApplyTargetOutlines(_action);
		m_isShowingPickTargets = true;
	}

	private void ClearPickTargetOutlines ()
	{
		if (!m_isShowingPickTargets)
			return;

		m_isShowingPickTargets = false;
		if (EntityActionDisplay.SelectedDisplay == null && PlayerController.Instance != null)
			PlayerController.Instance.ClearTargetOutlines();
	}

	public AEntityAction GetSelectedDisplayAction ()
	{
		if (EntityActionDisplay.SelectedDisplay == null)
			return null;

		return hasModActionSelected
			? EntityActionDisplay.SelectedDisplay.RecordedAction.freeAction
			: EntityActionDisplay.SelectedDisplay.RecordedAction.action;
	}

	public bool CanAddTargetOnSelectedDisplay ( Tile _tile )
	{
		AEntityAction action = GetSelectedDisplayAction();

		if (action == null || _tile == null || action.Data.DoesResolveItsOwnTarget() || m_currentActionTargetTiles.Contains(_tile))
			return false;

		int maxTargetAmount = action.Data.GetMaxTargetAmount(action, action.PerformingEntity, _tile.GetEntity(true)) * action.Data.tokenDuration;

		return m_currentActionTargetTiles.Count < maxTargetAmount;
	}

	public bool TryAddTargetTileOnSelectedDisplay ( Tile _tile )
	{
		if (!CanAddTargetOnSelectedDisplay(_tile))
			return false;

		m_currentActionTargetTiles.Add(_tile);
		RefreshSelectedDisplayTargets(GetSelectedDisplayAction());
		return true;
	}

	public bool TryRemoveTargetTileOnSelectedDisplay ( Tile _tile )
	{
		AEntityAction action = GetSelectedDisplayAction();

		if (action == null || _tile == null || !m_currentActionTargetTiles.Remove(_tile))
			return false;

		RefreshSelectedDisplayTargets(action);
		return true;
	}

	private void RefreshSelectedDisplayTargets ( AEntityAction _action )
	{
		WriteCurrentTargetTilesInto(_action);

		PlayerController.Instance.ClearHoverPreviews();
		RefreshActionDisplay(_action.performingEntityID, false, _action.TimeAtEnd);
		ApplyTargetOutlines(_action);

		if (EntityActionDisplay.SelectedDisplay != null)
			EntityActionDisplay.SelectedDisplay.RefreshMissingTargetWarning();

		onActionTargetsChanged?.Invoke();
	}

	public bool HasActionMissingTarget ( int _entityID )
	{
		if (!m_recordedActionInput.ContainsKey(_entityID))
			return false;

		foreach (RecordedAction recordedAction in m_recordedActionInput[_entityID])
		{
			if (recordedAction.action != null && recordedAction.action.IsMissingTarget())
				return true;

			if (recordedAction.HasFreeAction && recordedAction.freeAction.IsMissingTarget())
				return true;
		}

		return false;
	}

	public bool TryGetFirstEntityWithMissingTarget ( out Entity _entity )
	{
		_entity = null;

		foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities)
		{
			if (entity.Equipment.IsDead || !HasActionMissingTarget(entity.ID))
				continue;

			_entity = entity;
			return true;
		}

		return false;
	}

	private void ApplyTargetOutlines ( AEntityAction _action )
	{
		if (CanDesignateTargetsOf(_action))
		{
			ShowDesignatedTargetsOf(_action);
			return;
		}

		List<Tile> zoneTiles = new();

		if (_action.PerformingEntity != null && !_action.Data.DoesResolveItsOwnTarget() && _action.Data.aoeType != EntityActionData.AOEType.Noone)
		{
			foreach (Tile targetTile in m_currentActionTargetTiles)
			{
				if (targetTile != null)
					zoneTiles.AddRange(_action.PerformingEntity.Equipment.GetTilesInAoERange(_action, targetTile, true, _usePlannedPose: true));
			}
		}

		PlayerController.Instance.SetTargetOutlines(m_currentActionTargetTiles, zoneTiles, _action.Data.GetMainActionType());
	}

	private void WriteCurrentTargetTilesInto ( AEntityAction _action )
	{
		List<int> entitiesIds = new();
		List<int> tilesIds = new();
		foreach (Tile tile in m_currentActionTargetTiles)
		{
			if (tile.TryGetEntity(true, out Entity entity))
				entitiesIds.Add(entity.ID);
			tilesIds.Add(tile.coordinates.ID);
		}
		_action.targetedEntityIDs = entitiesIds.ToArray();
		_action.targetTileIDs = tilesIds.ToArray();
	}

	public bool TryRegisterActionWithoutTarget ()
	{
		if (m_currentEntityAction == null || m_currentEntityAction.Data.DoesNeedATargetTile())
			return false;

		AEntityAction registeredAction = m_currentEntityAction;
		int performingEntityID = registeredAction.performingEntityID;
		RegisterAction(performingEntityID, registeredAction, GetDefaultStateOf(performingEntityID));

		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		EntityActionData movementAction = performingEntity == null ? null : performingEntity.AI.GetMovementAction();
		if (movementAction != null)
			SetCurrentActionSelected(movementAction.enumID, null, true);

		if (EntityActionDisplay.SelectedDisplay == null && CanDesignateTargetsOf(registeredAction) && IsQueued(registeredAction))
			m_designationPendingAction = registeredAction;

		RefreshActionDisplay(performingEntityID, true);
		return true;
	}

	public static bool CanDesignateTargetsOf ( AEntityAction _action )
	{
		return _action != null && _action.Data.DoesResolveItsOwnTarget();
	}

	public AEntityAction GetDesignationFocusAction ()
	{
		Entity selectedEntity = PlayerController.Instance == null ? null : PlayerController.Instance.SelectedEntity;
		if (currentPhase != TurnPhase.Recording || selectedEntity == null)
			return null;

		if (EntityActionDisplay.SelectedDisplay != null)
		{
			AEntityAction selectedAction = EntityActionDisplay.SelectedDisplay.RecordedAction.action;
			return !hasModActionSelected && CanDesignateTargetsOf(selectedAction) && selectedAction.performingEntityID == selectedEntity.ID
				? selectedAction : null;
		}

		return m_designationPendingAction != null && m_designationPendingAction.performingEntityID == selectedEntity.ID
			&& IsQueued(m_designationPendingAction)
			? m_designationPendingAction : null;
	}

	public HashSet<Tile> GetDesignationReach ( AEntityAction _action )
	{
		Entity performer = _action == null ? null : _action.PerformingEntity;
		if (performer == null)
			return new HashSet<Tile>();

		if (m_designationReachAction != _action)
		{
			m_designationReachAction = _action;
			m_designationReach = performer.Equipment.GetTilesInReach(_action, GridManager.Instance.Tiles[_action.supposedPositionAtActionStartID], false, true);
		}

		return m_designationReach;
	}

	public List<Entity> GetDesignableEnemies ( AEntityAction _action )
	{
		List<Entity> enemies = new();
		Entity performer = _action == null ? null : _action.PerformingEntity;
		if (performer == null)
			return enemies;

		foreach (Tile tile in GetDesignationReach(_action))
		{
			Entity entity = tile.GetCurrentEntity();
			if (IsDesignableBy(performer, entity) && !enemies.Contains(entity))
				enemies.Add(entity);
		}

		return enemies;
	}

	public bool TryGetDesignableEnemyOn ( Tile _tile, AEntityAction _action, out Entity _enemy )
	{
		_enemy = _tile == null || _action == null ? null : _tile.GetCurrentEntity();
		return _enemy != null && IsDesignableBy(_action.PerformingEntity, _enemy) && GetDesignationReach(_action).Contains(_tile);
	}

	public bool TryGetDesignableItemOn ( Tile _tile, AEntityAction _action, out Item _item )
	{
		_item = _tile == null || _action == null ? null : _tile.GetItem(true);
		return _item != null && IsItemDesignableBy(_action, _item) && GetDesignationReach(_action).Contains(_tile);
	}

	public List<Item> GetDesignableItems ( AEntityAction _action )
	{
		List<Item> items = new();
		if (_action == null)
			return items;

		foreach (Tile tile in GetDesignationReach(_action))
		{
			Item item = tile.GetItem(true);
			if (item != null && IsItemDesignableBy(_action, item) && !items.Contains(item))
				items.Add(item);
		}

		return items;
	}

	public bool TryToggleDesignatedTarget ( Tile _tile )
	{
		AEntityAction action = GetDesignationFocusAction();
		if (TryGetDesignableEnemyOn(_tile, action, out Entity enemy))
			action.ToggleDesignatedTarget(enemy.ID, action.Data.GetMaxTargetAmount(action, action.PerformingEntity, enemy));
		else if (TryGetDesignableItemOn(_tile, action, out Item item))
			action.ToggleDesignatedItem(item);
		else
			return false;

		OnDesignatedTargetsChanged(action);
		return true;
	}

	public bool TryRemoveDesignatedTarget ( Tile _tile )
	{
		AEntityAction action = GetDesignationFocusAction();
		if (_tile == null || action == null)
			return false;

		Entity entity = _tile.GetCurrentEntity();
		bool isRemoved = entity != null ? action.RemoveDesignatedTarget(entity.ID) : action.RemoveDesignatedItem(_tile.GetItem(true));
		if (!isRemoved)
			return false;

		OnDesignatedTargetsChanged(action);
		return true;
	}

	private static bool IsItemDesignableBy ( AEntityAction _action, Item _item )
	{
		Entity performer = _action.PerformingEntity;
		return performer != null && _action.Data.GetMainActionType() == EntityActionData.MainActionType.Attack
			&& _item.IsTargetableBy(performer.OwnerID, false) && _item.IsShownToLocalPlayer;
	}

	private static bool IsDesignableBy ( Entity _performer, Entity _target )
	{
		return _performer != null && _target != null && _target != _performer && !_target.Equipment.IsDead
			&& !_target.IsAlliedTo(_performer.OwnerID) && _target.IsVisible
			&& (int)_target.HowIsUnitVisible < (int)NeuronalMembraneEquipmentData.VisionTypes.Radar;
	}

	private bool IsQueued ( AEntityAction _action )
	{
		return m_recordedActionInput.TryGetValue(_action.performingEntityID, out Queue<RecordedAction> queue)
			&& queue != null && queue.Any(recordedAction => recordedAction.action == _action);
	}

	private void OnDesignatedTargetsChanged ( AEntityAction _action )
	{
		PlayerController.Instance.ClearHoverPreviews();
		ShowDesignatedTargetsOf(_action);
		onDesignatedTargetsChanged?.Invoke(_action);
		RefreshActionDisplay(_action.performingEntityID, false, EntityActionDisplay.SelectedDisplay != null ? _action.TimeAtEnd : -1);
		PlayerController.Instance.RefreshDesignationHover();
	}

	private void ShowDesignatedTargetsOf ( AEntityAction _action )
	{
		PlayerController.Instance.SetTargetOutlines(_action.GetDesignatedTargetTiles(), Array.Empty<Tile>(), _action.Data.GetMainActionType());
	}

	private void EndDesignationWindow ()
	{
		if (m_designationPendingAction == null)
			return;

		m_designationPendingAction = null;
		if (EntityActionDisplay.SelectedDisplay == null && PlayerController.Instance != null)
			PlayerController.Instance.ClearTargetOutlines();
	}

	public sealed class FacingPicker
	{
		public int entityID;
		public Tile pivot;
		public RecordedAction host;
		public RotateEntityAction probe;
		public int hoveredOrientation = -1;
		public bool isDrawn;
		public readonly Dictionary<int, HashSet<Tile>> coneTilesPerOrientation = new();
	}

	public bool IsFacingPickerOpen => m_facingPicker != null;

	public static bool CanHostFacingOrder ( RecordedAction _recordedAction )
	{
		if (_recordedAction == null || _recordedAction.action == null)
			return false;

		bool hasFreeSlot = !_recordedAction.HasFreeAction || _recordedAction.freeAction is RotateEntityAction { isFacingOrder: true };
		EntityActionData.ActionCodeType codeType = _recordedAction.action.Data.codeType;
		return hasFreeSlot && (codeType == EntityActionData.ActionCodeType.Wait || codeType == EntityActionData.ActionCodeType.TargetTileMove);
	}

	public bool TryOpenFacingPickerAt ( Entity _entity, Tile _tile )
	{
		if (_tile == null || _entity == null || EntityActionDisplay.SelectedDisplay != null
			|| _tile.coordinates.ID != GetLastRegisteredPositionOfEntity(_entity.ID))
			return false;

		RecordedAction lastRecordedAction = GetLastRecordedAction(_entity.ID);
		bool hasHost = lastRecordedAction != null && CanHostFacingOrder(lastRecordedAction);
		bool canAddWait = m_remainingActionToken.TryGetValue(_entity.ID, out int remainingTokens) && remainingTokens > 0;

		return (hasHost || canAddWait) && TryOpenFacingPicker(_entity, null);
	}

	private bool TryOpenFacingPicker ( Entity _entity, RecordedAction _host )
	{
		if (currentPhase != TurnPhase.Recording || _entity == null || _entity.Equipment.IsDead
			|| !_entity.IsAlliedTo(GameManager.Instance.PlayerID))
			return false;

		Tile pivot = GridManager.Instance.Tiles[_host != null ? _host.action.positionAtActionEndID : GetLastRegisteredPositionOfEntity(_entity.ID)];
		RotateEntityAction probe = GetAction(EntityActionEnumID.RotateEntity, _entity.ID, null, currentTick) as RotateEntityAction;
		if (probe == null)
			return false;

		CloseFacingPicker(false);
		EndDesignationWindow();
		PlayerController.Instance.ClearHoverPreviews();
		m_currentActionTargetTiles.Clear();

		probe.supposedPositionAtActionStartID = pivot.coordinates.ID;
		probe.positionAtActionEndID = pivot.coordinates.ID;
		m_facingPicker = new FacingPicker { entityID = _entity.ID, pivot = pivot, host = _host, probe = probe };

		onActionSelected?.Invoke(probe);
		RefreshFacingPicker(null);
		return true;
	}

	public bool IsFacingPickerOption ( Tile _tile )
	{
		return m_facingPicker != null && m_facingPicker.probe.TileInteractPredicate(_tile);
	}

	public void RefreshFacingPicker ( Tile _hoveredTile )
	{
		if (m_facingPicker == null)
			return;

		int hoveredOrientation = IsFacingPickerOption(_hoveredTile)
			? GridManager.Instance.GetClosestOrientation(m_facingPicker.pivot, _hoveredTile) : -1;
		if (m_facingPicker.isDrawn && hoveredOrientation == m_facingPicker.hoveredOrientation)
			return;

		m_facingPicker.hoveredOrientation = hoveredOrientation;
		m_facingPicker.isDrawn = true;
		RefreshActionDisplay(m_facingPicker.entityID, false, GetFacingPickerTokenCount());
	}

	private int GetFacingPickerTokenCount ()
	{
		if (m_facingPicker.host != null)
			return m_facingPicker.host.action.TimeAtEnd;

		RecordedAction lastRecordedAction = GetLastRecordedAction(m_facingPicker.entityID);
		return lastRecordedAction == null ? 0 : lastRecordedAction.action.TimeAtEnd;
	}

	private void DrawFacingPicker ()
	{
		if (m_facingPicker == null || !GameManager.Instance.GetEntityFromID(out Entity entity, m_facingPicker.entityID))
			return;

		RecordedAction host = GetFacingPickerHost();
		int plannedOrientation = GetPlannedOrientationOf(m_facingPicker.entityID, GetFacingPickerTokenCount());
		int fromOrientation = host != null ? GetOrientationBeforeFreeRotation(host) : plannedOrientation;
		int shownOrientation = m_facingPicker.hoveredOrientation >= 0 ? m_facingPicker.hoveredOrientation : plannedOrientation;

		PlayerController.Instance.ShowFacingPicker(entity, m_facingPicker.pivot, fromOrientation, shownOrientation, GetFacingConeTiles(entity, shownOrientation));
	}

	private RecordedAction GetFacingPickerHost ()
	{
		RecordedAction host = m_facingPicker.host ?? GetLastRecordedAction(m_facingPicker.entityID);
		return host != null && CanHostFacingOrder(host) && host.action.positionAtActionEndID == m_facingPicker.pivot.coordinates.ID ? host : null;
	}

	public bool IsEditingFacingOf ( RecordedAction _host )
	{
		return m_facingPicker != null && _host != null && _host == GetFacingPickerHost();
	}

	private HashSet<Tile> GetFacingConeTiles ( Entity _entity, int _orientation )
	{
		if (!m_facingPicker.coneTilesPerOrientation.TryGetValue(_orientation, out HashSet<Tile> coneTiles))
		{
			coneTiles = _entity.Equipment.GetWeaponConeTilesAt(m_facingPicker.pivot, _orientation);
			m_facingPicker.coneTilesPerOrientation.Add(_orientation, coneTiles);
		}

		return coneTiles;
	}

	public bool TryValidateFacing ( Tile _tile )
	{
		if (!IsFacingPickerOption(_tile))
			return false;

		FacingPicker picker = m_facingPicker;
		int orientation = GridManager.Instance.GetClosestOrientation(picker.pivot, _tile);
		RecordedAction host = GetFacingPickerHost();
		bool canHost = host != null;

		if (!canHost && (!m_remainingActionToken.TryGetValue(picker.entityID, out int remainingTokens) || remainingTokens <= 0))
			return false;

		int timeAtStart = canHost ? host.action.timeAtStart : GetFacingPickerTokenCount();
		RotateEntityAction facingOrder = GetAction(EntityActionEnumID.RotateEntity, picker.entityID, null, timeAtStart) as RotateEntityAction;
		facingOrder.SetFacingOrder(picker.pivot, orientation, _tile);

		CloseFacingPicker();

		if (canHost)
		{
			host.AddActionMod(facingOrder);
			onActionTargetsChanged?.Invoke();
			onFacingOrdersChanged?.Invoke(host);
		}
		else
			RegisterActionAndMod(picker.entityID, GetAction(EntityActionEnumID.Wait, picker.entityID, null, timeAtStart), facingOrder, GetDefaultStateOf(picker.entityID));

		RefreshActionDisplay(picker.entityID, true);
		return true;
	}

	public void RemoveFacingOrderOf ( RecordedAction _host )
	{
		if (_host == null || _host.freeAction is not RotateEntityAction { isFacingOrder: true })
			return;

		if (m_facingPicker != null && m_facingPicker.host == _host)
			CloseFacingPicker();

		_host.freeAction = null;
		_host.freeActionType = EntityActionEnumID.Wait;

		onActionTargetsChanged?.Invoke();
		onFacingOrdersChanged?.Invoke(_host);
		RefreshActionDisplay(_host.action.performingEntityID, false);
	}

	public void CloseFacingPicker ( bool _restoreSelection = true )
	{
		if (m_facingPicker == null)
			return;

		RecordedAction host = m_facingPicker.host;
		m_facingPicker = null;

		if (PlayerController.Instance == null)
			return;

		PlayerController.Instance.ClearRangePreviewOutlines();

		if (!_restoreSelection)
			return;

		if (host != null && EntityActionDisplay.SelectedDisplay != null && EntityActionDisplay.SelectedDisplay.RecordedAction == host)
		{
			EntityActionDisplay.SelectedDisplay.Deselect();
			return;
		}

		if (m_currentEntityAction != null && PlayerController.Instance.SelectedEntity != null
			&& m_currentEntityAction.performingEntityID == PlayerController.Instance.SelectedEntity.ID)
		{
			m_currentEntityAction.OnSelectActionTileInteractPredicatePrewarm();
			onActionSelected?.Invoke(m_currentEntityAction);
		}

		RefreshActionDisplay(PlayerController.Instance.SelectedEntity != null ? PlayerController.Instance.SelectedEntity.ID : null, false);
	}

	private void CloseFacingPickerOf ( int _entityID )
	{
		if (m_facingPicker != null && m_facingPicker.entityID == _entityID)
			CloseFacingPicker(false);
	}

	private RecordedAction GetLastRecordedAction ( int _entityID )
	{
		RecordedAction lastRecordedAction = null;
		if (m_recordedActionInput.TryGetValue(_entityID, out Queue<RecordedAction> queue) && queue != null)
		{
			foreach (RecordedAction recordedAction in queue)
				lastRecordedAction = recordedAction;
		}

		return lastRecordedAction;
	}

	public int GetPlannedOrientationOf ( int _entityID, int _tokenCount = int.MaxValue )
	{
		Entity entity = GameManager.Instance.GetEntityFromID(_entityID);
		int orientation = entity == null ? 0 : entity.Displacement.CurrentOrientation;
		if (!m_recordedActionInput.TryGetValue(_entityID, out Queue<RecordedAction> queue) || queue == null)
			return orientation;

		int totalCost = 0;
		foreach (RecordedAction recordedAction in queue)
		{
			totalCost += recordedAction.action.TotalDuration;
			if (totalCost > _tokenCount)
				break;

			orientation = GetPlannedOrientationAfter(recordedAction, orientation);
		}

		return orientation;
	}

	public int GetPlannedShieldOrientation ( Shield _shield, int _tokenCount = int.MaxValue )
	{
		int orientation = _shield.Orientation;
		if (!m_recordedActionInput.TryGetValue(_shield.LinkedEntity.ID, out Queue<RecordedAction> queue) || queue == null)
			return orientation;

		int totalCost = 0;
		foreach (RecordedAction recordedAction in queue)
		{
			totalCost += recordedAction.action.TotalDuration;
			if (totalCost > _tokenCount)
				break;

			if (recordedAction.action is TurnShieldAction turn && turn.linkedEquipmentId == _shield.ID)
				orientation = turn.targetedOrientation;
		}

		return orientation;
	}

	public int GetOrientationBeforeFreeRotation ( RecordedAction _host )
	{
		return GetFacingAfterMovementOf(_host, GetPlannedOrientationOf(_host.action.performingEntityID, _host.action.timeAtStart));
	}

	private static int GetFacingAfterMovementOf ( RecordedAction _recordedAction, int _orientationBefore )
	{
		return _recordedAction.freeAction is RotateEntityAction { IsPlayedFirst: true }
			? _orientationBefore : GetMovementFacingOf(_recordedAction.action, _orientationBefore);
	}

	public static int GetPlannedOrientationAfter ( RecordedAction _recordedAction, int _orientationBefore )
	{
		RotateEntityAction rotation = _recordedAction.HasFreeAction ? _recordedAction.freeAction as RotateEntityAction : null;
		bool hasRotation = rotation != null && rotation.FacingOrientation >= 0;

		if (hasRotation && rotation.IsPlayedFirst)
			return rotation.FacingOrientation;

		int orientation = GetMovementFacingOf(_recordedAction.action, _orientationBefore);
		return hasRotation ? rotation.FacingOrientation : orientation;
	}

	private static int GetMovementFacingOf ( AEntityAction _action, int _orientationBefore )
	{
		Tile[] tiles = GridManager.Instance.Tiles;
		if (_action is MoveToTargetAction moveAction && moveAction.targetTileIDs != null && moveAction.targetTileIDs.Length > 0)
		{
			int walkedTileCount = Mathf.Min(moveAction.targetTileIDs.Length, Mathf.Max(1, moveAction.Data.movementSpeed));
			int lastTileID = moveAction.targetTileIDs[walkedTileCount - 1];
			int previousTileID = walkedTileCount > 1 ? moveAction.targetTileIDs[walkedTileCount - 2] : moveAction.supposedPositionAtActionStartID;

			return IsValidTileID(lastTileID) && IsValidTileID(previousTileID) && lastTileID != previousTileID
				? GridManager.Instance.GetClosestOrientation(tiles[previousTileID], tiles[lastTileID])
				: _orientationBefore;
		}

		if (_action.Data.codeType == EntityActionData.ActionCodeType.JumpMove
			&& IsValidTileID(_action.supposedPositionAtActionStartID) && IsValidTileID(_action.positionAtActionEndID)
			&& _action.supposedPositionAtActionStartID != _action.positionAtActionEndID)
			return GridManager.Instance.GetClosestOrientation(tiles[_action.supposedPositionAtActionStartID], tiles[_action.positionAtActionEndID]);

		return _orientationBefore;
	}

	private static bool IsValidTileID ( int _tileID )
	{
		return _tileID >= 0 && _tileID < GridManager.Instance.Tiles.Length;
	}

	public AEntityAction GetAction ( EntityActionEnumID _actionType, int _performingEntityID, string _linkedEquipmentID, int _timeAtStart )
	{
		return GetAction(GameAssets.current.game.entityActionsData[_actionType], _performingEntityID, _linkedEquipmentID, _timeAtStart);
	}

	public AEntityAction GetAction ( EntityActionData _actionData, int _performingEntityID, string _linkedEquipmentID, int _timeAtStart )
	{
		AEntityAction action = null;
		if (_actionData == null)
			return null;

		//for base actions or exceptions
		switch (_actionData.codeType)
		{
			case EntityActionData.ActionCodeType.TargetTileMove:
				action = new MoveToTargetAction();
				break;
			case EntityActionData.ActionCodeType.Attack:
				action = new AttackAction();
				break;
			case EntityActionData.ActionCodeType.MoveThenAttack:
				action = new MoveThenAttackAction();
				break;
			case EntityActionData.ActionCodeType.TurnEntity:
				action = new RotateEntityAction();
				break;
			case EntityActionData.ActionCodeType.TurnShield:
				action = new TurnShieldAction();
				break;
			case EntityActionData.ActionCodeType.Special:
			case EntityActionData.ActionCodeType.ApplyEffect:
				action = new SpecialAction();
				break;
			case EntityActionData.ActionCodeType.InvokeEntity:
				action = new InvokeEntityAction();
				break;
			case EntityActionData.ActionCodeType.AddEffectToAction:
				action = new AddEffectToAction();
				break;
			case EntityActionData.ActionCodeType.Wait:
				action = new WaitAction();
				break;
			case EntityActionData.ActionCodeType.InvokeItem:
				action = new InvokeItemAction();
				break;
			case EntityActionData.ActionCodeType.JumpMove:
				action = new JumpToTarget();
				break;
			case EntityActionData.ActionCodeType.MoveItem:
				action = new MoveItemAction();
				break;
			default:
				Debug.LogError("Missing entree in TurnManager.GetAction for type \"" + _actionData.codeType + "\"");
				return action;
		}
		action.Init(GameAssets.current.game.entityActionsData[_actionData.enumID], _linkedEquipmentID, _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID), _timeAtStart);

		return action;
	}

	public void RegisterAction ( int _entityID, AEntityAction _action, Entity.EntityState _state )
	{
		if (EntityActionDisplay.SelectedDisplay != null)
		{
			if (m_currentActionTargetTiles.Count > 0)
				WriteCurrentTargetTilesInto(_action);
			m_currentActionTargetTiles.Clear();
			if (hasModActionSelected)
			{
				EntityActionDisplay.SelectedDisplay.RecordedAction.freeAction = _action;
				EntityActionDisplay.SelectedDisplay.RecordedAction.freeActionType = _action.enumID;
			}
			else
			{
				EntityActionDisplay.SelectedDisplay.RecordedAction.action = _action;
				EntityActionDisplay.SelectedDisplay.RecordedAction.type = _action.enumID;
			}
			EntityActionDisplay.SelectedDisplay.RecordedAction.entityState = _state;
		}
		else if (hasModActionSelected)
		{
			m_currentEntityModAction = _action;
			hasModActionSelected = false;
		}
		else
			AddAction(_entityID, _action, _state);
	}

	public bool AddAction ( int _entityID, EntityActionEnumID _actionType, Entity.EntityState _state, string _linkedEquipmentID )
	{
		return AddAction(_entityID, GetAction(_actionType, _entityID, _linkedEquipmentID, GetNextActionStartTime(_entityID)), _state);
	}

	public bool AddAction ( int _entityID, AEntityAction _action, Entity.EntityState _state )
	{
		return RegisterActionAndMod(_entityID, _action, m_currentEntityModAction, _state);
	}

	public bool RegisterActionAndMod ( int _entityID, AEntityAction _action, AEntityAction _modAction, Entity.EntityState _state )
	{
		EndDesignationWindow();
		CloseFacingPickerOf(_entityID);
		if (!m_recordedActionInput.ContainsKey(_entityID))
			m_recordedActionInput.Add(_entityID, new());

		if (m_remainingActionToken[_entityID] <= 0)
			return false;

		if (m_currentActionTargetTiles.Count > 0)
			WriteCurrentTargetTilesInto(_action);
		m_currentActionTargetTiles.Clear();

		RecordedAction recordedAction = new(_action, _state, _modAction);
		m_currentEntityModAction = null;

		m_recordedActionInput[_entityID].Enqueue(recordedAction);
		m_remainingActionToken[_entityID] -= _action.TotalDuration;

		TrackedEventCheck();

		LogConsole.AddLog("Add " + _action.ToString() + " action to queue.", LogConsole.LogEventType.InputPhase);
		onActionAdded?.Invoke(recordedAction);
		return true;
	}

	public void RemoveActionFrom ( RecordedAction _actionToStartRemoveFrom, int _recordedActionPositionInQueue )
	{
		if (!m_recordedActionInput.ContainsKey(_actionToStartRemoveFrom.performingEntityID)
			|| m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID].Count <= _recordedActionPositionInQueue)
			return;

		CloseFacingPickerOf(_actionToStartRemoveFrom.performingEntityID);

		List<RecordedAction> actionQueue = m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID].ToList();
		for (int i = actionQueue.Count - 1; i >= _recordedActionPositionInQueue; i--)
		{
			actionQueue[i].action.CancelAction();
			m_remainingActionToken[_actionToStartRemoveFrom.performingEntityID] += actionQueue[i].action.TotalDuration;
			actionQueue.RemoveAt(i);
		}

		if (actionQueue.Count > 0)
			m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID] = new Queue<RecordedAction>(actionQueue);
		else
			m_recordedActionInput.Remove(_actionToStartRemoveFrom.performingEntityID);

		if (m_currentEntityAction != null)
			m_currentEntityAction.timeAtStart = GetNextActionStartTime(_actionToStartRemoveFrom.performingEntityID);

		if (m_designationPendingAction != null && !IsQueued(m_designationPendingAction))
			EndDesignationWindow();

		onActionRemoved?.Invoke(_actionToStartRemoveFrom);

		TrackedEventCheck();
		RefreshActionDisplay(_actionToStartRemoveFrom.performingEntityID, true);
	}

	public AEntityAction GetActionPerformedAtTick ( int _entityID )
	{
		return GetRecordedActionPerformedAtTick(_entityID)?.action;
	}

	public AEntityAction GetActionInProgressOf ( int _entityID )
	{
		RecordedAction queued = m_actionsToPlay.TryGetValue(_entityID, out Queue<RecordedAction> queue) && queue != null && queue.Count > 0
			&& queue.Peek().action.timeAtStart <= currentTick ? queue.Peek() : null;
		RecordedAction beingDone = m_actionsBeingDone.TryGetValue(_entityID, out Tuple<RecordedAction, bool> tuple) ? tuple?.Item1 : null;

		return (currentPhase == TurnPhase.Calculating ? queued ?? beingDone : beingDone ?? queued)?.action;
	}

	public int GetEntityPositionAtEndOfTick ( int _entityID, int _defaultTileID )
	{
		RecordedAction recordedAction = GetRecordedActionPerformedAtTick(_entityID);
		return recordedAction == null ? _defaultTileID : recordedAction.action.positionAtActionEndID;
	}

	private RecordedAction GetRecordedActionPerformedAtTick ( int _entityID )
	{
		if (currentPhase == TurnPhase.Calculating)
			return FindQueuedActionPerformedAtTick(_entityID) ?? FindActionBeingDonePerformedAtTick(_entityID);

		return FindActionBeingDonePerformedAtTick(_entityID) ?? FindQueuedActionPerformedAtTick(_entityID);
	}

	private RecordedAction FindQueuedActionPerformedAtTick ( int _entityID )
	{
		if (!m_actionsToPlay.ContainsKey(_entityID) || m_actionsToPlay[_entityID] == null)
			return null;

		foreach (RecordedAction recordedAction in m_actionsToPlay[_entityID])
		{
			if (recordedAction.action.IsPerformingAtTick(currentTick))
				return recordedAction;
		}

		return null;
	}

	private RecordedAction FindActionBeingDonePerformedAtTick ( int _entityID )
	{
		if (!m_actionsBeingDone.ContainsKey(_entityID) || m_actionsBeingDone[_entityID] == null)
			return null;

		RecordedAction actionBeingDone = m_actionsBeingDone[_entityID].Item1;
		return actionBeingDone.action.IsPerformingAtTick(currentTick) ? actionBeingDone : null;
	}

	public bool IsEntityLeavingTileThisTick ( int _entityID, int _tileID )
	{
		if (m_actionsToPlay.ContainsKey(_entityID) && m_actionsToPlay[_entityID] != null)
		{
			foreach (RecordedAction recordedAction in m_actionsToPlay[_entityID])
			{
				if (recordedAction.action.IsPerformingAtTick(currentTick) && recordedAction.action.DoesLeaveTileThisTick(_tileID))
					return true;
			}
		}

		if (m_actionsBeingDone.ContainsKey(_entityID) && m_actionsBeingDone[_entityID] != null)
		{
			AEntityAction actionBeingDone = m_actionsBeingDone[_entityID].Item1.action;
			if (actionBeingDone.IsPerformingAtTick(currentTick) && actionBeingDone.DoesLeaveTileThisTick(_tileID))
				return true;
		}

		return false;
	}

	public int GetLastRegisteredPositionOfEntity ( int _entityID )
	{
		RecordedAction lastRecordedAction = GetLastRecordedAction(_entityID);
		return lastRecordedAction == null
			? GameManager.Instance.GetEntityFromID(_entityID).Displacement.Coordinates.ID
			: lastRecordedAction.action.positionAtActionEndID;
	}

	public int GetLastRegisteredOrientationOfEntity(int _entityID )
	{
		return GetPlannedOrientationOf(_entityID);
	}

	private void TrackedEventCheck ()
	{
		foreach (KeyValuePair<int, TrackedEntityEvents> pair in m_trackedEventsPerEntity)
		{
			pair.Value.ResetAllValues();

			if (!m_recordedActionInput.ContainsKey(pair.Key))
				continue;

			foreach (RecordedAction recordedAction in m_recordedActionInput[pair.Key])
			{
				EntityActionData.ActionType actionType = recordedAction.action.Data.type;
				if (actionType == EntityActionData.ActionType.Movement)
				{
					pair.Value.firstTimeEntityMoved = recordedAction.action.timeAtStart;
					pair.Value.traveledTileCountThisTurn++;
				}
				if (actionType == EntityActionData.ActionType.DistanceAttack || actionType == EntityActionData.ActionType.MeleeAttack)
					pair.Value.firstTimeEntityAttacked = recordedAction.action.timeAtStart;
				pair.Value.usedActionThisTurn.Add(recordedAction.type);
			}
		}
	}

	public void RefreshActionDisplay ( int? _selectedEntityID, bool _isResetingAction, int _specificTokenCount = -1 )
	{
		PlayerController.Instance.ClearActionOnTileDisplay();
		PlayerController.Instance.ClearGhostActionOnTileDisplay();
		PlayerController.Instance.ClearGhostEntitiesAndItems();
		PlayerController.Instance.ClearAoEPreviewOutlines();

		if (_selectedEntityID.HasValue && _isResetingAction
			&& m_remainingActionToken[_selectedEntityID.Value] >= GameAssets.current.game.entityActionsData[m_currentActionTypeSelected].GetTokenTotalCost(m_currentEntityAction, GameManager.Instance.GetEntityFromID(_selectedEntityID.Value), null))
		{
			Entity selectedEntity = GameManager.Instance.GetEntityFromID(_selectedEntityID.Value);
			EntityActionData movementAction = selectedEntity != null ? selectedEntity.AI.GetMovementAction() : null;
			bool canReselect = GameAssets.current.game.entityActionsData[m_currentActionTypeSelected].CanInvokeMoreItems(selectedEntity, m_currentEquipmentLinkedToActionTypeSelected);

			if (!canReselect && movementAction != null)
				SetCurrentActionSelected(movementAction.enumID, null, _isResetingAction);
			else
				SetCurrentActionSelected(m_currentActionTypeSelected, m_currentEquipmentLinkedToActionTypeSelected, _isResetingAction);
		}

		AEntityAction currentSelectedAction = EntityActionDisplay.SelectedDisplay != null ? GetSelectedDisplayAction() : m_currentEntityAction;

		foreach (int entityID in m_recordedActionInput.Keys)
		{
			int totalCost = 0;
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			Tile lastRecordedPosition = entity.Displacement.Coordinates.GetTile();
			int plannedOrientation = entity.Displacement.CurrentOrientation;
			int lastRecordedOrientation = plannedOrientation;
			int ghostTokenCount = _specificTokenCount == -1 && entityID == _selectedEntityID && EntityActionDisplay.SelectedDisplay == null
				? int.MaxValue : _specificTokenCount;

			foreach (RecordedAction recordedAction in m_recordedActionInput[entityID])
			{
				totalCost += recordedAction.action.TotalDuration;
				recordedAction.action.Display(recordedAction);

				int movementFacing = GetFacingAfterMovementOf(recordedAction, plannedOrientation);
				if (movementFacing != plannedOrientation)
					RotationActionDisplay.ShowMovementTurn(entityID, GridManager.Instance.Tiles[recordedAction.action.supposedPositionAtActionStartID], plannedOrientation, movementFacing);

				if (recordedAction.HasFreeAction)
					recordedAction.freeAction.Display(recordedAction);

				plannedOrientation = GetPlannedOrientationAfter(recordedAction, plannedOrientation);
				if (ghostTokenCount != -1 && totalCost <= ghostTokenCount)
				{
					lastRecordedPosition = GridManager.Instance.Tiles[recordedAction.action.positionAtActionEndID];
					lastRecordedOrientation = plannedOrientation;
				}
			}

			if (_selectedEntityID.HasValue)
			{
				Tile ghostPosition = EntityActionDisplay.SelectedDisplay != null && entityID == _selectedEntityID.Value
					? GridManager.Instance.Tiles[EntityActionDisplay.SelectedDisplay.RecordedAction.action.positionAtActionEndID]
					: lastRecordedPosition;

				PlayerController.Instance.AddGhostEntityAt(entity, ghostPosition, lastRecordedOrientation);
			}
		}

		if (_selectedEntityID.HasValue && _specificTokenCount != -1 && currentSelectedAction != null)
		{
			currentSelectedAction.DisplayAoEPreviewOnHoveredTile();
			currentSelectedAction.GhostDisplay(CurrentStateTypeSelected);
		}

		PlayerController.Instance.RedrawTargetOutlines();
		PlayerController.Instance.RefreshTargetMarkers();
		PlayerController.Instance.RefreshAimedTargets(_selectedEntityID.HasValue && _specificTokenCount != -1 ? currentSelectedAction : null);
		DrawFacingPicker();
	}

	[Button]
	public void StartInputPhase ()
	{
		currentTick = 0;
		currentPhase = TurnPhase.Recording;
		FaceClosestEnemiesAfterRound();
		if (!GameManager.Instance.IsOnline)
			GameManager.Instance.RespawnDeadMatchUnits();
		LogConsole.AddLog("Start Input phase", LogConsole.LogEventType.DebugSys);

		m_remainingActionToken.Clear();
		m_recordedActionInput.Clear();
		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				m_remainingActionToken.Add(entity.ID, GameConfig.current.game.actionTokenPerRound);
			}
		}

		foreach (TrackedEntityEvents trackedEvents in m_trackedEventsPerEntity.Values)
			trackedEvents.ResetAllValues();

		m_roundCount++;
		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_round), m_roundCount), LogConsole.LogEventType.Round);

		onStartInputPhase?.Invoke();

		if (GameManager.Instance.IsOnline && GameManager.Instance.Lobby.IsServer)
			NetworkTaskOrchestrator.Instance.LaunchClientTask("InputPhase", EndInputPhase);
	}

	private void FaceClosestEnemiesAfterRound ()
	{
		foreach (KeyValuePair<int, Entity.EntityState> lastState in m_stateOfLastPlayedAction)
		{
			if (lastState.Value != Entity.EntityState.Patroling && lastState.Value != Entity.EntityState.Fleeing)
				continue;

			if (m_entitiesHoldingFacingOrder.Contains(lastState.Key))
				continue;

			if (GameManager.Instance.GetEntityFromID(out Entity entity, lastState.Key))
				entity.AI.FaceClosestVisibleEnemy();
		}

		m_stateOfLastPlayedAction.Clear();
		m_entitiesHoldingFacingOrder.Clear();
	}

	private void TrackFacingOrder ( RecordedAction _recordedAction, bool _doesPlayFreeAction )
	{
		if (_doesPlayFreeAction && _recordedAction.freeAction is RotateEntityAction { isFacingOrder: true })
			m_entitiesHoldingFacingOrder.Add(_recordedAction.performingEntityID);
		else if (_recordedAction.action.Data.codeType != EntityActionData.ActionCodeType.Wait)
			m_entitiesHoldingFacingOrder.Remove(_recordedAction.performingEntityID);
	}

	[Button]
	public void EndInputPhase ()
	{
		EndDesignationWindow();
		CloseFacingPicker(false);
		currentPhase = TurnPhase.Calculating;

		//A half picked action leaves its tiles here, and the next AddAction takes them for its own - which
		//would hand the bot's first action the tiles the player was still choosing.
		m_currentActionTargetTiles.Clear();
		StartTurn();
	}

	#endregion

	#region Play phase

	[Button]
	public void StartTurn ()
	{
		LogConsole.AddLog("Start turn", LogConsole.LogEventType.DebugSys);
		m_actionsToPlay.Clear();
		m_actionsBeingDone.Clear();
		currentTick = 0;

		StartNextRoundTick();
	}

	private void StartNextRoundTick ()
	{
		m_currentTickStartTime = Time.time;
		LogConsole.AddLog("Start tick " + currentTick, LogConsole.LogEventType.DebugSys);

		foreach (int entityID in new List<int>(m_recordedActionInput.Keys))
		{
			if (m_actionsToPlay.ContainsKey(entityID))
				continue;

			Queue<RecordedAction> recordedActions = m_recordedActionInput[entityID];
			Queue<RecordedAction> actionsPlayedThisRound = new();
			m_actionsToPlay.Add(entityID, actionsPlayedThisRound);
			int totalCost = 0;
			while (totalCost < 1 && recordedActions.Count > 0)
			{
				RecordedAction recordedAction = recordedActions.Dequeue();
				LogConsole.AddLog(GameManager.Instance.GetEntityFromID(entityID).Data.name + " will play new action this tick " + recordedAction.action.ToString(), LogConsole.LogEventType.ActionResolution);
				actionsPlayedThisRound.Enqueue(recordedAction);
				totalCost += Mathf.Max(1, recordedAction.action.TotalDuration);
			}

			if (recordedActions.Count == 0)
				m_recordedActionInput.Remove(entityID);
		}

		currentPhase = TurnPhase.Calculating;
		GridManager.Instance.StartNewPhase(currentTick);

		foreach (Item item in GameManager.Instance.Items)
		{
			item.Data.OnActionTickStart(currentTick, item.LinkedData, item);
		}

		foreach (int entityID in new List<int>(m_actionsToPlay.Keys))
		{
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			Queue<RecordedAction> returnActionToPlayThisRound = new();
			foreach (RecordedAction recordedAction in m_actionsToPlay[entityID])
			{
				EntityAIPlugin.CheckActionResultInfo resultInfo = entity.AI.CheckAction(recordedAction);

				if (entity.IsAlliedTo(GameManager.Instance.PlayerID) && resultInfo.isActionChanging && resultInfo.replacementReasonTxt != null)
					LogConsole.AddLog(resultInfo.replacementReasonTxt, LogConsole.LogEventType.AICheck);

				if (recordedAction.action.lifetime > 0 || !resultInfo.isActionChanging)
				{
					if (recordedAction.action.IsPerformingAtTick(currentTick))
						recordedAction.action.ConflictCheckPrewarm();

					returnActionToPlayThisRound.Enqueue(recordedAction);
				}
				else
				{
					recordedAction.action.CancelAction();
					if (recordedAction.freeAction != null)
						recordedAction.freeAction.CancelAction();
					if (resultInfo.replacedFreeAction != null)
						resultInfo.replacedFreeAction.OnModActionAdded(resultInfo.replacedAction);

					resultInfo.replacedAction.wasReplacedByAI = true;

					if (resultInfo.replacedAction.IsPerformingAtTick(currentTick))
						resultInfo.replacedAction.ConflictCheckPrewarm();

					returnActionToPlayThisRound.Enqueue(new RecordedAction(resultInfo.replacedAction, recordedAction.entityState, resultInfo.replacedFreeAction));
				}
			}

			m_actionsToPlay[entityID] = returnActionToPlayThisRound;
		}

		int currentIteration = 0;
		m_recordedConflict = CheckForConflicts();
		while (m_recordedConflict.Count > 0 && currentIteration++ < 20)
			m_recordedConflict = ResolveConflicts();

		foreach (RecordedAction actionInConflict in m_recordedConflict)
			Debug.LogError("This action conflict cannot be resolved: " + actionInConflict.type);

		foreach (int entityID in m_actionsToPlay.Keys)
		{
			foreach (RecordedAction recordedAction in m_actionsToPlay[entityID])
			{
				if (!recordedAction.action.IsPerformingAtTick(currentTick))
					continue;

				recordedAction.action.orientationAtPerform = GetOrientationAtPerformOf(recordedAction);
				recordedAction.action.Prepare(recordedAction.entityState);
			}
		}

		if (!GameManager.Instance.IsOnline)
			PlayThisRoundActions();
		else
		{
			NetworkTaskOrchestrator.Instance.LaunchClientTask("PlayPhase", EndRoundTick);
			List<RecordedEntityActionsContainer> actionsToSend = new();

			foreach (var kvp in m_actionsToPlay)
			{
				actionsToSend.Add(new RecordedEntityActionsContainer
				{
					entityId = kvp.Key,
					actions = kvp.Value.ToArray()
				});
				foreach (RecordedAction recordedAction in kvp.Value)
					LogConsole.AddLog("Action sent: " + recordedAction.action.ToString(), LogConsole.LogEventType.InputPhase);
			}
			m_networkedTurnSystem.StartPlayPhaseClientRPC(actionsToSend.ToArray());
		}
	}

	private List<RecordedAction> CheckForConflicts ()
	{
		List<RecordedAction> conflicts = new();
		foreach (int entity in m_actionsToPlay.Keys)
		{
			foreach (RecordedAction action in m_actionsToPlay[entity])
			{
				if (!action.action.IsPerformingAtTick(currentTick))
					continue;

				bool wasPairedWithAnotherAction = false;
				foreach (int otherEntity in m_actionsToPlay.Keys)
				{
					if (entity == otherEntity) continue;

					foreach (RecordedAction otherAction in m_actionsToPlay[otherEntity])
					{
						if (!otherAction.action.IsPerformingAtTick(currentTick))
							continue;

						wasPairedWithAnotherAction = true;
						AEntityAction.ActionConflictResultInfo resultInfo = action.action.CheckConflict(otherAction.action);
						if (resultInfo.isFirstActionConflicted)
						{
							LogConsole.AddLog("Conflict detected: [" + action.action.ToString() + "]", LogConsole.LogEventType.ActionConflict);
							conflicts.Add(action);
						}
						else if (resultInfo.isSecondActionConflicted)
						{
							LogConsole.AddLog("Conflict detected: [" + otherAction.action.ToString() + "]", LogConsole.LogEventType.ActionConflict);
							conflicts.Add(otherAction);
						}
					}
				}

				if (!wasPairedWithAnotherAction && action.action.ConflictCheckAlone())
				{
					LogConsole.AddLog("Conflict detected: [" + action.action.ToString() + "]", LogConsole.LogEventType.ActionConflict);
					conflicts.Add(action);
				}
			}
		}

		return conflicts;
	}

	private List<RecordedAction> ResolveConflicts ()
	{
		List<RecordedAction> remainingConflict = new();

		foreach (RecordedAction conflictedAction in m_recordedConflict)
		{
			bool wasPairedWithAnotherAction = false;
			foreach (int otherEntity in m_actionsToPlay.Keys)
			{
				if (conflictedAction.performingEntityID == otherEntity) continue;

				foreach (RecordedAction otherAction in m_actionsToPlay[otherEntity])
				{
					if (!otherAction.action.IsPerformingAtTick(currentTick))
						continue;

					wasPairedWithAnotherAction = true;
					AEntityAction.ActionConflictResultInfo resultInfo = conflictedAction.action.CheckConflict(otherAction.action, false);
					if (resultInfo.isFirstActionConflicted)
					{
						LogConsole.AddLog("Conflict detected: [" + conflictedAction.action.ToString() + "]", LogConsole.LogEventType.ActionConflict);
						remainingConflict.Add(conflictedAction);
					}
					else if (resultInfo.isSecondActionConflicted)
					{
						LogConsole.AddLog("Conflict detected: [" + otherAction.action.ToString() + "]", LogConsole.LogEventType.ActionConflict);
						remainingConflict.Add(otherAction);
					}
				}
			}

			if (!wasPairedWithAnotherAction && conflictedAction.action.ConflictCheckAlone(false))
				remainingConflict.Add(conflictedAction);
		}

		return remainingConflict;
	}

	public void PlayThisRoundActions ()
	{
		m_tickPlayStartTime = Time.time;
		onNewRoundStart?.Invoke();
		currentPhase = TurnPhase.Playing;
		List<int> entityIDs = new(m_actionsToPlay.Keys);

		foreach (int entityID in entityIDs)
		{
			if (m_actionsToPlay.ContainsKey(entityID) && m_actionsToPlay[entityID] != null && m_actionsToPlay[entityID].Count > 0
				&& m_actionsToPlay[entityID].Peek().action.timeAtStart <= currentTick)
			{
				RecordedAction action = m_actionsToPlay[entityID].Dequeue();
				if (!m_actionsBeingDone.ContainsKey(entityID))
					m_actionsBeingDone.Add(entityID, new Tuple<RecordedAction, bool>(action, false));
				else
					m_actionsBeingDone[entityID] = new Tuple<RecordedAction, bool>(action, false);
			}
		}

		if (m_actionsBeingDone.Count == 0)
		{
			TryEndRoundTick();
			return;
		}

		foreach (Tuple<RecordedAction, bool> tuple in m_actionsBeingDone.Values.ToArray())
		{
			PlayActionTick(tuple.Item1);
		}
	}

	private bool DoesPlayFreeActionAtTick ( RecordedAction _recordedAction )
	{
		return _recordedAction.HasFreeAction
			&& _recordedAction.freeAction.lifetime < _recordedAction.freeAction.TotalDuration
			&& _recordedAction.action.IsPerformingAtTick(currentTick);
	}

	private int GetOrientationAtPerformOf ( RecordedAction _recordedAction )
	{
		Entity performingEntity = GameManager.Instance.GetEntityFromID(_recordedAction.performingEntityID);
		int currentOrientation = performingEntity == null ? -1 : performingEntity.Displacement.CurrentOrientation;

		if (!DoesPlayFreeActionAtTick(_recordedAction) || !_recordedAction.freeAction.IsPlayedFirst)
			return currentOrientation;

		return _recordedAction.freeAction is RotateEntityAction rotateAction
			&& rotateAction.targetedOrientationID != null
			&& rotateAction.targetedOrientationID.Length > rotateAction.lifetime
			? rotateAction.targetedOrientationID[rotateAction.lifetime]
			: currentOrientation;
	}

	private void PlayActionTick ( RecordedAction _recordedAction )
	{
		m_stateOfLastPlayedAction[_recordedAction.performingEntityID] = _recordedAction.entityState;
		bool doesPlayFreeAction = DoesPlayFreeActionAtTick(_recordedAction);
		bool isFreeActionPlayedFirst = doesPlayFreeAction && _recordedAction.freeAction.IsPlayedFirst;

		_recordedAction.action.doesFreeActionOwnFacing = isFreeActionPlayedFirst && EntityActionData.IsRotationAction(_recordedAction.freeAction.enumID);
		_recordedAction.action.tickTimeReserve = doesPlayFreeAction && !isFreeActionPlayedFirst
			? Mathf.Min(GameConfig.current.game.entityRotationDuration, TickDuration * .5f) : 0f;
		TrackFacingOrder(_recordedAction, doesPlayFreeAction);

		if (doesPlayFreeAction)
		{
			if (isFreeActionPlayedFirst)
			{
				_recordedAction.freeAction.onEndTick = ( performingEntity, didEndAction ) =>
				{
					_recordedAction.action.onEndTick = OnActionEndTick;
					_recordedAction.action.PerformTick(_recordedAction.entityState);
				};
				_recordedAction.freeAction.PerformTick(_recordedAction.entityState);
			}
			else
			{
				_recordedAction.action.onEndTick = ( performingEntityID, didEndMainAction ) =>
				{
					Entity performer = GameManager.Instance.GetEntityFromID(performingEntityID);
					if (performer == null || performer.Equipment.IsDead)
					{
						OnActionEndTick(performingEntityID, didEndMainAction);
						return;
					}

					_recordedAction.freeAction.onEndTick = ( entityID, _ ) => OnActionEndTick(entityID, didEndMainAction);
					_recordedAction.freeAction.PerformTick(_recordedAction.entityState);
				};
				_recordedAction.action.PerformTick(_recordedAction.entityState);
			}
		}
		else
		{
			_recordedAction.action.onEndTick = OnActionEndTick;
			_recordedAction.action.PerformTick(_recordedAction.entityState);
		}
	}

	private void OnActionEndTick ( int _performingEntityID, bool _didEndAction )
	{
		bool hasQueuedAction = _didEndAction && m_actionsToPlay.ContainsKey(_performingEntityID)
			&& m_actionsToPlay[_performingEntityID] != null && m_actionsToPlay[_performingEntityID].Count > 0;

		if (hasQueuedAction && m_actionsToPlay[_performingEntityID].Peek().action.timeAtStart <= currentTick)
		{
			RecordedAction action = m_actionsToPlay[_performingEntityID].Dequeue();
			m_actionsBeingDone[_performingEntityID] = new(action, false);
			PlayActionTick(action);
		}
		else if (hasQueuedAction)
		{
			Debug.LogWarning("entity " + _performingEntityID + " has " + m_actionsToPlay[_performingEntityID].Count
				+ " action(s) queued starting at tick " + m_actionsToPlay[_performingEntityID].Peek().action.timeAtStart
				+ " while current tick is " + currentTick);
			m_actionsBeingDone.Remove(_performingEntityID);
			TryEndRoundTick();
		}
		else
		{
			if (_didEndAction)
			{
				m_actionsToPlay.Remove(_performingEntityID);
				m_actionsBeingDone.Remove(_performingEntityID);
			}
			else
			{
				if (!m_actionsBeingDone.ContainsKey(_performingEntityID))
				{
					TryEndRoundTick();
					return;
				}

				if (!m_actionsToPlay.ContainsKey(_performingEntityID))
					m_actionsToPlay.Add(_performingEntityID, new());
				else
					m_actionsToPlay[_performingEntityID].Clear();

				RecordedAction action = m_actionsBeingDone[_performingEntityID].Item1;
				m_actionsToPlay[_performingEntityID].Enqueue(action);
				m_actionsBeingDone[_performingEntityID] = new(action, true);
			}

			TryEndRoundTick();
		}
	}

	private void TryEndRoundTick ()
	{
		bool areAllActionPerformed = true;
		foreach (int playerID in m_actionsBeingDone.Keys)
		{
			if (m_actionsBeingDone[playerID].Item2 == false)
				areAllActionPerformed = false;
		}

		if (areAllActionPerformed && m_inPlayEventBeingDone.Count == 0)
		{
			if (DelayTickEnd())
				return;

			if (!GameManager.Instance.IsOnline)
			{
				EndRoundTick();
			}
			else
			{
				LogConsole.AddLog("Client ended tick", LogConsole.LogEventType.DebugSys);
				NetworkTaskOrchestrator.Instance.NotifyTaskEndToServerRPC("PlayPhase");
			}
		}
	}

	private bool DelayTickEnd ()
	{
		if (m_tickEndDelay != null)
			return true;

		float remaining = TickDuration - (Time.time - m_tickPlayStartTime);
		if (remaining <= c_tickEndTolerance)
			return false;

		m_tickEndDelay = DOVirtual.DelayedCall(remaining, () =>
		{
			m_tickEndDelay = null;
			if (currentPhase == TurnPhase.Playing)
				TryEndRoundTick();
		});
		return true;
	}

	public void NotifyTickCompletionIfDone ()
	{
		if (currentPhase == TurnPhase.Playing)
			TryEndRoundTick();
	}

	public void AddGameEvent ( RecordedEvent _newGameEvent )
	{
		_newGameEvent.onEventFinished += OnGameEventEnded;
		m_inPlayEventBeingDone.Add(_newGameEvent);
	}

	private void OnGameEventEnded ( RecordedEvent _event )
	{
		m_inPlayEventBeingDone.Remove(_event);
		TryEndRoundTick();
	}

	private void EndRoundTick ()
	{
		if (currentPhase != TurnPhase.Playing)
			return;

		LogConsole.AddLog("Server ended tick " + currentTick, LogConsole.LogEventType.DebugSys);

		bool isRoundOver = m_recordedActionInput.Keys.Count == 0 || currentTick >= GameConfig.current.game.actionTokenPerRound - 1;
		if (isRoundOver)
			ScoreObjectivesAtRoundEnd();

		bool isGameFinished = IsGameFinished(out bool _playerOneWin, out bool _playerTwoWin);

		if (isGameFinished)
			EndTurn(true, _playerOneWin, _playerTwoWin);
		else if (isRoundOver)
			EndTurn(false, _playerOneWin, _playerTwoWin);
		else
		{
			currentTick++;
			StartNextRoundTick();
		}
	}

	private void OnEntityDeath ( int _entityID )
	{
		Entity deadEntity = GameManager.Instance.GetEntityFromID(_entityID);
		deadEntity.Equipment.onDeath -= OnEntityDeath;
		foreach (AEntityPassiveEffect.PassiveEffectContainer effetID in deadEntity.AllPassiveEffects)
		{
			GameAssets.current.game.entityEffects[effetID.enumID].OnDeathTrigger(deadEntity);
		}
		LogConsole.AddLog(deadEntity.Data.name + " died", LogConsole.LogEventType.DebugSys);

		m_recordedActionInput.Remove(_entityID);
		m_actionsToPlay.Remove(_entityID);
		m_actionsBeingDone.Remove(_entityID);
	}

	public EndLevelPopup.GameResult GetLocalGameResult ( bool _playerOneWin, bool _playerTwoWin )
	{
		if (_playerOneWin && _playerTwoWin)
			return EndLevelPopup.GameResult.Draw;

		bool didLocalPlayerLoose = GameManager.Instance.PlayerID == 0 ? _playerTwoWin : _playerOneWin;

		return didLocalPlayerLoose ? EndLevelPopup.GameResult.Loose : EndLevelPopup.GameResult.Win;
	}

	private void EndTurn (bool _isGameFinished, bool _playerOneWin, bool _playerTwoWin)
	{
		onEndPlayPhase?.Invoke();
		LogConsole.AddLog("EndRound", LogConsole.LogEventType.DebugSys);

		if (!GameManager.Instance.IsOnline)
		{
			if (_isGameFinished)
				EndLevel(GetLocalGameResult(_playerOneWin, _playerTwoWin));
			else
				StartInputPhase();
		}
		else
		{
			if (m_networkedTurnSystem.IsServer && !m_networkedTurnSystem.IsHost)
			{
				if (_isGameFinished)
					EndLevel(GetLocalGameResult(_playerOneWin, _playerTwoWin));
				else
					StartInputPhase();
			}
			m_networkedTurnSystem.EndRoundClientRPC(_isGameFinished, _playerOneWin, _playerTwoWin);
		}

	}

	private bool IsGameFinished ( out bool _playerOneWin, out bool _playerTwoWin )
	{
#if UNITY_EDITOR
		if (isMatchEndDisabled)
		{
			_playerOneWin = false;
			_playerTwoWin = false;
			return false;
		}
#endif
		bool isPlayerOneAlive = false;
		bool isPlayerTwolive = false;
		foreach (Entity ally in GameManager.Instance.PlayersEntityAnchor[0].Entities)
			if (!ally.Equipment.IsDead && !ally.Data.FrameData.isImmortal)
				isPlayerOneAlive = true;

		foreach (Entity enemy in GameManager.Instance.PlayersEntityAnchor[1].Entities)
			if (!enemy.Equipment.IsDead && !enemy.Data.FrameData.isImmortal)
				isPlayerTwolive = true;

		_playerOneWin = !isPlayerTwolive;
		_playerTwoWin = !isPlayerOneAlive;
		switch (GameManager.Instance.CurrentMission.type)
		{
			case MissionData.MissionType.Extermination:
				break;
			case MissionData.MissionType.DefenseDeZone:
				if (GameManager.Instance.GetPlayerPoints(0) >= GameConfig.current.game.zonePointsToWin)
					_playerOneWin = true;
				if (GameManager.Instance.GetPlayerPoints(1) >= GameConfig.current.game.zonePointsToWin)
					_playerTwoWin = true;
				break;
			case MissionData.MissionType.ControleDePoint:
				int controlPointCount = GridManager.Instance.ControlPoints.Count;
				if (controlPointCount > 0 && GameManager.Instance.GetPlayerPoints(0) == controlPointCount)
					_playerOneWin = true;
				if (controlPointCount > 0 && GameManager.Instance.GetPlayerPoints(1) == controlPointCount)
					_playerTwoWin = true;
				break;
			case MissionData.MissionType.Construction:
				if (GridManager.Instance.AreCommPointsLinked(0, CableItemData.GetPlacedCableTiles(0)))
					_playerOneWin = true;
				if (GridManager.Instance.AreCommPointsLinked(1, CableItemData.GetPlacedCableTiles(1)))
					_playerTwoWin = true;
				break;
			case MissionData.MissionType.Sabotage:
				if (GameManager.Instance.GetSabotagePointCount(0, false) > 0 && GameManager.Instance.GetSabotagePointCount(0, true) == 0)
					_playerTwoWin = true;
				if (GameManager.Instance.GetSabotagePointCount(1, false) > 0 && GameManager.Instance.GetSabotagePointCount(1, true) == 0)
					_playerOneWin = true;
				break;
			case MissionData.MissionType.Assassinat:
				if (GameManager.Instance.PlayersEntityAnchor[0].IsKingDead)
					_playerTwoWin = true;
				if (GameManager.Instance.PlayersEntityAnchor[1].IsKingDead)
					_playerOneWin = true;

				break;
		}

		return _playerOneWin || _playerTwoWin;
	}

	private void ScoreObjectivesAtRoundEnd ()
	{
		MissionData mission = GameManager.Instance.CurrentMission;
		if (mission == null)
			return;

		switch (mission.type)
		{
			case MissionData.MissionType.DefenseDeZone:
				int controllingPlayer = GetControllingPlayer(GameManager.Instance.PlayersEntityAnchor[0].Zones);
				if (controllingPlayer != -1)
					GameManager.Instance.AddPointsToPlayer(controllingPlayer, 1);
				LogZoneControl(controllingPlayer);
				break;
			case MissionData.MissionType.ControleDePoint:
				if (GridManager.Instance.ControlPoints.Count == 0)
					break;

				int[] heldControlPoints = new int[GameManager.Instance.PlayersEntityAnchor.Length];
				foreach (HashSet<Tile> controlPoint in GridManager.Instance.ControlPoints)
				{
					int owner = GetControllingPlayer(controlPoint);
					if (owner != -1)
						heldControlPoints[owner]++;
				}

				for (int i = 0; i < heldControlPoints.Length; i++)
					GameManager.Instance.SetPlayerPoints(i, heldControlPoints[i]);
				LogControlPoints();
				break;
		}
	}

	private void LogControlPoints ()
	{
		int playerID = GameManager.Instance.PlayerID;
		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_control_points)
			, GameManager.Instance.GetPlayerPoints(playerID), GameManager.Instance.GetPlayerPoints(1 - playerID), GridManager.Instance.ControlPoints.Count)
			, LogConsole.LogEventType.Round);
	}

	private void LogZoneControl ( int _controllingPlayer )
	{
		int playerID = GameManager.Instance.PlayerID;
		LocalizationKey key = _controllingPlayer == -1 ? LocalizationKey.log_zone_point_none
			: _controllingPlayer == playerID ? LocalizationKey.log_zone_point_ally : LocalizationKey.log_zone_point_enemy;

		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(key), GameManager.Instance.GetPlayerPoints(playerID), GameManager.Instance.GetPlayerPoints(1 - playerID))
			, LogConsole.LogEventType.Round);
	}

	public int GetControllingPlayer ( HashSet<Tile> _tiles )
	{
		int controllingPlayer = -1;
		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity.Equipment.IsDead || !_tiles.Contains(entity.Displacement.Coordinates.GetTile()))
					continue;

				if (controllingPlayer == -1)
					controllingPlayer = entity.OwnerID;
				else if (controllingPlayer != entity.OwnerID)
					return -1;
			}
		}

		return controllingPlayer;
	}

	[Button]
	public void EndLevel ( EndLevelPopup.GameResult _gameResult )
	{
		if (currentPhase == TurnPhase.Off)
			return;

		m_recordedActionInput.Clear();
		m_actionsToPlay.Clear();
		m_actionsBeingDone.Clear();
		m_remainingActionToken.Clear();

		currentPhase = TurnPhase.Off;
		GameManager.Instance.EndGame(_gameResult);
		onEndLevel?.Invoke();
	}

	public void AddEntityMidGame ( Entity _entity, Action _onEndSpawn = null )
	{
		int remainingActionTickThisTurn = GameConfig.current.game.actionTokenPerRound - currentTick;

		Entity.EntityState availableState = GetDefaultStateOf(_entity);
		if (!m_recordedActionInput.ContainsKey(_entity.ID))
			m_recordedActionInput.Add(_entity.ID, new());

		for (int i = 1; i < remainingActionTickThisTurn; i++)
			m_recordedActionInput[_entity.ID].Enqueue(new RecordedAction(GetAction(EntityActionEnumID.Wait, _entity.ID, null, currentTick + i), availableState));

		_onEndSpawn?.Invoke();
	}

	#endregion

}
