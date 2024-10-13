using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEntityActionList : MonoBehaviour
{
	[SerializeField] private ActionButton m_baseActionButtonPrefab;
	[SerializeField] private BaseButton m_baseStateButtonPrefab;
	[SerializeField] private Transform m_buttonsParent;

	private List<ActionButton> m_actionButtons = new();
	private List<BaseButton> m_stateButtons = new();

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
			foreach(ActionButton btn in m_actionButtons)
			{
				btn.SetVisible(_isVisible: false, _isInstant: true);
			}
		}
		else
		{
			int amountMissingBtn = _entity.Data.knownedActions.Count - m_actionButtons.Count;
			for (int i = 0; i < amountMissingBtn; i++)
				CreateNewActionButton().SetVisible(false, true);

			for (int i = 0; i < m_actionButtons.Count; i++)
			{
				m_actionButtons[i].Init(_entity.Data.knownedActions[i]);
				m_actionButtons[i].SetVisible(_isVisible: true, _isInstant: true);
			}
		}
	}

	private ActionButton CreateNewActionButton ()
	{
		ActionButton newBtn = Instantiate(m_baseActionButtonPrefab, m_buttonsParent);
		m_actionButtons.Add(newBtn);
		return newBtn;
	}

	private BaseButton CreacteNewStateButton ()
	{
		BaseButton newButton = Instantiate(m_baseStateButtonPrefab, m_buttonsParent);
		m_stateButtons.Add(newButton);
		return 
	}

	private void OnClickStateButton ()
	{

	}

	public void HideButtons ()
	{
		OnEntitySelected(null);
	}
}
