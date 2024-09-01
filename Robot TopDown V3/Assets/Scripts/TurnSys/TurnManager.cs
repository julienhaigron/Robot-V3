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
	private Dictionary<Entity, RecordedAction> m_actionsBeingDone = new(); //current actions running

	private List<System.Tuple<RecordedAction, RecordedAction>> m_recordedConflict;

	private AEntityAction m_currentEntityAction;
	public AEntityAction CurrentActionSelected => m_currentEntityAction;
	private EntityActionEnum m_currentActionTypeSelected;
	public EntityActionEnum CurrentActionTypeSelected => m_currentActionTypeSelected;

	public enum TurnPhase { Recording, Calculating, Playing }
	public TurnPhase currentPhase;

	public struct RecordedAction
	{
		public AEntityAction action;
		public Entity.EntityState entityState;
	}

	#region Recording phase

	public Tile GetLastRegisteredPositionOfEntity ( Entity _entity )
	{
		if (m_recordedActionInput.ContainsKey(_entity) == false)
			return _entity.Displacement.Coordinates.GetTile();

		RecordedAction lastRecordedAction = m_recordedActionInput[_entity].ToArray()[^1];
		return lastRecordedAction.action.positionAtActionEnd;
	}

	public void SetCurrentActionSelected ( EntityActionEnum _action )
	{
		m_currentActionTypeSelected = _action;

		switch (_action)
		{
			case EntityActionEnum.Move:
				m_currentEntityAction = new MoveAction();
				m_currentEntityAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnum.Move], PlayerController.Instance.SelectedEntity, GetLastRegisteredPositionOfEntity(PlayerController.Instance.SelectedEntity));
				break;
			/*case EntityActionEnum.Attack:
				m_currentEntityAction = new Action();
				m_currentEntityAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnum.Attack], PlayerController.Instance.SelectedEntity, GetCurrentSelectedEntityTile());
				break;*/
			case EntityActionEnum.Wait:
				m_currentEntityAction = new WaitAction();
				m_currentEntityAction.Init(GameAssets.current.game.entityActionsData[EntityActionEnum.Wait], PlayerController.Instance.SelectedEntity, GetLastRegisteredPositionOfEntity(PlayerController.Instance.SelectedEntity));
				break;

		}


		onActionSelected?.Invoke(m_currentEntityAction);
	}

	public bool AddAction ( Entity _entity, AEntityAction _action )
	{
		Debug.Log("Action added");
		if (m_recordedActionInput.ContainsKey(_entity) == false)
			m_recordedActionInput.Add(_entity, new());
		/*else
			return false;*/

		m_recordedActionInput[_entity].Enqueue(new RecordedAction
		{
			action = _action,
			entityState = _entity.State
		});

		//Update action display on grid + UI
		onActionAdded?.Invoke(_action);

		//TODO : refresh selected entity actions display
		RefreshSelectedEntityActionDisplay();

		return true;
	}

	public void RefreshSelectedEntityActionDisplay ()
	{
		//1) pop ghost if no ghost
		//else: refresh ghost position

		//2) display all selected entity actions
		foreach (RecordedAction recordedAction in m_recordedActionInput[PlayerController.Instance.SelectedEntity])
		{
			recordedAction.action.Display();
		}
	}

	[Button]
	public void StartInputPhase ()
	{
		currentPhase = TurnPhase.Recording;
		//UIManager.Instance.OpenPanel<InGamePanel>();
	}

	[Button]
	public void EndInputPhase ()
	{
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
	}

	#endregion

	#region Play phase

	[Button]
	public void StartRound ()
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
		m_recordedActionInput = new(recordedActions);

		currentPhase = TurnPhase.Calculating;
		GridManager.Instance.StartNewPhase();

		//1- register action (like movment in grid)
		//   => checks at this moment if action result in conflict
		//
		foreach (Entity entity in m_actionsToPlay.Keys)
		{
			Queue<RecordedAction> actionsPlayedThisRound = m_actionsToPlay[entity];
			foreach (RecordedAction recordedAction in actionsPlayedThisRound.ToArray())
			{
				recordedAction.action.Prepare(recordedAction.entityState);
			}
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

		m_actionsBeingDone = new();
		foreach (Entity entity in m_actionsToPlay.Keys)
		{
			if (m_actionsToPlay[entity].Count != 0)
			{
				RecordedAction action = m_actionsToPlay[entity].Dequeue();
				m_actionsBeingDone[entity] = action;
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

					Queue<RecordedAction> otherEntityActionsPlayedThisRound = m_actionsToPlay[entity];
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
			if (conflict.Item1.action.CheckConflict(conflict.Item2.action))
				remainingConflict.Add(conflict);
		}

		return remainingConflict;
	}

	private void OnActionEndPerform ( Entity _performingEntity )
	{
		//d)wait for all entity to perform their actions to play in this phase to play the next one
		if (m_actionsToPlay.ContainsKey(_performingEntity) && m_actionsToPlay[_performingEntity].Count > 0)
		{
			RecordedAction action = m_actionsToPlay[_performingEntity].Dequeue();
			m_actionsBeingDone[_performingEntity] = action;
			action.action.onEndPerform += OnActionEndPerform;
			action.action.Perform(action.entityState);
		}
		else
		{
			m_actionsBeingDone.Remove(_performingEntity);
			if (m_actionsBeingDone.Keys.Count == 0)
			{
				if (m_recordedActionInput[_performingEntity].Count == 0)
				{
					m_recordedActionInput.Remove(_performingEntity);
				}

				if (m_recordedActionInput.Keys.Count == 0)
					EndRound(); //end turn
				else if(m_actionsBeingDone.Count == 0)
					StartNextPhase();

				//end this phase
			}
		}

	}

	private void EndRound ()
	{
		Debug.Log("EndRound");


		//TODO : check if finish level condition (all enemy killed || all ally killed)

		StartInputPhase();
	}


	#endregion


}
