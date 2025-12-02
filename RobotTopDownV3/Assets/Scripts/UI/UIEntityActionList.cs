using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEntityActionList : MonoBehaviour
{
	[SerializeField] private ActionButton m_baseActionButtonPrefab;
	[SerializeField] private StateButton m_baseStateButtonPrefab;
	[SerializeField] private Transform m_actionButtonsParent;
	[SerializeField] private Transform m_stateButtonsParent;

	private List<ActionButton> m_actionButtons = new();
	private List<StateButton> m_stateButtons = new();

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

	private void OnEntitySelected (int? _entityID)
	{
		if(_entityID == null)
		{
			foreach(ActionButton btn in m_actionButtons)
			{
				btn.SetVisible(_isVisible: false, _isInstant: true);
			}
			foreach (StateButton btn in m_stateButtons)
			{
				btn.SetVisible(_isVisible: false, _isInstant: true);
			}
		}
		else
		{
			Entity selectedEntity = GameManager.Instance.GetEntityFromID((int)_entityID);
			int amountMissingActionBtn = selectedEntity.Data.FrameData.knownedActions.Length - m_actionButtons.Count;
			for (int i = 0; i < amountMissingActionBtn; i++)
				CreateNewActionButton().SetVisible(false, true);

			int amountMissingStateBtn = selectedEntity.Data.FrameData.knownedStates.Length - m_stateButtons.Count;
			for (int i = 0; i < amountMissingStateBtn; i++)
				CreateNewStateButton().SetVisible(false, true);

			for (int i = 0; i < m_actionButtons.Count; i++)
			{
				m_actionButtons[i].Init(selectedEntity.Data.FrameData.knownedActions[i]);
				m_actionButtons[i].SetVisible(_isVisible: true, _isInstant: true);
			}

			for (int i = 0; i < m_stateButtons.Count; i++)
			{
				m_stateButtons[i].Init(selectedEntity.Data.FrameData.knownedStates[i]);
				m_stateButtons[i].SetVisible(_isVisible: true, _isInstant: true);
			}
		}
	}

	private ActionButton CreateNewActionButton ()
	{
		ActionButton newBtn = Instantiate(m_baseActionButtonPrefab, m_actionButtonsParent);
		m_actionButtons.Add(newBtn);
		return newBtn;
	}

	private BaseButton CreateNewStateButton ()
	{
		StateButton newButton = Instantiate(m_baseStateButtonPrefab, m_stateButtonsParent);
		m_stateButtons.Add(newButton);
		return newButton;
	}

	public void HideButtons ()
	{
		OnEntitySelected(null);
	}
}
