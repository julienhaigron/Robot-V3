using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;
using System.Linq;
using System;

//1) record robots action in order for a single turn

//2) calculate conflict

//3) resolve action conflict (two entity actions happening at the same time and involving the same entities
//   => store result in a "final" action list

//4) play final action list

public class TurnManager : Singleton<TurnManager>
{
	public static System.Action<RecordedAction> onActionAdded;
	public static System.Action<RecordedAction> onActionRemoved;
	public static System.Action<AEntityAction> onActionSelected;
	public static System.Action onStartInputPhase;
	public static System.Action onEndInputPhase;
	public static System.Action onNewRoundStart;
	public static System.Action onEndLevel;

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

	private Entity.EntityState m_currentStateTypeSelected;
	public Entity.EntityState CurrentStateTypeSelected => m_currentStateTypeSelected;

	public enum TurnPhase { Recording, Calculating, Playing, Off }
	public TurnPhase currentPhase = TurnPhase.Off;
	public int currentTick = 0;

	//prevision
	private Dictionary<int, TrackedEntityEvents> m_trackedEventsPerEntity = new();
	public Dictionary<int, TrackedEntityEvents> TrackedEventsPerEntity => m_trackedEventsPerEntity;
	[Serializable]
	public class TrackedEntityEvents
	{
		public int firstTimeEntityMoved;
		public int firstTimeEntityAttacked;

		public void ResetAllValues ()
		{
			firstTimeEntityMoved = -1;
			firstTimeEntityAttacked = -1;
		}
	}

	private List<InPlayEvent> m_inPlayEventBeingDone = new();
	public class InPlayEvent
	{
		public Action<InPlayEvent> onEventFinished;

		public void EndEvent ()
		{
			onEventFinished?.Invoke(this);
		}
	}

	public struct RecordedAction : INetworkSerializable
	{
		public int timeAtStart;
		public int performingEntityID;
		public Entity.EntityState entityState;
		public EntityActionEnumID type;
		public AEntityAction action;
		public EntityActionEnumID freeActionType;
		public AEntityAction freeAction;

		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref timeAtStart);
			serializer.SerializeValue(ref performingEntityID);
			serializer.SerializeValue(ref entityState);
			serializer.SerializeValue(ref type);

			if (serializer.IsWriter)
			{
				action.NetworkSerialize(serializer);
			}
			else
			{
				action = Instance.GetAction(GameAssets.current.game.entityActionsData[type], performingEntityID, timeAtStart);

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
				freeAction = Instance.GetAction(GameAssets.current.game.entityActionsData[freeActionType], performingEntityID, timeAtStart);

				if (freeAction == null)
				{
					Debug.LogError("ERROR : freeAction is null when " + (serializer.IsWriter ? "writing" : "reading") + " with type " + type);
				}
				freeAction.NetworkSerialize(serializer);
			}
		}

		public void AddActionMod(AEntityAction _actionMod )
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
	}

	private void OnDestroy ()
	{
		EntityAnchor.onEntityAdded -= OnEntityAdded;
		PlayerController.onEntitySelected -= OnEntitySelected;
	}

	private void OnEntitySelected ( int? _selectedEntity )
	{
		if (_selectedEntity.HasValue)
		{
			Entity selectedEntity = GameManager.Instance.GetEntityFromID(_selectedEntity.Value);
			SetCurrentActionSelected(selectedEntity.KnownedActions[0]);
			SetCurrentStateSelected(selectedEntity.KnownedStates[0]);
		}
		RefreshActionDisplay(_selectedEntity);
	}

	public void OnEntityAdded ( Entity _entity )
	{
		_entity.Equipment.onDeath += OnEntityDeath;
		m_trackedEventsPerEntity.Add(_entity.ID, new TrackedEntityEvents()
		{
			firstTimeEntityMoved = -1,
			firstTimeEntityAttacked = -1
		});
	}

	public void Init ()
	{
		m_trackedEventsPerEntity.Clear();
	}

	#region Input phase

	public void SetCurrentStateSelected ( Entity.EntityState _state )
	{
		m_currentStateTypeSelected = _state;
	}

	public void SetCurrentActionSelected ( EntityActionEnumID _action )
	{
		int performingEntityID = PlayerController.Instance.SelectedEntity.ID;
		int timeAtStart = m_recordedActionInput.ContainsKey(performingEntityID) && m_recordedActionInput[performingEntityID].Count > 0
			? m_recordedActionInput[performingEntityID].ToArray()[^1].action.TimeAtEnd : currentTick;
		
		m_currentEntityAction = GetAction(GameAssets.current.game.entityActionsData[_action], performingEntityID, timeAtStart);
		m_currentActionTypeSelected = _action;

		onActionSelected?.Invoke(m_currentEntityAction);
	}

	public AEntityAction GetAction ( EntityActionEnumID _actionType, int _performingEntityID, int _timeAtStart )
	{
		return GetAction(GameAssets.current.game.entityActionsData[_actionType], _performingEntityID, _timeAtStart);
	}

	public AEntityAction GetAction ( EntityActionData _actionData, int _performingEntityID, int _timeAtStart )
	{
		AEntityAction action = null;

		//for base actions or exceptions
		switch (_actionData.codeType)
		{
			case EntityActionData.ActionCodeType.NeighborMove:
				action = new MoveToNeighborAction();
				break;
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
			default:
				Debug.LogError("Missing entree in TurnManager.GetAction for type \"" + _actionData.codeType + "\"");
				return action;
		}
		action.Init(GameAssets.current.game.entityActionsData[_actionData.enumID], _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID), _timeAtStart);

		return action;
	}

	public bool AddAction ( int _entityID, EntityActionEnumID _actionType, Entity.EntityState _state )
	{
		AEntityAction action = null;
		int timeAtStart = m_recordedActionInput.ContainsKey(_entityID) && m_recordedActionInput[_entityID].Count > 0
			? m_recordedActionInput[_entityID].ToArray()[^1].action.TimeAtEnd : currentTick;

		action = GetAction(GameAssets.current.game.entityActionsData[_actionType], _entityID, timeAtStart);
		return AddAction(_entityID, action, _state);
	}

	public bool AddAction ( int _entityID, AEntityAction _action, Entity.EntityState _state )
	{
		if (m_recordedActionInput.ContainsKey(_entityID) == false)
			m_recordedActionInput.Add(_entityID, new());

		if (m_remainingActionToken[_entityID] <= 0)
			return false;

		RecordedAction recordedAction = new RecordedAction
		{
			timeAtStart = _action.timeAtStart,
			type = _action.enumID,
			performingEntityID = _entityID,
			action = _action,
			entityState = _state,
			freeAction = new WaitAction(), //default is wait action
			freeActionType = EntityActionEnumID.Wait
		};
		m_recordedActionInput[_entityID].Enqueue(recordedAction);


		m_remainingActionToken[_entityID] -= GameAssets.current.game.entityActionsData[_action.enumID].GetTokenTotalCost(_action, GameManager.Instance.GetEntityFromID(_entityID), null);

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
			m_remainingActionToken[_actionToStartRemoveFrom.performingEntityID] += actionQueue[i].action.TotalCost;
			actionQueue.RemoveAt(i);
		}
		//actionQueue.RemoveRange(_recordedActionPositionInQueue, actionQueue.Count - _recordedActionPositionInQueue);
		m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID] = new Queue<RecordedAction>(actionQueue);
		onActionRemoved?.Invoke(_actionToStartRemoveFrom);

		TrackedEventCheck();
		RefreshActionDisplay(_actionToStartRemoveFrom.performingEntityID);
	}

	/*public void RemoveAction ( RecordedAction _removedRecordedAction )
	{
		if (!m_recordedActionInput.ContainsKey(_removedRecordedAction.performingEntityID))
			return;

		List<RecordedAction> actionQueue = m_recordedActionInput[_removedRecordedAction.performingEntityID].ToList();
		actionQueue.Remove(_removedRecordedAction);
		m_recordedActionInput[_removedRecordedAction.performingEntityID] = new Queue<RecordedAction>(actionQueue);
		onActionRemoved?.Invoke(_removedRecordedAction);

		TrackedEventCheck();
		RefreshActionDisplay(_removedRecordedAction.performingEntityID);
	}*/

	public int GetLastRegisteredPositionOfEntity ( int _entityID )
	{
		if (m_recordedActionInput.ContainsKey(_entityID) == false
			|| m_recordedActionInput[_entityID] == null || m_recordedActionInput[_entityID].Count == 0)
			return GameManager.Instance.GetEntityFromID(_entityID).Displacement.Coordinates.ID;

		RecordedAction lastRecordedAction = m_recordedActionInput[_entityID].ToArray()[^1];
		return lastRecordedAction.action.positionAtActionEndID;
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
					pair.Value.firstTimeEntityMoved = recordedAction.action.timeAtStart;
				if (recordedAction.action.Data.type == EntityActionData.ActionType.DistanceAttack || recordedAction.action.Data.type == EntityActionData.ActionType.MeleeAttack)
					pair.Value.firstTimeEntityAttacked = recordedAction.action.timeAtStart;
			}
		}
	}

	public void RefreshActionDisplay ( int? _selectedEntityID, int _specificTokenCount = -1 )
	{
		PlayerController.Instance.ClearActionOnTileDisplay();
		PlayerController.Instance.ClearGhostActionOnTileDisplay();
		PlayerController.Instance.ClearGhostEntities();


		if (_selectedEntityID.HasValue && m_recordedActionInput.ContainsKey(_selectedEntityID.Value)
			&& m_remainingActionToken[_selectedEntityID.Value] >= GameAssets.current.game.entityActionsData[m_currentActionTypeSelected].GetTokenTotalCost(m_currentEntityAction, GameManager.Instance.GetEntityFromID(_selectedEntityID.Value), null))
			SetCurrentActionSelected(m_currentActionTypeSelected);

		// display all player entity actions
		foreach (int entityID in m_recordedActionInput.Keys)
		{
			int totalCost = 0;
			Entity entity = GameManager.Instance.GetEntityFromID(entityID);
			Tile lastRecordedPosition = entity.Displacement.Coordinates.GetTile();
			int lastRecordedOrientation = entity.Displacement.CurrentOrientation;

			foreach (RecordedAction recordedAction in m_recordedActionInput[entityID].ToArray())
			{
				totalCost += recordedAction.action.TotalCost;
				recordedAction.action.Display(recordedAction);

				if (_specificTokenCount != -1 && totalCost <= _specificTokenCount)
				{
					lastRecordedPosition = GridManager.Instance.Tiles[recordedAction.action.positionAtActionEndID];
					if (recordedAction.action.enumID == EntityActionEnumID.RotateEntity)
						lastRecordedOrientation = (recordedAction.action as RotateEntityAction).targetedOrientationID;
					else if (recordedAction.freeAction != null && recordedAction.freeAction.enumID == EntityActionEnumID.RotateEntity)
						lastRecordedOrientation = (recordedAction.freeAction as RotateEntityAction).targetedOrientationID;
				}
			}

			if (_selectedEntityID.HasValue)
				PlayerController.Instance.AddGhostAt(entity, lastRecordedPosition, lastRecordedOrientation);
		}
	}

	[Button]
	public void StartInputPhase ()
	{
		currentTick = 0;
		currentPhase = TurnPhase.Recording;
		//UIManager.Instance.OpenPanel<InGamePanel>();
		LogConsole.AddLog("Start Input phase", LogConsole.LogEventType.Main);

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

		if (GameManager.Instance.IsOnline && GameManager.Instance.Lobby.IsServer)
			NetworkTaskOrchestrator.Instance.LaunchClientTask("InputPhase", EndInputPhase);
	}

	[Button]
	public void EndInputPhase ()
	{
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
		LogConsole.AddLog("Start turn", LogConsole.LogEventType.Main);
		m_actionsToPlay.Clear();
		m_actionsBeingDone.Clear();
		currentTick = 0;

		StartNextRoundTick();
	}

	private void StartNextRoundTick ()
	{
		currentTick++;
		LogConsole.AddLog("Start tick", LogConsole.LogEventType.Main);

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
				m_actionsToPlay[entityID].Enqueue(recordedAction);
				totalCost += recordedAction.action.TotalCost;
			}

			if (m_recordedActionInput[entityID].Count == 0)
			{
				recordedActions.Remove(entityID);
				continue;
			}
		}
		m_recordedActionInput = new(recordedActions);

		currentPhase = TurnPhase.Calculating;
		GridManager.Instance.StartNewPhase();

		//1- register action (like movement in grid)
		//   => checks at this moment if action changes in another
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

				if (recordedAction.action.lifetime > 0 || !resultInfo.isActionChanging)
				{
					if (recordedAction.action.lifetime >= recordedAction.action.TimeAtStartPerform 
						&& recordedAction.action.lifetime < recordedAction.action.TimeAtStartPerform + recordedAction.action.actualDuration)
					{
						recordedAction.action.Prepare(recordedAction.entityState);
						LogConsole.AddLog("Succesfully add " + resultInfo.replacedAction + " action to queue", LogConsole.LogEventType.PlayPhase);
					}
					returnActionToPlayThisRound.Enqueue(recordedAction);
				}
				else
				{
					if (recordedAction.action.lifetime >= recordedAction.action.TimeAtStartPerform
						&& recordedAction.action.lifetime < recordedAction.action.TimeAtStartPerform + recordedAction.action.actualDuration)
					{
						resultInfo.replacedFreeAction.OnModActionAdded(resultInfo.replacedAction);
						resultInfo.replacedAction.Prepare(recordedAction.entityState);
						LogConsole.AddLog("Action replaced to " + resultInfo.replacedAction, LogConsole.LogEventType.PlayPhase);
					}
					returnActionToPlayThisRound.Enqueue(new RecordedAction()
					{
						timeAtStart = recordedAction.action.timeAtStart,
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

		foreach(RecordedAction actionInConflict in m_recordedConflict)
		{
			Debug.LogError("This action conflict cannot be resolved: " + actionInConflict.type);
			return;
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
				foreach (int otherEntity in m_actionsToPlay.Keys)
				{
					if (entity == otherEntity) continue;

					Queue<RecordedAction> otherEntityActionsPlayedThisRound = m_actionsToPlay[otherEntity];
					foreach (RecordedAction otherAction in otherEntityActionsPlayedThisRound.ToArray())
					{
						AEntityAction.ActionConflictResultInfo resultInfo = action.action.CheckConflict(otherAction.action);
						if (resultInfo.isFirstActionConflicted)
						{
							LogConsole.AddLog("Conflict detected: [" + action.action.ToString() + "]", LogConsole.LogEventType.PlayPhase);
							conflicts.Add(action);
						}
						else if (resultInfo.isSecondActionConflicted)
						{
							LogConsole.AddLog("Conflict detected: [" + otherAction.action.ToString() + "]", LogConsole.LogEventType.PlayPhase);
							conflicts.Add(otherAction);
						}
					}
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
			foreach (int otherEntity in m_actionsToPlay.Keys)
			{
				if (conflictedAction.performingEntityID == otherEntity) continue;

				Queue<RecordedAction> otherEntityActionsPlayedThisRound = m_actionsToPlay[otherEntity];
				foreach (RecordedAction otherAction in otherEntityActionsPlayedThisRound.ToArray())
				{
					AEntityAction.ActionConflictResultInfo resultInfo = conflictedAction.action.CheckConflict(otherAction.action, false);
					if (resultInfo.isFirstActionConflicted)
					{
						LogConsole.AddLog("Conflict detected: [" + conflictedAction.action.ToString() + "]", LogConsole.LogEventType.PlayPhase);
						remainingConflict.Add(conflictedAction);
					}
					else if (resultInfo.isSecondActionConflicted)
					{
						LogConsole.AddLog("Conflict detected: [" + otherAction.action.ToString() + "]", LogConsole.LogEventType.PlayPhase);
						remainingConflict.Add(otherAction);
					}
				}
			}
		}

		return remainingConflict;
	}

	public void PlayThisRoundActions ()
	{
		onNewRoundStart?.Invoke();
		currentPhase = TurnPhase.Playing;
		//m_actionsBeingDone.Clear();
		List<int> entityIDs = new(m_actionsToPlay.Keys);
		foreach (int entityID in entityIDs)
		{
			//GameManager.Instance.GetEntityFromID(entityID).OnPhaseStart();

			if (m_actionsBeingDone.ContainsKey(entityID))
				continue;

			if (m_actionsToPlay.ContainsKey(entityID) && m_actionsToPlay[entityID] != null && m_actionsToPlay[entityID].Count != 0)
			{
				RecordedAction action = m_actionsToPlay[entityID].Dequeue();
				m_actionsBeingDone.Add(entityID, new(action, false));
			}
		}

		foreach (Tuple<RecordedAction, bool> tuple in m_actionsBeingDone.Values.ToArray())
		{
			PlayActionTick(tuple.Item1);
		}
	}

	private void PlayActionTick ( RecordedAction _recordedAction )
	{
		if (_recordedAction.freeActionType != EntityActionEnumID.Wait
			&& _recordedAction.freeActionType != EntityActionEnumID.Unknowned)
		{
			_recordedAction.action.onEndTick += ( performingEntity, didEndAction ) =>
			{
				_recordedAction.freeAction.onEndTick += OnActionEndTick;
				_recordedAction.freeAction.PerformTick(_recordedAction.entityState);
			};

		}
		else
			_recordedAction.action.onEndTick += OnActionEndTick;

		LogConsole.AddLog("Action performed: " + _recordedAction.action.ToString(), LogConsole.LogEventType.PlayPhase);
		_recordedAction.action.PerformTick(_recordedAction.entityState);
	}

	private void OnActionEndTick ( int _performingEntityID, bool _didEndAction )
	{
		if (_didEndAction && m_actionsToPlay.ContainsKey(_performingEntityID) && m_actionsToPlay[_performingEntityID].Count > 0)
		{
			//performing entity still has actions this phase to do
			RecordedAction action = m_actionsToPlay[_performingEntityID].Dequeue();
			m_actionsBeingDone[_performingEntityID] = new(action, false);
			PlayActionTick(action);
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
				m_actionsBeingDone[_performingEntityID] = new(m_actionsBeingDone[_performingEntityID].Item1, true);

			TryEndRoundTick();
		}

	}

	private void TryEndRoundTick ()
	{
		bool areAllActionPerformed = true;
		foreach (int playerID in m_actionsBeingDone.Keys)
		{
			if(m_actionsBeingDone[playerID].Item2 == false)
				areAllActionPerformed = false;
		}

		if (areAllActionPerformed && m_inPlayEventBeingDone.Count == 0)
		{
			//m_actionsToPlay.Clear();

			if (!GameManager.Instance.IsOnline)
			{
				EndRoundTick();
			}
			else
			{
				LogConsole.AddLog("Client ended tick", LogConsole.LogEventType.PlayPhase);
				NetworkTaskOrchestrator.Instance.NotifyTaskEndToServerRPC("PlayPhase");
			}
		}
	}

	public void AddGameEvent ( InPlayEvent _newGameEvent )
	{
		_newGameEvent.onEventFinished += OnGameEventEnded;
		m_inPlayEventBeingDone.Add(_newGameEvent);
	}

	private void OnGameEventEnded ( InPlayEvent _event )
	{
		m_inPlayEventBeingDone.Remove(_event);
		TryEndRoundTick();
	}

	private void EndRoundTick ()
	{
		LogConsole.AddLog("Server ended tick", LogConsole.LogEventType.PlayPhase);
		if (m_recordedActionInput.Keys.Count == 0 || currentTick >= GameConfig.current.game.actionTokenPerRound)
			EndTurn(); //end turn
		else
			StartNextRoundTick(); //end this phase
	}

	private void OnEntityDeath ( int _entityID )
	{
		GameManager.Instance.GetEntityFromID(_entityID).Equipment.onDeath -= OnEntityDeath;
		Entity deadEntity = GameManager.Instance.GetEntityFromID(_entityID);
		foreach(AEntityPassiveEffect.PassiveEffectContainer effetID in deadEntity.AllPassiveEffects)
		{
			GameAssets.current.game.entityEffects[effetID.enumID].OnDeathTrigger(deadEntity);
		}

		m_recordedActionInput.Remove(_entityID);
		m_actionsToPlay.Remove(_entityID);
		m_actionsBeingDone.Remove(_entityID);
	}

	private void EndTurn ()
	{
		LogConsole.AddLog("EndRound", LogConsole.LogEventType.Main);

		//check if finish level condition (all enemy killed || all ally killed)
		GameManager.Instance.LevelCompletionCheck(out bool _isPlayerOneDead, out bool _isPlayerTwoDead);
		if (!GameManager.Instance.IsOnline)
		{
			if (_isPlayerOneDead || _isPlayerTwoDead)
			{
				EndLevel(!_isPlayerOneDead);
			}
			else
			{
				StartInputPhase();
			}
		}
		else
		{
			if (m_networkedTurnSystem.IsServer && !m_networkedTurnSystem.IsHost)
			{
				if (_isPlayerOneDead || _isPlayerTwoDead)
				{
					EndLevel(!_isPlayerOneDead && OnlinePlayerInstance.Self.IsHost);
				}
				else
				{
					StartInputPhase();
				}
			}
			m_networkedTurnSystem.EndRoundClientRPC(_isPlayerOneDead, _isPlayerTwoDead);
		}

	}

	public void EndLevel ( bool _isSuccess )
	{
		if (currentPhase == TurnPhase.Off)
			return;

		currentPhase = TurnPhase.Off;
		GameManager.Instance.EndGame(_isSuccess);
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
				action = GetAction(EntityActionEnumID.Wait, _entity.ID, currentTick + i),
				freeAction = GetAction(EntityActionEnumID.Wait, _entity.ID, currentTick + i),
				freeActionType = EntityActionEnumID.Wait,
				entityState = availableState
			});
		}

		_onEndSpawn?.Invoke();
	}

	#endregion

}
