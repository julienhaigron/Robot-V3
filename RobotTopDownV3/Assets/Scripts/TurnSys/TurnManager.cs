using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;
using System.Linq;
using System;

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
	public static Action onEndLevel;
	public static Action onActionTargetsChanged;
	
	public static int currentTick = 0;

	[SerializeField] private NetworkedTurnSystem m_networkedTurnSystem;

	[SerializeField] private SerializableDictionary<int, Queue<RecordedAction>> m_recordedActionInput = new(); //all actions this turn
	public SerializableDictionary<int, Queue<RecordedAction>> RecordedActions => m_recordedActionInput;
	[SerializeField] private SerializableDictionary<int, Queue<RecordedAction>> m_actionsToPlay = new(); //this tick actions
	public SerializableDictionary<int, Queue<RecordedAction>> ActionsToPlay => m_actionsToPlay;
	private SerializableDictionary<int, Tuple<RecordedAction, bool>> m_actionsBeingDone = new(); //current actions running

	private SerializableDictionary<int, int> m_remainingActionToken = new();
	public SerializableDictionary<int, int> RemainingActionToken => m_remainingActionToken;

	private List<RecordedAction> m_recordedConflict;

	private AEntityAction m_currentEntityAction;
	public AEntityAction CurrentActionSelected => m_currentEntityAction;
	private EntityActionEnumID m_currentActionTypeSelected;
	public EntityActionEnumID CurrentActionTypeSelected => m_currentActionTypeSelected;

	private AEntityAction m_currentEntityModAction;
	public AEntityAction CurrentModActionSelected => m_currentEntityAction;
	private EntityActionEnumID m_currentModActionTypeSelected;
	public EntityActionEnumID CurrentModActionTypeSelected => m_currentActionTypeSelected;

	private string m_currentEquipmentLinkedToActionTypeSelected;
	public string CurrentEquipmentLinkedToActionTypeSelected => m_currentEquipmentLinkedToActionTypeSelected;

	private Entity.EntityState m_currentStateTypeSelected;
	public Entity.EntityState CurrentStateTypeSelected => m_currentStateTypeSelected;

	private List<Tile> m_currentActionTargetTiles = new();
	public List<Tile> CurrentActionTargetTiles => m_currentActionTargetTiles;

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
			usedActionThisTurn = new();
		}
	}

	private Dictionary<int, EntireGameTrackedEvents> m_entireGameTrackedEventsPerEntity = new();
	public Dictionary<int, EntireGameTrackedEvents> EntireGameTrackedEventsPerEntity => m_entireGameTrackedEventsPerEntity;
	[Serializable]
	public class EntireGameTrackedEvents
	{
		public HashSet<EntityActionEnumID> usedActions = new();
	}

	private List<RecordedEvent> m_inPlayEventBeingDone = new();
	public class RecordedEvent
	{
		public Action<RecordedEvent> onEventFinished;

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
			if (serializer.IsWriter)
			{
				freeAction.NetworkSerialize(serializer);
			}
			else
			{
				freeAction = Instance.GetAction(GameAssets.current.game.entityActionsData[freeActionType]
					, performingEntityID, linkedEquipmentID, timeAtStart);

				if (freeAction == null)
				{
					Debug.LogError("ERROR : freeAction is null when " + (serializer.IsWriter ? "writing" : "reading") + " with type " + type);
				}
				freeAction.NetworkSerialize(serializer);
			}
		}

		public void AddActionMod ( AEntityAction _actionMod, bool _isPlayedFirst )
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
		PlayerController.Instance.ClearHoverPreviews();

		if (EntityActionDisplay.SelectedDisplay != null)
			EntityActionDisplay.SelectedDisplay.Deselect();

		if (_selectedEntity.HasValue)
		{
			Entity selectedEntity = GameManager.Instance.GetEntityFromID(_selectedEntity.Value);
			SetCurrentActionSelected(selectedEntity.AI.GetMovementAction().enumID, null, true);
			SetCurrentStateSelected(selectedEntity.KnownedStates[0]);
			//SetCurrentModActionSelected(selectedEntity.KnownedModActions[0], selectedEntity.ComponentLinkedToAction[selectedEntity.KnownedModActions[0]][0], true);
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
		PlayerController.Instance.ClearHoverPreviews();

		if (_selectedDisplay != null)
		{
			hasModActionSelected = _isModAction;
			m_currentStateTypeSelected = _selectedDisplay.RecordedAction.entityState;
			m_currentActionTargetTiles.Clear();
			AEntityAction action = _isModAction ? _selectedDisplay.RecordedAction.freeAction : _selectedDisplay.RecordedAction.action;
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
				SetCurrentStateSelected(PlayerController.Instance.SelectedEntity.KnownedStates[0]);
			}
		}
	}

	#endregion

	public void Init ()
	{
		currentTick = 0;
		m_trackedEventsPerEntity.Clear();
		m_entireGameTrackedEventsPerEntity.Clear();
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

	public void SetCurrentStateSelected ( Entity.EntityState _state )
	{
		m_currentStateTypeSelected = _state;
		onStateSelected?.Invoke(_state);

		RefreshActionDisplay(PlayerController.Instance.SelectedEntity != null ? PlayerController.Instance.SelectedEntity.ID : null, false);
	}

	public void SetCurrentActionSelected ( EntityActionEnumID _action, string _linkedEquipmentID, bool _isResetingAction )
	{
		int performingEntityID = PlayerController.Instance.SelectedEntity.ID;
		int timeAtStart = m_recordedActionInput.ContainsKey(performingEntityID) && m_recordedActionInput[performingEntityID].Count > 0
			? m_recordedActionInput[performingEntityID].ToArray()[^1].action.TimeAtEnd : currentTick;

		hasModActionSelected = false;
		m_currentActionTypeSelected = _action;
		m_currentEquipmentLinkedToActionTypeSelected = _linkedEquipmentID;
		if (_isResetingAction)
		{
			m_currentActionTargetTiles.Clear();
			m_currentEntityAction = GetAction(GameAssets.current.game.entityActionsData[_action], performingEntityID, _linkedEquipmentID, timeAtStart);

			m_currentEntityAction.OnSelectActionTileInteractPredicatePrewarm();
			onActionSelected?.Invoke(m_currentEntityAction);
		}
	}

	public void SetCurrentModActionSelected ( EntityActionEnumID _action, string _linkedEquipmentID, bool _isResetingAction )
	{
		int performingEntityID = PlayerController.Instance.SelectedEntity.ID;
		int timeAtStart = m_recordedActionInput.ContainsKey(performingEntityID) && m_recordedActionInput[performingEntityID].Count > 0
			? m_recordedActionInput[performingEntityID].ToArray()[^1].action.TimeAtEnd : currentTick;

		hasModActionSelected = true;
		m_currentModActionTypeSelected = _action;
		m_currentEquipmentLinkedToActionTypeSelected = _linkedEquipmentID;

		if (_isResetingAction)
		{
			m_currentActionTargetTiles.Clear();
			m_currentEntityAction = GetAction(GameAssets.current.game.entityActionsData[_action], performingEntityID, _linkedEquipmentID, timeAtStart);

			m_currentEntityAction.OnSelectActionTileInteractPredicatePrewarm();
			onActionSelected?.Invoke(m_currentEntityAction);
		}
	}

	public void AddTargetTileInCurrentAction ( Tile _tile )
	{
		m_currentActionTargetTiles.Add(_tile);
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

			bool hasFreeAction = recordedAction.freeActionType != EntityActionEnumID.Unknowned && recordedAction.freeActionType != EntityActionEnumID.Wait;
			if (hasFreeAction && recordedAction.freeAction != null && recordedAction.freeAction.IsMissingTarget())
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
		List<Tile> zoneTiles = new();

		if (_action.PerformingEntity != null)
		{
			if (_action.Data.DoesResolveItsOwnTarget())
			{
				Tile from = GridManager.Instance.Tiles[_action.supposedPositionAtActionStartID];
				zoneTiles.AddRange(_action.PerformingEntity.Equipment.GetTilesInReach(_action, from, false, true));
			}
			else if (_action.Data.aoeType != EntityActionData.AOEType.Noone)
			{
				foreach (Tile targetTile in m_currentActionTargetTiles)
				{
					if (targetTile != null)
						zoneTiles.AddRange(_action.PerformingEntity.Equipment.GetTilesInAoERange(_action, targetTile, true));
				}
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

	/// <summary>
	/// Queues the selected action right away, with no target, when it is one that aims at an entity: the
	/// player picks the action and EntityAIPlugin.CheckAction resolves what it shoots at when its tick comes
	/// up. Returns false for anything that still needs the player to click a tile.
	/// Called from the action button and not from SetCurrentActionSelected, which RefreshActionDisplay
	/// re-enters on every refresh - the action would be queued again on each one.
	/// </summary>
	public bool TryRegisterActionWithoutTarget ()
	{
		if (hasModActionSelected || m_currentEntityAction == null || !m_currentEntityAction.Data.DoesResolveItsOwnTarget())
			return false;

		int performingEntityID = m_currentEntityAction.performingEntityID;
		RegisterAction(performingEntityID, m_currentEntityAction, m_currentStateTypeSelected);

		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		EntityActionData movementAction = performingEntity == null ? null : performingEntity.AI.GetMovementAction();
		if (movementAction != null)
			SetCurrentActionSelected(movementAction.enumID, null, true);

		RefreshActionDisplay(performingEntityID, true);
		return true;
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
			/*case EntityActionData.ActionCodeType.NeighborMove:
				action = new MoveToNeighborAction();
				break;*/
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
				action = new SpecialAction();
				break;
			case EntityActionData.ActionCodeType.InvokeEntity:
				action = new InvokeEntityAction();
				break;
			case EntityActionData.ActionCodeType.ApplyEffect:
				action = new ApplyEffectAction();
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
			if (m_currentActionTargetTiles != null && m_currentActionTargetTiles.Count > 0)
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
		else
		{
			if (hasModActionSelected)
				m_currentEntityModAction = _action;
			else
				AddAction(_entityID, _action, _state);
		}

	}

	public bool RegisterActionAndMod( int _entityID, AEntityAction _action, AEntityAction _modAction, Entity.EntityState _state )
	{
		if (m_recordedActionInput.ContainsKey(_entityID) == false)
			m_recordedActionInput.Add(_entityID, new());

		if (m_remainingActionToken[_entityID] <= 0)
			return false;

		if (m_currentActionTargetTiles != null && m_currentActionTargetTiles.Count > 0)
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
		m_currentActionTargetTiles.Clear();

		RecordedAction recordedAction = new RecordedAction
		{
			timeAtStart = _action.timeAtStart,
			type = _action.enumID,
			performingEntityID = _entityID,
			action = _action,
			entityState = _state,
			freeAction = _modAction,
			freeActionType = _modAction.enumID
		};

		m_recordedActionInput[_entityID].Enqueue(recordedAction);
		m_remainingActionToken[_entityID] -= _action.TotalDuration;

		TrackedEventCheck();

		LogConsole.AddLog("Add " + _action.ToString() + " action to queue.", LogConsole.LogEventType.InputPhase);
		//Update action display on grid + UI
		onActionAdded?.Invoke(recordedAction);
		return true;
	}

	public bool AddAction ( int _entityID, EntityActionEnumID _actionType, Entity.EntityState _state, string _linkedEquipmentID )
	{
		AEntityAction action = null;
		int timeAtStart = m_recordedActionInput.ContainsKey(_entityID) && m_recordedActionInput[_entityID].Count > 0
			? m_recordedActionInput[_entityID].ToArray()[^1].action.TimeAtEnd : currentTick;

		action = GetAction(GameAssets.current.game.entityActionsData[_actionType], _entityID, _linkedEquipmentID, timeAtStart);
		return AddAction(_entityID, action, _state);
	}

	public bool AddAction ( int _entityID, AEntityAction _action, Entity.EntityState _state )
	{
		if (m_recordedActionInput.ContainsKey(_entityID) == false)
			m_recordedActionInput.Add(_entityID, new());

		if (m_remainingActionToken[_entityID] <= 0)
			return false;

		if (m_currentActionTargetTiles != null && m_currentActionTargetTiles.Count > 0)
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
		m_currentActionTargetTiles.Clear();

		RecordedAction recordedAction = new RecordedAction
		{
			timeAtStart = _action.timeAtStart,
			type = _action.enumID,
			performingEntityID = _entityID,
			action = _action,
			entityState = _state,
			freeAction = m_currentEntityModAction,
			freeActionType = m_currentModActionTypeSelected
		};

		m_currentEntityModAction = null;
		m_currentModActionTypeSelected = EntityActionEnumID.Wait;

		m_recordedActionInput[_entityID].Enqueue(recordedAction);
		m_remainingActionToken[_entityID] -= _action.TotalDuration;

		/*if (!m_lastRecordedAction.ContainsKey(_entityID))
			m_lastRecordedAction.Add(_entityID, recordedAction);
		else
			m_lastRecordedAction[_entityID] = recordedAction;*/

		TrackedEventCheck();

		LogConsole.AddLog("Add " + _action.ToString() + " action to queue.", LogConsole.LogEventType.InputPhase);
		//Update action display on grid + UI
		onActionAdded?.Invoke(recordedAction);
		return true;
	}

	public void RemoveActionFrom ( RecordedAction _actionToStartRemoveFrom, int _recordedActionPositionInQueue )
	{
		if (!m_recordedActionInput.ContainsKey(_actionToStartRemoveFrom.performingEntityID)
			|| m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID].Count <= _recordedActionPositionInQueue)
			return;

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

		CurrentActionSelected.timeAtStart = m_recordedActionInput.ContainsKey(_actionToStartRemoveFrom.performingEntityID) && m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID].Count > 0
			? m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID].ToArray()[^1].action.TimeAtEnd : 0;

		onActionRemoved?.Invoke(_actionToStartRemoveFrom);

		TrackedEventCheck();
		RefreshActionDisplay(_actionToStartRemoveFrom.performingEntityID, true);
	}

	public AEntityAction GetActionPerformedAtTick ( int _entityID )
	{
		if (m_actionsToPlay.ContainsKey(_entityID) && m_actionsToPlay[_entityID] != null)
		{
			foreach (RecordedAction recordedAction in m_actionsToPlay[_entityID])
			{
				if (recordedAction.action.IsPerformingAtTick(currentTick))
					return recordedAction.action;
			}
		}

		if (m_actionsBeingDone.ContainsKey(_entityID) && m_actionsBeingDone[_entityID] != null)
		{
			AEntityAction actionBeingDone = m_actionsBeingDone[_entityID].Item1.action;
			if (actionBeingDone.IsPerformingAtTick(currentTick))
				return actionBeingDone;
		}

		return null;
	}

	public int GetEntityPositionAtEndOfTick ( int _entityID, int _defaultTileID )
	{
		if (m_actionsToPlay.ContainsKey(_entityID) && m_actionsToPlay[_entityID] != null)
		{
			foreach (RecordedAction recordedAction in m_actionsToPlay[_entityID])
			{
				if (recordedAction.action.IsPerformingAtTick(currentTick))
					return recordedAction.action.positionAtActionEndID;
			}
		}

		if (m_actionsBeingDone.ContainsKey(_entityID) && m_actionsBeingDone[_entityID] != null)
		{
			AEntityAction actionBeingDone = m_actionsBeingDone[_entityID].Item1.action;
			if (actionBeingDone.IsPerformingAtTick(currentTick))
				return actionBeingDone.positionAtActionEndID;
		}

		return _defaultTileID;
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

	public bool HasRotationFreeActionThisTick ( int _entityID )
	{
		if (m_actionsToPlay.ContainsKey(_entityID) && m_actionsToPlay[_entityID] != null)
		{
			foreach (RecordedAction recordedAction in m_actionsToPlay[_entityID])
			{
				if (recordedAction.action.IsPerformingAtTick(currentTick))
					return IsRotationFreeAction(recordedAction);
			}
		}

		if (m_actionsBeingDone.ContainsKey(_entityID) && m_actionsBeingDone[_entityID] != null)
		{
			RecordedAction actionBeingDone = m_actionsBeingDone[_entityID].Item1;
			if (actionBeingDone.action.IsPerformingAtTick(currentTick))
				return IsRotationFreeAction(actionBeingDone);
		}

		return false;
	}

	private bool IsRotationFreeAction ( RecordedAction _recordedAction )
	{
		return _recordedAction.freeAction != null
			&& _recordedAction.freeActionType != EntityActionEnumID.Wait
			&& _recordedAction.freeActionType != EntityActionEnumID.Unknowned
			&& _recordedAction.freeAction.Data.type == EntityActionData.ActionType.Rotation;
	}

	public int GetLastRegisteredPositionOfEntity ( int _entityID )
	{
		if (!m_recordedActionInput.ContainsKey(_entityID) || m_recordedActionInput[_entityID] == null || m_recordedActionInput[_entityID].Count == 0)
			return GameManager.Instance.GetEntityFromID(_entityID).Displacement.Coordinates.ID;

		return m_recordedActionInput[_entityID].ToArray()[^1].action.positionAtActionEndID;
	}

	public int GetLastRegisteredOrientationOfEntity(int _entityID )
	{
		int orientation = GameManager.Instance.GetEntityFromID(_entityID).Displacement.CurrentOrientation;
		if (!m_recordedActionInput.ContainsSerializedKey(_entityID) || m_recordedActionInput[_entityID] == null || m_recordedActionInput[_entityID].Count == 0)
			return orientation;

		foreach(RecordedAction recordedAction in m_recordedActionInput[_entityID])
		{
			//Type match rather than enumID: there are two rotations now, RotateEntity aimed at a tile and
			//RotateToEntity aimed at an entity, and both end up in this slot.
			if (recordedAction.freeAction is RotateEntityAction rotateEntityAction
				&& rotateEntityAction.targetedOrientationID != null && rotateEntityAction.targetedOrientationID.Length > 0)
				orientation = rotateEntityAction.targetedOrientationID[^1];
		}

		return orientation;
	}

	public int GetPositionOfEntityAtEndOfRound ( int _entityID )
	{
		if (currentPhase != TurnPhase.Calculating)
			return -1;

		if (m_actionsToPlay.ContainsKey(_entityID) == false
			|| m_actionsToPlay[_entityID] == null || m_actionsToPlay[_entityID].Count == 0)
		{
			if (m_actionsBeingDone.ContainsKey(_entityID))
				return m_actionsBeingDone[_entityID].Item1.action.positionAtActionEndID;
			else
				return GameManager.Instance.GetEntityFromID(_entityID).Displacement.Coordinates.ID;
		}
		else
			return m_actionsToPlay[_entityID].ToList()[^1].action.positionAtActionEndID;
	}

	private void TrackedEventCheck ()
	{
		foreach (KeyValuePair<int, TrackedEntityEvents> pair in m_trackedEventsPerEntity)
		{
			pair.Value.ResetAllValues();

			if (!m_recordedActionInput.ContainsKey(pair.Key))
				continue;

			foreach (RecordedAction recordedAction in m_recordedActionInput[pair.Key].ToArray())
			{
				if (recordedAction.action.Data.type == EntityActionData.ActionType.Movement)
				{
					pair.Value.firstTimeEntityMoved = recordedAction.action.timeAtStart;
					pair.Value.traveledTileCountThisTurn++;
				}
				if (recordedAction.action.Data.type == EntityActionData.ActionType.DistanceAttack || recordedAction.action.Data.type == EntityActionData.ActionType.MeleeAttack)
					pair.Value.firstTimeEntityAttacked = recordedAction.action.timeAtStart;
				if (!pair.Value.usedActionThisTurn.Contains(recordedAction.type))
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
			if (hasModActionSelected)
				SetCurrentModActionSelected(m_currentActionTypeSelected, m_currentEquipmentLinkedToActionTypeSelected, _isResetingAction);
			else
				SetCurrentActionSelected(m_currentActionTypeSelected, m_currentEquipmentLinkedToActionTypeSelected, _isResetingAction);
		}

		AEntityAction currentSelectedAction = EntityActionDisplay.SelectedDisplay != null
			? (hasModActionSelected ? EntityActionDisplay.SelectedDisplay.RecordedAction.freeAction : EntityActionDisplay.SelectedDisplay.RecordedAction.action)
			: (hasModActionSelected ? CurrentModActionSelected : CurrentActionSelected);

		// display all player entity actions
		foreach (int entityID in m_recordedActionInput.Keys)
		{
			int totalCost = 0;
			RecordedAction lastRecordedAction = new();
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			Tile lastRecordedPosition = entity.Displacement.Coordinates.GetTile();
			int lastRecordedOrientation = entity.Displacement.CurrentOrientation;

			foreach (RecordedAction recordedAction in m_recordedActionInput[entityID].ToArray())
			{
				lastRecordedAction = recordedAction;
				totalCost += recordedAction.action.TotalDuration;
				recordedAction.action.Display(recordedAction);

				if (recordedAction.freeActionType != EntityActionEnumID.Unknowned && recordedAction.freeActionType != EntityActionEnumID.Wait)
					recordedAction.freeAction.Display(recordedAction);

				if (_specificTokenCount != -1 && totalCost <= _specificTokenCount)
				{
					lastRecordedPosition = GridManager.Instance.Tiles[recordedAction.action.positionAtActionEndID];
					//Both rotations land in this slot, so the type is what identifies one, not its enumID
					if (recordedAction.freeAction is RotateEntityAction rotateEntityAction
						&& rotateEntityAction.targetedOrientationID != null && rotateEntityAction.targetedOrientationID.Length > 0)
						lastRecordedOrientation = rotateEntityAction.targetedOrientationID[^1];
				}
			}

			if (_selectedEntityID.HasValue)
			{
				Tile ghostPosition = lastRecordedPosition;
				if (EntityActionDisplay.SelectedDisplay != null && entityID == _selectedEntityID.Value)
				{
					lastRecordedAction = EntityActionDisplay.SelectedDisplay.RecordedAction;
					ghostPosition = GridManager.Instance.Tiles[lastRecordedAction.action.positionAtActionEndID];
				}

				PlayerController.Instance.AddGhostEntityAt(entity, ghostPosition, lastRecordedOrientation);
			}
		}

		if (_selectedEntityID.HasValue && _specificTokenCount != -1 && currentSelectedAction != null)
		{
			currentSelectedAction.DisplayAoEPreviewOnHoveredTile();
			currentSelectedAction.GhostDisplay(m_currentStateTypeSelected);
		}

		PlayerController.Instance.RedrawTargetOutlines();
	}

	[Button]
	public void StartInputPhase ()
	{
		currentTick = 0;
		currentPhase = TurnPhase.Recording;
		//UIManager.Instance.OpenPanel<InGamePanel>();
		LogConsole.AddLog("Start Input phase", LogConsole.LogEventType.DebugSys);

		//reset RemainingActionToken
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

		onStartInputPhase?.Invoke();
		//PlayerController.Instance.SelectEntity(GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities[0]);

		if (GameManager.Instance.IsOnline && GameManager.Instance.Lobby.IsServer)
			NetworkTaskOrchestrator.Instance.LaunchClientTask("InputPhase", EndInputPhase);
	}

	[Button]
	public void EndInputPhase ()
	{
		currentPhase = TurnPhase.Calculating;

		//A half picked action leaves its tiles here, and the next AddAction takes them for its own - which
		//would hand the bot's first action the tiles the player was still choosing.
		m_currentActionTargetTiles.Clear();
		/*SerializableDictionary<int, Queue<RecordedAction>> recordedActionInput = new(m_recordedActionInput);
		m_recordedActionInput.Clear();
		foreach (int entityID in recordedActionInput.Keys)
		{
			m_recordedActionInput.Add(entityID, new Queue<RecordedAction>());

			foreach (RecordedAction record in recordedActionInput[entityID])
			{
				m_recordedActionInput[entityID].Enqueue(record);
				if (record.action.cost > 1)
				{
					//add wait tile for each actions in queue
					for (int i = 0; i < record.action.cost - 1; i++)
					{
						m_recordedActionInput[entityID].Enqueue(new RecordedAction
						{
							type = EntityActionEnumID.Wait,
							performingEntityID = entityID,
							action = new WaitAction(),
							entityState = record.entityState
						});
					}
				}

			}
		}*/

		//if( !GameManager.Instance.IsOnline)
		StartTurn();
		/*else
		{
			//send actions to server and wait for all players
			//if all player ready then start Round
			here
		}*/
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
		LogConsole.AddLog("Start tick " + currentTick, LogConsole.LogEventType.DebugSys);

		//1 - calculate phase

		//a)get all actions played by entities in one tick
		SerializableDictionary<int, Queue<RecordedAction>> recordedActions = new(m_recordedActionInput);
		//m_actionsToPlay.Clear();
		foreach (int entityID in m_recordedActionInput.Keys)
		{
			if (m_actionsToPlay.ContainsKey(entityID))
				continue;

			Queue<RecordedAction> actionsPlayedThisRound = new();
			m_actionsToPlay.Add(entityID, actionsPlayedThisRound);
			int totalCost = 0;
			while (totalCost < 1 && recordedActions[entityID].Count > 0)
			{
				RecordedAction recordedAction = recordedActions[entityID].Dequeue();
				LogConsole.AddLog(entityID + " will play new action this tick " + recordedAction.action.ToString(), LogConsole.LogEventType.ActionResolution);
				m_actionsToPlay[entityID].Enqueue(recordedAction);
				totalCost += Mathf.Max(1, recordedAction.action.TotalDuration);
			}

			if (m_recordedActionInput[entityID].Count == 0)
			{
				recordedActions.Remove(entityID);
				continue;
			}
		}
		m_recordedActionInput = new(recordedActions);

		currentPhase = TurnPhase.Calculating;
		GridManager.Instance.StartNewPhase(currentTick);

		//call GameManager.Items => item.OnActyionTIck
		foreach (Item item in GameManager.Instance.Items)
		{
			item.Data.OnActionTickStart(currentTick, item.LinkedData, item);
		}

		//AI Check
		List<int> entityIDs = new(m_actionsToPlay.Keys);

		foreach (int entityID in entityIDs)
		{
			Queue<RecordedAction> returnActionToPlayThisRound = new Queue<RecordedAction>();
			foreach (RecordedAction recordedAction in m_actionsToPlay[entityID].ToArray())
			{
				//Entities check in new EntityUILogic.cs wheter action changes in another depending on factors checked in said script
				//ex: MoveAction changes to ShootAction because of a Entity visible in coneRange
				//    => cone range trigger is in EntityUILogic.cs

				EntityAIPlugin.CheckActionResultInfo resultInfo = GameManager.Instance.GetEntityFromID(entityID).AI.CheckAction(recordedAction);

				if (GameManager.Instance.GetEntityFromID(entityID).IsAlliedTo(GameManager.Instance.PlayerID) 
					&& resultInfo.isActionChanging && resultInfo.replacementReasonTxt != null)
					LogConsole.AddLog(resultInfo.replacementReasonTxt/* + ", action " + recordedAction.action + " replaced to " + resultInfo.replacedAction*/, LogConsole.LogEventType.AICheck);

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

					if (resultInfo.replacedAction.IsPerformingAtTick(currentTick))
						resultInfo.replacedAction.ConflictCheckPrewarm();

					returnActionToPlayThisRound.Enqueue(new RecordedAction()
					{
						timeAtStart = resultInfo.replacedAction.timeAtStart,
						type = resultInfo.replacedAction.enumID,
						performingEntityID = resultInfo.replacedAction.performingEntityID,
						action = resultInfo.replacedAction,
						entityState = recordedAction.entityState,
						freeAction = resultInfo.replacedFreeAction,
						freeActionType = resultInfo.replacedFreeAction == null ? EntityActionEnumID.Wait : resultInfo.replacedFreeAction.enumID
					});
				}
			}

			m_actionsToPlay[entityID] = new(returnActionToPlayThisRound);
		}

		//2-recursively check for possible conflict and change actions if needed
		//	     => dealing with conflict can create new one
		int currentIteration = 0;
		m_recordedConflict = CheckForConflicts();
		while (m_recordedConflict.Count > 0 && currentIteration++ < 20)
		{
			m_recordedConflict = ResolveConflicts();
		}

		foreach (RecordedAction actionInConflict in m_recordedConflict)
		{
			Debug.LogError("This action conflict cannot be resolved: " + actionInConflict.type);
			return;
		}

		foreach (int entityID in m_actionsToPlay.Keys)
		{
			foreach (RecordedAction recordedAction in m_actionsToPlay[entityID])
			{
				if (!recordedAction.action.IsPerformingAtTick(currentTick))
					continue;
				recordedAction.action.Prepare(recordedAction.entityState);
			}
		}

		//c)play this phases entities turn actions

		//make a pause here, send actions to all clients
		//AND only after that is done, start perform actions
		//and wait for all actions to be performed and server signaled by every clients
		//then server do EndPhase 
		if (!GameManager.Instance.IsOnline)
			PlayThisRoundActions();
		else if (GameManager.Instance.IsOnline)
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
				foreach (RecordedAction recordedAction in kvp.Value.ToArray())
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
			Queue<RecordedAction> actionsPlayedThisRound = m_actionsToPlay[entity];
			foreach (RecordedAction action in actionsPlayedThisRound.ToArray())
			{
				if (!action.action.IsPerformingAtTick(currentTick))
					continue;

				bool wasPairedWithAnotherAction = false;
				foreach (int otherEntity in m_actionsToPlay.Keys)
				{
					if (entity == otherEntity) continue;

					Queue<RecordedAction> otherEntityActionsPlayedThisRound = m_actionsToPlay[otherEntity];
					foreach (RecordedAction otherAction in otherEntityActionsPlayedThisRound.ToArray())
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

				//nobody to be paired with this tick: the action still has to validate and book its target tiles
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

				Queue<RecordedAction> otherEntityActionsPlayedThisRound = m_actionsToPlay[otherEntity];
				foreach (RecordedAction otherAction in otherEntityActionsPlayedThisRound.ToArray())
				{
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

	private void PlayActionTick ( RecordedAction _recordedAction )
	{
		if (_recordedAction.freeAction != null
			&& _recordedAction.freeActionType != EntityActionEnumID.Wait
			&& _recordedAction.freeActionType != EntityActionEnumID.Unknowned
			&& _recordedAction.action.IsPerformingAtTick(currentTick))
		{
			if (_recordedAction.freeAction.Data.isPlayedFirst)
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
				_recordedAction.action.onEndTick = ( performingEntity, didEndAction ) =>
				{
					_recordedAction.freeAction.onEndTick = OnActionEndTick;
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
			//performing entity still has actions this phase to do
			RecordedAction action = m_actionsToPlay[_performingEntityID].Dequeue();
			m_actionsBeingDone[_performingEntityID] = new(action, false);
			PlayActionTick(action);
		}
		else if (hasQueuedAction)
		{
			//next action starts on a later tick, it is kept queued instead of being burnt on this one
			Debug.LogWarning("entity " + _performingEntityID + " has " + m_actionsToPlay[_performingEntityID].Count
				+ " action(s) queued starting at tick " + m_actionsToPlay[_performingEntityID].Peek().action.timeAtStart
				+ " while current tick is " + currentTick);
			m_actionsBeingDone.Remove(_performingEntityID);
			TryEndRoundTick();
		}
		else
		{
			//no more action for this entity or still performing one
			if (_didEndAction)
			{
				m_actionsToPlay.Remove(_performingEntityID);
				m_actionsBeingDone.Remove(_performingEntityID);
			}
			else
			{
				if (!m_actionsBeingDone.ContainsKey(_performingEntityID))
				{
					//here
					Debug.Log("error here with entity " + _performingEntityID);
					LogConsole.AddLog("error here with entity " + _performingEntityID, LogConsole.LogEventType.ActionResolution);
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
		{
			Debug.Log("Server ended tick " + currentTick);
			return; //error is here
		}

		LogConsole.AddLog("Server ended tick " + currentTick, LogConsole.LogEventType.DebugSys);

		bool isGameFinished = IsGameFinished(out bool _playerOneWin, out bool _playerTwoWin);
		EndLevelPopup.GameResult result = _playerOneWin && _playerTwoWin ? EndLevelPopup.GameResult.Draw : (GameManager.Instance.PlayerID == 0 ? _playerTwoWin : _playerOneWin) ? EndLevelPopup.GameResult.Loose : EndLevelPopup.GameResult.Win;

		if (isGameFinished)
			EndTurn(true, result);
		else if (m_recordedActionInput.Keys.Count == 0 || currentTick >= GameConfig.current.game.actionTokenPerRound - 1)
			EndTurn(false, result); //end turn
		else
		{
			currentTick++;
			StartNextRoundTick(); //end this phase
		}
	}

	private void OnEntityDeath ( int _entityID )
	{
		GameManager.Instance.GetEntityFromID(_entityID).Equipment.onDeath -= OnEntityDeath;
		Entity deadEntity = GameManager.Instance.GetEntityFromID(_entityID);
		foreach (AEntityPassiveEffect.PassiveEffectContainer effetID in deadEntity.AllPassiveEffects)
		{
			GameAssets.current.game.entityEffects[effetID.enumID].OnDeathTrigger(deadEntity);
		}
		LogConsole.AddLog("Entity " + _entityID + " died", LogConsole.LogEventType.DebugSys);

		m_recordedActionInput.Remove(_entityID);
		m_actionsToPlay.Remove(_entityID);
		m_actionsBeingDone.Remove(_entityID);
	}

	private void EndTurn (bool _isGameFinished, EndLevelPopup.GameResult _result)
	{
		onEndPlayPhase?.Invoke();
		LogConsole.AddLog("EndRound", LogConsole.LogEventType.DebugSys);

		if (!GameManager.Instance.IsOnline)
		{
			if (_isGameFinished)
				EndLevel(_result);
			else
				StartInputPhase();
		}
		else
		{
			if (m_networkedTurnSystem.IsServer && !m_networkedTurnSystem.IsHost)
			{
				if (_isGameFinished)
					EndLevel(_result);
				else
					StartInputPhase();
			}
			m_networkedTurnSystem.EndRoundClientRPC(_isGameFinished, _result);
		}

	}

	private bool IsGameFinished ( out bool _playerOneWin, out bool _playerTwoWin )
	{
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
				//nothing else to check
				break;
			case MissionData.MissionType.DefenseDeZone:
				foreach (Tile zoneCenterTile in GameManager.Instance.PlayersEntityAnchor[0].Zones)
				{
					bool areAllEntityOfSameTeam1 = true;
					int teamID1 = -1;
					foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(zoneCenterTile, 0, GameConfig.current.game.zoneRange, true, false, false))
					{
						if (!tile.TryGetEntity(true, out Entity entity))
							continue;

						if (teamID1 != -1 && entity.OwnerID != teamID1)
						{
							areAllEntityOfSameTeam1 = false;
							break;
						}
						else
							teamID1 = entity.OwnerID;
					}
					if (areAllEntityOfSameTeam1)
						GameManager.Instance.AddPointsToPlayer(teamID1, 1);
				}
				if (GameManager.Instance.PlayersPoints[0] >= 5)
					_playerOneWin = true;
				if (GameManager.Instance.PlayersPoints[1] >= 5)
					_playerTwoWin = true;
				break;
			case MissionData.MissionType.ControleDePoint:

				bool areAllEntityOfSameTeam2 = true;
				int teamID2 = -1;
				foreach (Tile zoneCenterTile in GameManager.Instance.PlayersEntityAnchor[0].Zones)
				{
					foreach (Tile tile in GridManager.Instance.GetTilesInVisionRange(zoneCenterTile, 0, GameConfig.current.game.zoneRange, true, false, false))
					{
						if (!tile.TryGetEntity(true, out Entity entity))
							continue;

						if (teamID2 != -1 && entity.OwnerID != teamID2)
						{
							areAllEntityOfSameTeam2 = false;
							break;
						}
						else
							teamID2 = entity.OwnerID;
					}
					if (!areAllEntityOfSameTeam2)
						break;
				}
				if (areAllEntityOfSameTeam2)
				{
					if (teamID2 == 0)
						_playerOneWin = true;
					else
						_playerTwoWin = true;
				}
				break;
			case MissionData.MissionType.Construction:
				//TBD
				break;
			case MissionData.MissionType.Sabotage:
				bool areAllPlayerOneStructureDead = true;
				bool areAllPlayerTwoStructureDead = true;
				foreach (Tile structureTile in GameManager.Instance.PlayersEntityAnchor[0].Structures)
				{
					if (structureTile.Structure.Health > 0)
						areAllPlayerOneStructureDead = false;
				}
				foreach (Tile structureTile in GameManager.Instance.PlayersEntityAnchor[1].Structures)
				{
					if (structureTile.Structure.Health > 0)
						areAllPlayerTwoStructureDead = false;
				}
				if (areAllPlayerOneStructureDead)
					_playerTwoWin = true;
				if (areAllPlayerTwoStructureDead)
					_playerOneWin = true;

				break;
			case MissionData.MissionType.Assassinat:
				if (GameManager.Instance.PlayersEntityAnchor[0].King.Equipment.IsDead)
					_playerTwoWin = true;
				if (GameManager.Instance.PlayersEntityAnchor[1].King.Equipment.IsDead)
					_playerOneWin = true;

				break;
		}

		return _playerOneWin || _playerTwoWin;
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

		Entity.EntityState availableState = _entity.KnownedStates[0];

		for (int i = 1; i < remainingActionTickThisTurn; i++)
		{
			m_recordedActionInput[_entity.ID].Enqueue(new RecordedAction()
			{
				timeAtStart = currentTick + i,
				type = EntityActionEnumID.Wait,
				performingEntityID = _entity.ID,
				action = GetAction(EntityActionEnumID.Wait, _entity.ID, null, currentTick + i),
				freeAction = GetAction(EntityActionEnumID.Wait, _entity.ID, null, currentTick + i),
				freeActionType = EntityActionEnumID.Wait,
				entityState = availableState
			});
		}

		_onEndSpawn?.Invoke();
	}

	#endregion

}
