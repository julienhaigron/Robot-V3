using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityActionQueueDisplay : MonoBehaviour
{
	[SerializeField] private Transform m_actionDisplayParent;
	[SerializeField] private GameObject m_scrollView;

	private List<EntityActionDisplay> m_displays = new();
	private int? m_currentEntitySelected;

	private void Awake ()
	{
		PlayerController.onEntitySelected += OnEntitySelected;
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionRemoved += OnActionRemoved;
		TurnManager.onEndInputPhase += OnEndInputPhase;
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
		RefreshQueue(null);
	}

	private void OnEntitySelected ( int? _entityID )
	{
		m_currentEntitySelected = _entityID;
		RefreshQueue(_entityID);
	}

	private void OnActionAdded ( TurnManager.RecordedAction _newRecordedAction )
	{
		/*if (m_displays.Count >= TurnManager.Instance.RecordedActions[_newRecordedAction.performingEntityID].Count)
		{
			m_displays[TurnManager.Instance.RecordedActions[_newRecordedAction.performingEntityID].Count - 1].gameObject.SetActive(true);
			m_displays[TurnManager.Instance.RecordedActions[_newRecordedAction.performingEntityID].Count - 1].Init(_newRecordedAction);
		}
		else
		{
			CreateActionDisplayButton();
			m_displays[^1].Init(_newRecordedAction);
		}*/
		RefreshQueue(m_currentEntitySelected);
	}

	private void OnActionRemoved ( TurnManager.RecordedAction _removedRecordedAction )
	{
		foreach(EntityActionDisplay display in m_displays)
		{
			if(display.RecordedAction.action == _removedRecordedAction.action)
			{
				display.Hide(false);
				return;
			}
		}
	}

	private void RefreshQueue ( int? _entityID )
	{
		if (_entityID != null && GameManager.Instance.GetEntityFromID(out Entity entity, _entityID.Value) && entity.IsAlliedTo(GameManager.Instance.PlayerID))
		{
			m_scrollView.SetActive(true);

			int count = 0;
			if (TurnManager.Instance.RecordedActions.ContainsKey(_entityID.Value))
			{
				foreach (TurnManager.RecordedAction action in TurnManager.Instance.RecordedActions[_entityID.Value])
				{
					if (m_displays.Count <= count)
						CreateActionDisplayButton();

					//TODO : take action type into account, display in different raw
					m_displays[count].gameObject.SetActive(true);
					m_displays[count++].Init(action);
				}
			}
			for (int i = count; i < m_displays.Count; i++)
			{
				m_displays[i].Hide(true);
			}
		}
		else
		{
			//hide stuff
			m_scrollView.SetActive(false);
		}
	}

	private EntityActionDisplay CreateActionDisplayButton ()
	{
		EntityActionDisplay newDisplay = Instantiate(GameAssets.current.ui.baseEntityActionDisplay, m_actionDisplayParent);
		m_displays.Add(newDisplay);
		return newDisplay;
	}
}
