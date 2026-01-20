using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.Netcode;
using System.Linq;

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

	[SerializeField] private NetworkedTurnSystem m_networkedTurnSystem;

	[SerializeField] private SerializableDictionary<int, Queue<RecordedAction>> m_recordedActionInput = new(); //all actions this turn
	public SerializableDictionary<int, Queue<RecordedAction>> RecordedActions => m_recordedActionInput;
	[SerializeField] private SerializableDictionary<int, Queue<RecordedAction>> m_actionsToPlay = new(); //this phase action
	public SerializableDictionary<int, Queue<RecordedAction>> ActionsToPlay => m_actionsToPlay;
	private SerializableDictionary<int, RecordedAction> m_actionsBeingDone = new(); //current actions running
	private SerializableDictionary<int, int> m_remainingActionToken = new();
	public SerializableDictionary<int, int> RemainingActionToken => m_remainingActionToken;

	private List<System.Tuple<RecordedAction, RecordedAction>> m_recordedConflict;

	private AEntityAction m_currentEntityAction;
	public AEntityAction CurrentActionSelected => m_currentEntityAction;
	private EntityActionEnumID m_currentActionTypeSelected;
	public EntityActionEnumID CurrentActionTypeSelected => m_currentActionTypeSelected;

	private Entity.EntityState m_currentStateTypeSelected;
	public Entity.EntityState CurrentStateTypeSelected => m_currentStateTypeSelected;

	public enum TurnPhase { Recording, Calculating, Playing }
	public TurnPhase currentPhase;

	public struct RecordedAction : INetworkSerializable
	{
		public EntityActionEnumID type;
		public int performingEntityID;
		public AEntityAction action;
		public Entity.EntityState entityState;

		public void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
		{
			serializer.SerializeValue(ref type);
			serializer.SerializeValue(ref performingEntityID);

			if (serializer.IsWriter)
			{
				action.NetworkSerialize(serializer);
			}
			else
			{
				action = Instance.GetAction(type, performingEntityID);

				if(action == null)
				{
					Debug.LogError("ERROR : action is null when " + (serializer.IsWriter ? "writing" : "reading") + " with type " + type);
				}
				action.NetworkSerialize(serializer);
			}

			serializer.SerializeValue(ref entityState);
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
		PlayerController.onEntitySelected += OnEntitySelected;
	}

	private void OnDestroy ()
	{
		PlayerController.onEntitySelected -= OnEntitySelected;
	}

	private void OnEntitySelected ( int? _selectedEntity )
	{
		RefreshActionDisplay(_selectedEntity);
	}

	public void Init ()
	{
		foreach(EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				entity.Equipment.onDeath += OnEntityDeath;
			}
		}
	}

	#region Input phase

	public int GetLastRegisteredPositionOfEntity ( int _entityID )
	{
		if (m_recordedActionInput.ContainsKey(_entityID) == false 
			|| m_recordedActionInput[_entityID] == null || m_recordedActionInput[_entityID].Count == 0)
			return GameManager.Instance.GetEntityFromID(_entityID).Displacement.Coordinates.ID;

		RecordedAction lastRecordedAction = m_recordedActionInput[_entityID].ToArray()[^1];
		return lastRecordedAction.action.positionAtActionEndID;
	}

	public void SetCurrentStateSelected (Entity.EntityState _state )
	{
		m_currentStateTypeSelected = _state;
	}

	public void SetCurrentActionSelected ( EntityActionEnumID _action )
	{
		m_currentActionTypeSelected = _action;
		m_currentEntityAction = GetAction(_action, PlayerController.Instance.SelectedEntity.ID);

		onActionSelected?.Invoke(m_currentEntityAction);
	}

	public AEntityAction GetAction(EntityActionEnumID _actionType, int _performingEntityID )
	{
		AEntityAction action = null;

		switch (_actionType)
		{
			case EntityActionEnumID.NeighborMove:
				action = new MoveToNeighborAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.NeighborMove], _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID));
				break;
			case EntityActionEnumID.TargetTileMove:
				action = new MoveToTargetAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.TargetTileMove], _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID));
				break;
			case EntityActionEnumID.Attack:
				action = new AttackAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Attack], _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID));
				break;
			case EntityActionEnumID.Wait:
				action = new WaitAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.Wait], _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID));
				break;
			case EntityActionEnumID.RotateWeapon:
				action = new RotateWeaponAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionEnumID.RotateWeapon], _performingEntityID, GetLastRegisteredPositionOfEntity(_performingEntityID));
				break;
		}

		return action;
	}

	public bool AddAction (int _entityID, EntityActionEnumID _actionType, Entity.EntityState _state )
	{
		AEntityAction action = null;
		action = GetAction(_actionType, _entityID);
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
			type = _action.enumID,
			performingEntityID = _entityID,
			action = _action,
			entityState = _state
		};
		m_recordedActionInput[_entityID].Enqueue(recordedAction);

		m_remainingActionToken[_entityID]-= GameAssets.current.game.entityActionsData[_action.enumID].tokenCost;

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
		for(int i = actionQueue.Count - 1; i >= _recordedActionPositionInQueue; i--)
		{
			m_remainingActionToken[_actionToStartRemoveFrom.performingEntityID] += actionQueue[i].action.Data.tokenCost;
			actionQueue.RemoveAt(i);
		}
		//actionQueue.RemoveRange(_recordedActionPositionInQueue, actionQueue.Count - _recordedActionPositionInQueue);
		m_recordedActionInput[_actionToStartRemoveFrom.performingEntityID] = new Queue<RecordedAction>(actionQueue);
		onActionRemoved?.Invoke(_actionToStartRemoveFrom);

		RefreshActionDisplay(_actionToStartRemoveFrom.performingEntityID);
	}

	public void RemoveAction ( RecordedAction _removedRecordedAction )
	{
		if (!m_recordedActionInput.ContainsKey(_removedRecordedAction.performingEntityID))
			return;

		List<RecordedAction> actionQueue = m_recordedActionInput[_removedRecordedAction.performingEntityID].ToList();
		actionQueue.Remove(_removedRecordedAction);
		m_recordedActionInput[_removedRecordedAction.performingEntityID] = new Queue<RecordedAction>(actionQueue);
		onActionRemoved?.Invoke(_removedRecordedAction);

		RefreshActionDisplay(_removedRecordedAction.performingEntityID);
	}

	public void RefreshActionDisplay (int? _selectedEntityID)
	{
		PlayerController.Instance.ClearActionOnTileDisplay();
		PlayerController.Instance.ClearGhostActionOnTileDisplay();

		if (!_selectedEntityID.HasValue || !m_recordedActionInput.ContainsKey((int)_selectedEntityID))
			return;

		if(m_remainingActionToken[_selectedEntityID.Value] >= GameAssets.current.game.entityActionsData[m_currentActionTypeSelected].tokenCost)
			SetCurrentActionSelected(m_currentActionTypeSelected);

		// display all selected entity actions
		foreach (RecordedAction recordedAction in m_recordedActionInput[(int)_selectedEntityID].ToArray())
		{
			recordedAction.action.Display(recordedAction);
		}
	}

	[Button]
	public void StartInputPhase ()
	{
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
				m_remainingActionToken.Add(entity.ID, entity.Data.FrameData.actionTokenAmount);
			}
		}

		onStartInputPhase?.Invoke();

		if (GameManager.Instance.IsOnline && GameManager.Instance.Lobby.IsServer)
			NetworkTaskOrchestrator.Instance.LaunchClientTask("InputPhase", EndInputPhase);
	}

	[Button]
	public void EndInputPhase ()
	{
		SerializableDictionary<int, Queue<RecordedAction>> recordedActionInput = new(m_recordedActionInput);
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
		}

		//if( !GameManager.Instance.IsOnline)
		StartRound();
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
	public void StartRound ()
	{
		LogConsole.AddLog("Start round", LogConsole.LogEventType.Main);

		StartNextPhase();
	}

	private void StartNextPhase ()
	{
		LogConsole.AddLog("Start phase", LogConsole.LogEventType.Main);

		//1 - calculate phase

		//a)get all actions played by entities in one phase
		SerializableDictionary<int, Queue<RecordedAction>> recordedActions = new(m_recordedActionInput);
		m_actionsToPlay.Clear();
		foreach (int entityID in m_recordedActionInput.Keys)
		{
			Queue<RecordedAction> actionsPlayedThisRound = new();
			m_actionsToPlay.Add(entityID, actionsPlayedThisRound);
			int totalCost = 0;
			while (totalCost < 1 && recordedActions[entityID].Count > 0)
			{
				RecordedAction recordedAction = recordedActions[entityID].Dequeue();
				m_actionsToPlay[entityID].Enqueue(recordedAction);
				totalCost += recordedAction.action.cost;
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

				if (!resultInfo.isActionChanging)
				{
					LogConsole.AddLog("Succesfully add " + resultInfo.replacedAction + " action to queue", LogConsole.LogEventType.PlayPhase);
					recordedAction.action.Prepare(recordedAction.entityState);
					returnActionToPlayThisRound.Enqueue(recordedAction);
				}
				else
				{
					LogConsole.AddLog("Action replaced to " + resultInfo.replacedAction, LogConsole.LogEventType.PlayPhase);
					resultInfo.replacedAction.Prepare(recordedAction.entityState);
					returnActionToPlayThisRound.Enqueue(new RecordedAction() { type = resultInfo.replacedAction.enumID, performingEntityID = resultInfo.replacedAction.performingEntityID, action = resultInfo.replacedAction, entityState = recordedAction.entityState });
				}
			}

			m_actionsToPlay[entityID] = new(returnActionToPlayThisRound);
		}

		//2-recursively check for possible conflict and change actions if needed
		//	     => dealing with conflict can create new one
		m_recordedConflict = CheckForConflicts();
		while (m_recordedConflict.Count > 0)
		{
			m_recordedConflict = ResolveConflicts();
		}

		//c)play this phases entities turn actions

		//make a pause here, send actions to all clients
		//AND only after that is done, start perform actions
		//and wait for all actions to be performed and server signaled by every clients
		//then server do EndPhase 
		if (!GameManager.Instance.IsOnline)
			PlayThisPhaseActions();
		else if (GameManager.Instance.IsOnline)
		{
			NetworkTaskOrchestrator.Instance.LaunchClientTask("PlayPhase", EndPhase);
			List<RecordedEntityActionsContainer> actionsToSend = new();

			foreach (var kvp in m_actionsToPlay)
			{
				actionsToSend.Add(new RecordedEntityActionsContainer
				{
					entityId = kvp.Key,
					actions = kvp.Value.ToArray()
				});
				foreach(RecordedAction recordedAction in kvp.Value.ToArray())
					LogConsole.AddLog("Action sent: " + recordedAction.action.ToString(), LogConsole.LogEventType.InputPhase);
			}
			m_networkedTurnSystem.StartPlayPhaseClientRPC(actionsToSend.ToArray());
		}
	}

	public void PlayThisPhaseActions ()
	{
		currentPhase = TurnPhase.Playing;
		m_actionsBeingDone.Clear();
		List<int> entityIDs = new(m_actionsToPlay.Keys);
		foreach (int entityID in entityIDs)
		{
			GameManager.Instance.GetEntityFromID(entityID).OnPhaseStart();

			if (m_actionsToPlay.ContainsKey(entityID) && m_actionsToPlay[entityID] != null && m_actionsToPlay[entityID].Count != 0)
			{
				RecordedAction action = m_actionsToPlay[entityID].Dequeue();
				m_actionsBeingDone.Add(entityID, action);
				action.action.onEndPerform += OnActionEndPerform;
				//TODO : display action on cam one by one when in conflict
			}
		}

		foreach(RecordedAction recordedAction in m_actionsBeingDone.Values.ToArray())
		{
			LogConsole.AddLog("Action performed: " + recordedAction.action.ToString(), LogConsole.LogEventType.PlayPhase);
			recordedAction.action.OnStartPerform(recordedAction.entityState);
			recordedAction.action.Perform(recordedAction.entityState);
		}
	}


	private List<System.Tuple<RecordedAction, RecordedAction>> CheckForConflicts ()
	{
		List<System.Tuple<RecordedAction, RecordedAction>> conflicts = new();
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
						if (action.action.CheckConflict(otherAction.action))
						{
							LogConsole.AddLog("Conflict detected: [" + action.action.ToString() + "|" + action.action.ToString() + "]", LogConsole.LogEventType.PlayPhase);
							conflicts.Add(new System.Tuple<RecordedAction, RecordedAction>(action, otherAction));
						}
					}
				}
			}
		}

		return conflicts;
	}

	private List<System.Tuple<RecordedAction, RecordedAction>> ResolveConflicts ()
	{
		List<System.Tuple<RecordedAction, RecordedAction>> remainingConflict = new();
		foreach (System.Tuple<RecordedAction, RecordedAction> conflict in m_recordedConflict)
		{
			if (conflict.Item1.action.CheckConflict(conflict.Item2.action, false))
				remainingConflict.Add(conflict);
		}

		return remainingConflict;
	}

	private void OnActionEndPerform ( int _performingEntityID )
	{
		if (m_actionsToPlay.ContainsKey(_performingEntityID) && m_actionsToPlay[_performingEntityID].Count > 0)
		{
			//performing entity still has actions this phase to do
			RecordedAction action = m_actionsToPlay[_performingEntityID].Dequeue();
			LogConsole.AddLog("Action performed: " + action.action.ToString(), LogConsole.LogEventType.PlayPhase);
			m_actionsBeingDone[_performingEntityID] = action;
			action.action.onEndPerform += OnActionEndPerform;
			action.action.OnStartPerform(action.entityState);
			action.action.Perform(action.entityState);
		}
		else
		{
			//no more action for thos entity
			m_actionsBeingDone.Remove(_performingEntityID);
			if (m_actionsBeingDone.Keys.Count == 0)
			{
				m_actionsToPlay.Clear();

				if (!GameManager.Instance.IsOnline)
				{
					EndPhase();
				}
				else
				{
					LogConsole.AddLog("Client ended phase", LogConsole.LogEventType.PlayPhase);
					NetworkTaskOrchestrator.Instance.NotifyTaskEndToServerRPC("PlayPhase");
				}
			}
		}

	}

	private void EndPhase ()
	{
		LogConsole.AddLog("Server ended phase", LogConsole.LogEventType.PlayPhase);
		if (m_recordedActionInput.Keys.Count == 0)
			EndRound(); //end turn
		else
			StartNextPhase(); //end this phase
	}

	private void OnEntityDeath(int _entityID )
	{
		GameManager.Instance.GetEntityFromID(_entityID).Equipment.onDeath -= OnEntityDeath;

		m_recordedActionInput.Remove(_entityID);
	}

	private void EndRound ()
	{
		LogConsole.AddLog("EndRound", LogConsole.LogEventType.Main);

		//check if finish level condition (all enemy killed || all ally killed)
		GameManager.Instance.LevelCompletionCheck(out bool _isPlayerOneDead, out bool _isPlayerTwoDead);
		if(!GameManager.Instance.IsOnline)
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
			m_networkedTurnSystem.EndRoundClientRPC( _isPlayerOneDead, _isPlayerTwoDead);
		}

	}

	public void EndLevel (bool _isSuccess)
	{
		if (_isSuccess)
			LogConsole.AddLog("Victory", LogConsole.LogEventType.Main);
		else
			LogConsole.AddLog("Defeat", LogConsole.LogEventType.Main);

		GameManager.Instance.EndGame();
	}


	#endregion

}
