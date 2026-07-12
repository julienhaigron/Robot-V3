using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class EntityActionQueueDisplay : MonoBehaviour
{
	[Title("Actions")]
	[SerializeField] private CounterDisplay m_actionTokenDisplay;
	[SerializeField] private EntityActionDisplay[] m_actionDisplays;
	[SerializeField] private Transform m_backgroundV1;
	[SerializeField] private Transform m_backgroundV2;

	[Title("States")]
	[SerializeField] private SerializableDictionary<Entity.EntityState, Transform> m_stateLineTfmDictionary;
	[SerializeField] private StateLineDisplay[] m_stateDisplays;
	[SerializeField] private SerializableDictionary<Entity.EntityState, StateLine> m_stateLines;

	[Title("PriorityQueue")]
	[SerializeField] private Transform m_actionPriorityQueueTfm;
	[SerializeField] private Image m_selectedActionIcon;
	[SerializeField] private PriorityQueueActionDisplay[] m_priorityQueueActionDisplays;
	[SerializeField] private float m_baseActionPriorityQueueHeight = 93f;
	[SerializeField] private float m_baseActionPriorityQueueElementHeight = 54.5f;

	private int? m_currentEntitySelected;

	[System.Serializable]
	public class StateLine
	{
		public StateLineSlot[] slots;
	}

	private void Awake ()
	{
		PlayerController.onEntitySelected += OnEntitySelected;
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionRemoved += OnActionRemoved;
		TurnManager.onEndInputPhase += OnEndInputPhase;

		foreach(Entity.EntityState state in m_stateLines.Keys)
		{
			for(int i = 0; i < m_stateLines[state].slots.Length; i++)
			{
				m_stateLines[state].slots[i].Init(state, i);
			}
		}

		RefreshVisual(null);
	}

	private void OnDestroy ()
	{
		PlayerController.onEntitySelected -= OnEntitySelected;
		TurnManager.onActionAdded -= OnActionAdded;
		TurnManager.onActionRemoved -= OnActionRemoved;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
	}

	private void OnEndInputPhase ()
	{
		RefreshVisual(null);
	}

	private void OnEntitySelected ( int? _entityID )
	{
		m_currentEntitySelected = _entityID;
		RefreshVisual(_entityID);
	}

	private void OnActionAdded ( TurnManager.RecordedAction _newRecordedAction )
	{
		RefreshVisual(m_currentEntitySelected);
	}

	private void OnActionRemoved ( TurnManager.RecordedAction _removedRecordedAction )
	{
		RefreshVisual(_removedRecordedAction.performingEntityID);
	}

	public void Init ()
	{
		RefreshVisual(null);
	}

	private void RefreshVisual ( int? _entityID )
	{
		/*if (_entityID == null || !GameManager.Instance.GetEntityFromID(out Entity entity, _entityID.Value) || !entity.IsAlliedTo(GameManager.Instance.PlayerID))
		{
			//gameObject.SetActive(false);
			return;

		}*/

		//gameObject.SetActive(true);

		//Actions
		RefreshActionQueue();

		//states
		RefreshStateQueue();

		RefreshPriorityQueue(_entityID);

		/*m_actionTokenDisplay.UpdateValue(TurnManager.Instance.RemainingActionToken[_entityID.Value]
			, _suffix: "/" + GameConfig.current.game.actionTokenPerRound);*/

	}

	private void RefreshActionQueue ()
	{
		Entity selectedEntity = PlayerController.Instance.SelectedEntity;
		if (selectedEntity == null || !TurnManager.Instance.RecordedActions.ContainsKey(selectedEntity.ID))
		{
			m_backgroundV1.gameObject.SetActive(selectedEntity != null);
			m_backgroundV2.gameObject.SetActive(selectedEntity == null);

			foreach (EntityActionDisplay display in m_actionDisplays)
				display.Hide(true);
			return;
		}

		m_backgroundV1.gameObject.SetActive(true);
		m_backgroundV2.gameObject.SetActive(false);
		TurnManager.RecordedAction[] recordedActions = TurnManager.Instance.RecordedActions[selectedEntity.ID].ToArray();
		for (int i = 0; i < m_actionDisplays.Length; i++)
		{
			if (recordedActions.Length > i)
			{
				m_actionDisplays[i].Init(recordedActions[i], i == 0 && selectedEntity != null);
			}
			else
				m_actionDisplays[i].Hide(true);
		}
	}

	private void RefreshStateQueue ()
	{
		Entity selectedEntity = PlayerController.Instance.SelectedEntity;
		if (selectedEntity == null || !TurnManager.Instance.RecordedActions.ContainsKey(selectedEntity.ID))
		{
			foreach (StateLineDisplay display in m_stateDisplays)
				display.Hide();
			return;
		}

		TurnManager.RecordedAction[] recordedActions = TurnManager.Instance.RecordedActions[selectedEntity.ID].ToArray();

		for (int i = 0; i < m_stateDisplays.Length; i++)
		{
			if (recordedActions.Length > i)
			{
				m_stateDisplays[i].transform.SetParent(m_stateLineTfmDictionary[recordedActions[i].entityState]);
				m_stateDisplays[i].Show();
				m_stateDisplays[i].Init(recordedActions[i].entityState, m_stateLines[recordedActions[i].entityState].slots[i], recordedActions[i]);
			}
			else
				m_stateDisplays[i].Hide();
		}

		foreach (Entity.EntityState state in m_stateLines.Keys)
		{
			int totalCost = 0;
			for (int i = 0; i < m_stateLines[state].slots.Length; i++)
			{
				if(recordedActions.Length > i)
				{
					m_stateLines[state].slots[i].RefresSizeAndPosition(recordedActions[i].action.TotalDuration, recordedActions[i].action.timeAtStart);
					m_stateLines[state].slots[i].Show();
					totalCost += recordedActions[i].action.TotalDuration;
				}
				else if(totalCost < 10)
				{
					m_stateLines[state].slots[i].RefresSizeAndPosition(1, totalCost);
					m_stateLines[state].slots[i].Show();
					totalCost += 1;
				}
				else
					m_stateLines[state].slots[i].Hide();

			}
		}
	}

	private void RefreshPriorityQueue ( int? _entityID )
	{
		if (_entityID == null)
		{
			foreach (PriorityQueueActionDisplay display in m_priorityQueueActionDisplays)
				display.Hide(true);

			m_actionPriorityQueueTfm.gameObject.SetActive(false);
			return;
		}

		m_actionPriorityQueueTfm.gameObject.SetActive(true);
		m_selectedActionIcon.sprite = TurnManager.Instance.CurrentActionSelected.Data.icon;
		List<EntityActionEnumID> recordedActions = PlayerController.Instance.SelectedEntity.GetReplacementActionFor(TurnManager.Instance.CurrentActionTypeSelected);

		Vector2 newSize = (m_actionPriorityQueueTfm.transform as RectTransform).sizeDelta;
		newSize.y = m_baseActionPriorityQueueHeight + (m_baseActionPriorityQueueElementHeight * recordedActions.Count);
		(m_actionPriorityQueueTfm.transform as RectTransform).sizeDelta = newSize;

		for (int i = 0; i < m_priorityQueueActionDisplays.Length; i++)
		{
			if (recordedActions.Count > i)
			{
				m_priorityQueueActionDisplays[i].Init(recordedActions[i]);
			}
			else
				m_priorityQueueActionDisplays[i].Hide(true);
		}
	}

#if UNITY_EDITOR

	[SerializeField] private EntityActionEnumID[] testActions;

	[Button]
	private void EditorTest (bool _isEntitySelected)
	{
		int timeAtStart = 0;
		for (int i = 0; i < m_actionDisplays.Length; i++)
		{
			if (testActions.Length > i && timeAtStart < m_actionDisplays.Length)
			{
				EntityActionData data = GameAssets.current.game.entityActionsData[testActions[i]];
				int preparationTime = data.GetTokenPreparationCost(null, null, null);
				int cooldownTime = data.GetTokenCooldownCost(null, null, null);
				int tyotalDuration = data.tokenDuration + preparationTime + cooldownTime;
				m_actionDisplays[i].Show(true);
				m_actionDisplays[i].RefreshVisual(timeAtStart, tyotalDuration, preparationTime, cooldownTime, i == 0 && _isEntitySelected);
				timeAtStart += tyotalDuration;
			}
			else
				m_actionDisplays[i].Hide(true);
		}
	}

#endif
}
