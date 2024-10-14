using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//TODO :

//1) record robots action in order for a single turn

//2) calculate conflict

//3) resolve action conflict (two entity actions happening at the same time and involving the same entities
//   => store result in a "final" action list

//4) play final action list

public class TurnManager : Singleton<TurnManager>
{
	public static System.Action<AEntityAction> onActionAdded;
	public static System.Action<AEntityAction> onActionSelected;
	public static System.Action onEndInputPhase;

	[SerializeField] private SerializableDictionary<Entity, Queue<RecordedAction>> m_recordedActionInput = new(); //all actions this turn
	public SerializableDictionary<Entity, Queue<RecordedAction>> RecordedActions => m_recordedActionInput;
	[SerializeField] private SerializableDictionary<Entity, Queue<RecordedAction>> m_actionsToPlay = new(); //this phase action
	private SerializableDictionary<Entity, RecordedAction> m_actionsBeingDone = new(); //current actions running
	private SerializableDictionary<Entity, int> m_remainingActionToken = new();
	public SerializableDictionary<Entity, int> RemainingActionToken => m_remainingActionToken;

	private List<System.Tuple<RecordedAction, RecordedAction>> m_recordedConflict;

	private AEntityAction m_currentEntityAction;
	public AEntityAction CurrentActionSelected => m_currentEntityAction;
	private EntityActionType m_currentActionTypeSelected;
	public EntityActionType CurrentActionTypeSelected => m_currentActionTypeSelected;

	private Entity.EntityState m_currentStateTypeSelected;
	public Entity.EntityState CurrentStateTypeSelected => m_currentStateTypeSelected;

	public enum TurnPhase { Recording, Calculating, Playing }
	public TurnPhase currentPhase;

	public struct RecordedAction
	{
		public AEntityAction action;
		public Entity.EntityState entityState;
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

	private void OnEntitySelected ( Entity _selectedEntity )
	{
		RefreshActionDisplay(_selectedEntity);
	}

	public void Init ()
	{
		foreach (Entity enemy in GameManager.Instance.EnnemiEntityAnchor.Entities)
		{
			enemy.Equipment.onDeath += OnEntityDeath;
		}

		foreach (Entity ally in GameManager.Instance.PlayerEntitiesAnchor.Entities)
		{
			ally.Equipment.onDeath += OnEntityDeath;
		}
	}

	#region Input phase

	public Tile GetLastRegisteredPositionOfEntity ( Entity _entity )
	{
		if (m_recordedActionInput.ContainsKey(_entity) == false)
			return _entity.Displacement.Coordinates.GetTile();

		RecordedAction lastRecordedAction = m_recordedActionInput[_entity].ToArray()[^1];
		return lastRecordedAction.action.positionAtActionEnd;
	}

	public void SetCurrentStateSelected (Entity.EntityState _state )
	{
		m_currentStateTypeSelected = _state;
	}

	public void SetCurrentActionSelected ( EntityActionType _action )
	{
		m_currentActionTypeSelected = _action;
		m_currentEntityAction = GetAction(_action, PlayerController.Instance.SelectedEntity);

		onActionSelected?.Invoke(m_currentEntityAction);
	}

	private AEntityAction GetAction(EntityActionType _actionType, Entity _performingEntity )
	{
		AEntityAction action = null;

		switch (_actionType)
		{
			case EntityActionType.NeighborMove:
				action = new MoveToNeighborAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionType.NeighborMove], _performingEntity, GetLastRegisteredPositionOfEntity(_performingEntity));
				break;
			case EntityActionType.TargetTileMove:
				action = new MoveToTargetAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionType.TargetTileMove], _performingEntity, GetLastRegisteredPositionOfEntity(_performingEntity));
				break;
			case EntityActionType.Attack:
				action = new AttackAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionType.Attack], _performingEntity, GetLastRegisteredPositionOfEntity(_performingEntity));
				break;
			case EntityActionType.Wait:
				action = new WaitAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionType.Wait], _performingEntity, GetLastRegisteredPositionOfEntity(_performingEntity));
				break;
			case EntityActionType.RotateWeapon:
				action = new RotateWeaponAction();
				action.Init(GameAssets.current.game.entityActionsData[EntityActionType.RotateWeapon], _performingEntity, GetLastRegisteredPositionOfEntity(_performingEntity));
				break;
		}

		return action;
	}

	public bool AddAction (Entity _entity, EntityActionType _actionType )
	{
		AEntityAction action = null;
		action = GetAction(_actionType, _entity);
		return AddAction(_entity, action);
	}

	public bool AddAction ( Entity _entity, AEntityAction _action )
	{
		if (m_recordedActionInput.ContainsKey(_entity) == false)
			m_recordedActionInput.Add(_entity, new());

		if (m_remainingActionToken[_entity] <= 0)
			return false;

		m_recordedActionInput[_entity].Enqueue(new RecordedAction
		{
			action = _action,
			entityState = _entity.State
		});

		m_remainingActionToken[_entity]--;

		//Update action display on grid + UI
		onActionAdded?.Invoke(_action);
		return true;
	}

	public void RefreshActionDisplay (Entity _selectedEntity)
	{
		PlayerController.Instance.ClearArrows();

		if (_selectedEntity == null || !m_recordedActionInput.ContainsKey(_selectedEntity))
			return;

		//1) pop ghost if no ghost
		//else: refresh ghost position

		//2) display all selected entity actions
		foreach (RecordedAction recordedAction in m_recordedActionInput[_selectedEntity].ToArray())
		{
			recordedAction.action.Display();
		}
	}

	[Button]
	public void StartInputPhase ()
	{
		currentPhase = TurnPhase.Recording;
		//UIManager.Instance.OpenPanel<InGamePanel>();

		//reset RemainingActionToken
		m_remainingActionToken.Clear();
		foreach (Entity entity in GameManager.Instance.PlayerEntitiesAnchor.Entities)
		{
			m_remainingActionToken.Add(entity, entity.Data.actionTokenAmount);
		}

		foreach (Entity entity in GameManager.Instance.EnnemiEntityAnchor.Entities)
		{
			m_remainingActionToken.Add(entity, entity.Data.actionTokenAmount);
		}
	}

	[Button]
	public void EndInputPhase ()
	{
		BotEnnemiPlayer.Instance.InputPhase();

		onEndInputPhase?.Invoke();

		SerializableDictionary<Entity, Queue<RecordedAction>> recordedActionInput = new(m_recordedActionInput);
		m_recordedActionInput.Clear();
		foreach (Entity entity in recordedActionInput.Keys)
		{
			m_recordedActionInput.Add(entity, new Queue<RecordedAction>());

			foreach (RecordedAction record in recordedActionInput[entity])
			{
				m_recordedActionInput[entity].Enqueue(record);
				if (record.action.cost > 1)
				{
					//add wait tile for each actions in queue
					for (int i = 0; i < record.action.cost - 1; i++)
					{
						m_recordedActionInput[entity].Enqueue(new RecordedAction
						{
							action = new WaitAction(),
							entityState = record.entityState
						});
					}
				}

			}
		}

		StartRound();
	}

	#endregion

	#region Play phase

	[Button]
	private void StartRound ()
	{
		Debug.Log("StartRound");

		/*//1 - calculate phase

		//a)get all actions played by entities in one phase
		SerializableDictionary<Entity, Queue<RecordedAction>> recordedActions = new(m_recordedActionInput);
		m_actionsToPlay.Clear();
		foreach (Entity entity in recordedActions.Keys)
		{
			Queue<RecordedAction> actionsPlayedThisRound = new();
			m_actionsToPlay.Add(entity, actionsPlayedThisRound);
			int totalCost = 0;
			while (totalCost < 1 && recordedActions[entity].Count > 0)
			{
				RecordedAction recordedAction = recordedActions[entity].Dequeue();
				m_actionsToPlay[entity].Enqueue(recordedAction);
				totalCost += recordedAction.action.cost;
			}
		}
		m_recordedActionInput = new(recordedActions);*/

		StartNextPhase();
	}

	private void StartNextPhase ()
	{
		Debug.Log("StartNextPhase");

		//1 - calculate phase

		//a)get all actions played by entities in one phase
		SerializableDictionary<Entity, Queue<RecordedAction>> recordedActions = new(m_recordedActionInput);
		m_actionsToPlay.Clear();
		foreach (Entity entity in m_recordedActionInput.Keys)
		{
			Queue<RecordedAction> actionsPlayedThisRound = new();
			m_actionsToPlay.Add(entity, actionsPlayedThisRound);
			int totalCost = 0;
			while (totalCost < 1 && recordedActions[entity].Count > 0)
			{
				RecordedAction recordedAction = recordedActions[entity].Dequeue();
				m_actionsToPlay[entity].Enqueue(recordedAction);
				totalCost += recordedAction.action.cost;
			}

			if (m_recordedActionInput[entity].Count == 0)
			{
				recordedActions.Remove(entity);
				continue;
			}
		}
		m_recordedActionInput = new(recordedActions);

		currentPhase = TurnPhase.Calculating;
		GridManager.Instance.StartNewPhase();

		//1- register action (like movement in grid)
		//   => checks at this moment if action changes in another
		//
		List<Entity> entities = new(m_actionsToPlay.Keys);

		foreach (Entity entity in entities)
		{
			Queue<RecordedAction> returnActionToPlayThisRound = new Queue<RecordedAction>();
			foreach (RecordedAction recordedAction in m_actionsToPlay[entity].ToArray())
			{
				//TODO here :
				//Entities check in new EntityUILogic.cs wheter action changes in another depending on factors checked in said script
				//ex: MoveAction changes to ShootAction because of a Entity visible in coneRange
				//    => cone range trigger is in EntityUILogic.cs

				EntityAIPlugin.CheckActionResultInfo resultInfo = entity.AI.CheckAction(recordedAction);

				if (!resultInfo.isActionChanging)
				{
					recordedAction.action.Prepare(recordedAction.entityState);
					returnActionToPlayThisRound.Enqueue(recordedAction);
				}
				else
				{
					AEntityAction newAction = null;
					newAction = GetAction(resultInfo.replacedActionType, entity);
					newAction.Prepare(recordedAction.entityState);
					returnActionToPlayThisRound.Enqueue(new RecordedAction() { action = newAction, entityState = recordedAction.entityState });
				}
			}

			m_actionsToPlay[entity] = new(returnActionToPlayThisRound);
		}

		//2-recursively check for possible conflict and change actions if needed
		//	     => dealing with conflict can create new one
		m_recordedConflict = CheckForConflicts();
		while (m_recordedConflict.Count > 0)
		{
			m_recordedConflict = ResolveConflicts();
		}

		//c)play this phases entities turn actions

		currentPhase = TurnPhase.Playing;

		m_actionsBeingDone.Clear();
		List<Entity> playingEntities = new(m_actionsToPlay.Keys);
		foreach (Entity entity in playingEntities)
		{
			if (m_actionsToPlay[entity].Count != 0)
			{
				RecordedAction action = m_actionsToPlay[entity].Dequeue();
				m_actionsBeingDone.Add(entity, action);
				action.action.onEndPerform += OnActionEndPerform;
				action.action.Perform(action.entityState);
				//TODO : display action on cam one by one when in conflict
			}
		}
	}


	private List<System.Tuple<RecordedAction, RecordedAction>> CheckForConflicts ()
	{
		List<System.Tuple<RecordedAction, RecordedAction>> conflicts = new();
		foreach (Entity entity in m_actionsToPlay.Keys)
		{
			Queue<RecordedAction> actionsPlayedThisRound = m_actionsToPlay[entity];
			foreach (RecordedAction action in actionsPlayedThisRound.ToArray())
			{
				foreach (Entity otherEntity in m_actionsToPlay.Keys)
				{
					if (entity == otherEntity) continue;

					Queue<RecordedAction> otherEntityActionsPlayedThisRound = m_actionsToPlay[otherEntity];
					foreach (RecordedAction otherAction in otherEntityActionsPlayedThisRound.ToArray())
					{
						if (action.action.CheckConflict(otherAction.action))
							conflicts.Add(new System.Tuple<RecordedAction, RecordedAction>(action, otherAction));
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

	private void OnActionEndPerform ( Entity _performingEntity )
	{
		if (m_actionsToPlay.ContainsKey(_performingEntity) && m_actionsToPlay[_performingEntity].Count > 0)
		{
			//performing entity still has actions this phase to do
			RecordedAction action = m_actionsToPlay[_performingEntity].Dequeue();
			m_actionsBeingDone[_performingEntity] = action;
			action.action.onEndPerform += OnActionEndPerform;
			action.action.Perform(action.entityState);
		}
		else
		{
			//no more action for thos entity
			m_actionsBeingDone.Remove(_performingEntity);
			if (m_actionsBeingDone.Keys.Count == 0)
			{
				if (m_recordedActionInput.Keys.Count == 0)
					EndRound(); //end turn
				else
					StartNextPhase(); //end this phase
			}
		}

	}

	private void OnEntityDeath(Entity _entity )
	{
		_entity.Equipment.onDeath -= OnEntityDeath;

		m_recordedActionInput.Remove(_entity);
	}

	private void EndRound ()
	{
		Debug.Log("EndRound");

		//check if finish level condition (all enemy killed || all ally killed)
		GameManager.Instance.LevelCompletionCheck(out bool _areAllEnemiesDead, out bool areAllPlayerEntitiesDead);
		if (_areAllEnemiesDead || areAllPlayerEntitiesDead)
		{
			EndLevel(!areAllPlayerEntitiesDead);
		}
		else
		{
			StartInputPhase();
		}

	}

	private void EndLevel (bool _isSuccess)
	{
		if (_isSuccess)
			Debug.Log("Player Victory");
		else
			Debug.Log("Enemy Victory");
	}


	#endregion

}
