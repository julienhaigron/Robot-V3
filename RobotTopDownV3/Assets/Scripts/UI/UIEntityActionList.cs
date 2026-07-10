using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class UIEntityActionList : MonoBehaviour
{
	[SerializeField] private Transform m_actionButtonsParent;
	[SerializeField] private Transform m_modActionButtonsParent;

	[SerializeField] private ActionButton[] m_actionButtons;
	[SerializeField] private ModActionButton[] m_modActionButtons;

	[Title("Parameters")]
	[SerializeField] private float m_actionListBaseWidth = 0f;
	[SerializeField] private float m_actionListElementWidth = 76.067f;
	[SerializeField] private float m_modActionListBaseWidth = 92.2728f;
	[SerializeField] private float m_modActionListElementWidth = 19f;

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

	private void OnEntitySelected ( int? _entityID )
	{
		if (_entityID == null)
			return;

		Entity selectedEntity = GameManager.Instance.GetEntityFromID((int)_entityID);
		EntityActionEnumID[] keys = selectedEntity.ComponentLinkedToAction.Keys.ToArray();
		for (int i = 0; i < m_actionButtons.Length; i++)
		{
			if (keys.Length > i)
			{
				m_actionButtons[i].Init(keys[i], selectedEntity.ComponentLinkedToAction[keys[i]][0]);
				m_actionButtons[i].SetVisible(_isVisible: true, _isInstant: true);
			}
			else
				m_actionButtons[i].SetVisible(_isVisible: false, _isInstant: true);
		}

		for (int i = 0; i < m_modActionButtons.Length; i++)
		{
			if (selectedEntity.KnownedModActions.Count > i)
			{
				m_modActionButtons[i].Init(selectedEntity.KnownedModActions[i], selectedEntity.ComponentLinkedToAction[selectedEntity.KnownedModActions[i]][0]);
				m_modActionButtons[i].SetVisible(_isVisible: true, _isInstant: true);
			}
			else
				m_modActionButtons[i].SetVisible(_isVisible: false, _isInstant: true);
		}

	}

	public void HideButtons ()
	{
		OnEntitySelected(null);
	}
}
