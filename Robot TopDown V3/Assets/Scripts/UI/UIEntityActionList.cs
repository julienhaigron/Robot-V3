using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEntityActionList : MonoBehaviour
{
	[SerializeField] private ActionButton m_baseButtonPrefab;
	[SerializeField] private Transform m_buttonsParent;

	private List<ActionButton> m_buttons = new List<ActionButton>();

	private void Awake ()
	{
		PlayerController.onEntitySelected += OnEntitySelected;
		TurnManager.onEndInputPhase += HideButtons;
	}

	private void OnDestroy ()
	{
		PlayerController.onEntitySelected -= OnEntitySelected;
		TurnManager.onEndInputPhase -= HideButtons;
	}


	private void OnEntitySelected (Entity _entity)
	{
		if(_entity == null)
		{
			foreach(ActionButton btn in m_buttons)
			{
				btn.SetVisible(_isVisible: false, _isInstant: true);
			}
		}
		else
		{
			int amountMissingBtn = _entity.Data.knownedActions.Count - m_buttons.Count;
			for (int i = 0; i < amountMissingBtn; i++)
				CreateNewButton().SetVisible(false, true);

			for (int i = 0; i < m_buttons.Count; i++)
			{
				m_buttons[i].Init(_entity.Data.knownedActions[i]);
				m_buttons[i].SetVisible(_isVisible: true, _isInstant: true);
			}
		}
	}

	private ActionButton CreateNewButton ()
	{
		ActionButton newBtn = Instantiate(m_baseButtonPrefab, m_buttonsParent);
		m_buttons.Add(newBtn);
		return newBtn;
	}

	public void HideButtons ()
	{
		OnEntitySelected(null);
	}
}
