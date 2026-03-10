using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ActionButton : BaseButton
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_tokenCost;

	private EntityActionEnumID m_actionType;

	private void Awake ()
	{
		TurnManager.onActionAdded += OnActionAdded;
	}

	private void OnDestroy ()
	{
		TurnManager.onActionAdded -= OnActionAdded;
	}

	public void Init( EntityActionEnumID _action )
	{
		m_actionType = _action;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		m_name.text = data.displayName;
		m_tokenCost.text = data.GetTokenTotalCost(null, null, null).ToString();

		RefreshInteractability();
	}

	private void RefreshInteractability ()
	{
		if (PlayerController.Instance.SelectedEntity == null)
			return;

		SetInteractability(GameAssets.current.game.entityActionsData[m_actionType].UseConditionPredicate(TurnManager.Instance.GetAction(m_actionType, PlayerController.Instance.SelectedEntity.ID), PlayerController.Instance.SelectedEntity, null));
	}

	public override void SetInteractability ( bool _isInteractable )
	{
		base.SetInteractability(_isInteractable);
		m_icon.color = _isInteractable ? Color.white : Color.black;
	}

	private void OnActionAdded (TurnManager.RecordedAction _addedAction)
	{
		//refresh if is available
		RefreshInteractability();
	}

	protected override void OnClick ()
	{
		TurnManager.Instance.SetCurrentActionSelected(m_actionType);
		base.OnClick();
	}


}
