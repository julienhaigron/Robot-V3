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


	public void Init( EntityActionEnumID _action )
	{
		m_actionType = _action;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		m_name.text = data.displayName;
		m_tokenCost.text = data.tokenCost.ToString();
	}

	private void OnActionTokenUsed ()
	{
		//refresh if is available
	}

	protected override void OnClick ()
	{
		TurnManager.Instance.SetCurrentActionSelected(m_actionType);
		base.OnClick();
	}


}
