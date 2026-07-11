using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class UIEntityActionList : MonoBehaviour
{
	[SerializeField] private RectTransform m_actionButtonsParent;
	[SerializeField] private RectTransform m_modActionButtonsParent;

	[SerializeField] private ActionButton[] m_actionButtons;
	[SerializeField] private ModActionButton[] m_modActionButtons;

	[Title("Parameters")]
	[SerializeField] private float m_actionListBaseWidth = 115.6f;
	[SerializeField] private float m_actionListElementWidth = 71.6f;
	[SerializeField] private float m_modActionListBaseWidth = 92.24f;
	[SerializeField] private float m_modActionListElementWidth = 31.93f;

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

	public void Init ()
	{
		OnEntitySelected(null);
	}

	private void OnEntitySelected ( int? _entityID )
	{
		if (_entityID == null)
		{
			gameObject.SetActive(false);
			return;
		}

		gameObject.SetActive(true);
		Entity selectedEntity = PlayerController.Instance.SelectedEntity;
		EntityActionEnumID[] keys = selectedEntity.ComponentLinkedToAction.Keys.ToArray();
		EntityActionEnumID[] keys2 = selectedEntity.KnownedModActions.ToArray();

		Vector2 newSize = m_actionButtonsParent.sizeDelta;
		newSize.x = m_actionListBaseWidth + ((keys.Length - 1) * m_actionListElementWidth);
		m_actionButtonsParent.sizeDelta = newSize;
		Vector2 newSize2 = m_modActionButtonsParent.sizeDelta;
		newSize2.x = m_modActionListBaseWidth + ((keys2.Length - 1) * m_modActionListElementWidth);
		m_modActionButtonsParent.sizeDelta = newSize2;

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
