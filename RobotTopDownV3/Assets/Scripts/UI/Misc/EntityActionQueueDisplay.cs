using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityActionQueueDisplay : MonoBehaviour
{
    [SerializeField] private Transform m_actionDisplayParent;
    [SerializeField] private GameObject m_scrollView;

	private List<EntityActionDisplay> m_displays = new();

	private void Awake ()
	{
		PlayerController.onEntitySelected += OnEntitySelected;
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionRemoved += OnActionRemoved;
	}

	private void OnDestroy ()
	{
		PlayerController.onEntitySelected -= OnEntitySelected;
		TurnManager.onActionAdded -= OnActionAdded;
		TurnManager.onActionRemoved -= OnActionRemoved;
	}

	private void OnEntitySelected (int? _entityID )
	{
		if(_entityID != null && GameManager.Instance.GetEntityFromID(out Entity entity, _entityID.Value) && entity.IsAlliedTo(GameManager.Instance.PlayerID))
		{
			m_scrollView.SetActive(true);

			if (!TurnManager.Instance.RecordedActions.ContainsKey(_entityID.Value))
				return;

			int count = 0;
			foreach(TurnManager.RecordedAction action in TurnManager.Instance.RecordedActions[_entityID.Value])
			{
				if (m_displays.Count < count)
					CreateActionDisplayButton();

				m_displays[count].gameObject.SetActive(true);
				m_displays[count++].Init(action);
			}
			for(int i = count; count < m_displays.Count; i++)
			{
				m_displays[i].gameObject.SetActive(false);
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

	private void OnActionAdded (TurnManager.RecordedAction _newRecordedAction )
	{
		if(m_displays.Count >= TurnManager.Instance.RecordedActions[_newRecordedAction.performingEntityID].Count)
		{
			m_displays[TurnManager.Instance.RecordedActions[_newRecordedAction.performingEntityID].Count - 1].gameObject.SetActive(true);
			m_displays[TurnManager.Instance.RecordedActions[_newRecordedAction.performingEntityID].Count - 1].Init(_newRecordedAction);
		}
		else
		{
			CreateActionDisplayButton();
			m_displays[^1].Init(_newRecordedAction);
		}
	}

	private void OnActionRemoved ( TurnManager.RecordedAction _removedRecordedAction )
	{
		m_displays[TurnManager.Instance.RecordedActions[_removedRecordedAction.performingEntityID].Count - 1].gameObject.SetActive(false);
		m_displays[TurnManager.Instance.RecordedActions[_removedRecordedAction.performingEntityID].Count - 1].Hide();
	}
}
