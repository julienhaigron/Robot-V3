using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ActionButton : MonoBehaviour
{
    [SerializeField] private Button m_button;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_tokenCost;

	private EntityActionType m_actionType;
	private bool m_isVisible = false;

	private void Awake ()
	{
		m_button.onClick.AddListener(OnClick);
	}

	private void OnDestroy ()
	{
		m_button.onClick.RemoveListener(OnClick);
	}


	public void Init( EntityActionType _action )
	{
		m_actionType = _action;
		EntityActionData data = GameAssets.current.game.entityActionsData[_action];
		m_icon.sprite = data.icon;
		m_name.text = data.displayName;
		m_tokenCost.text = data.tokenCost.ToString();
	}

	public void SetVisible(bool _isVisible, bool _isInstant )
	{
		if (m_isVisible == _isVisible)
			return;

		m_isVisible = _isVisible;

		if (_isInstant)
			transform.localScale = _isVisible ? Vector3.one : Vector3.zero;
		else
			transform.DOScale(_isVisible ? 1f : 0f, 1f);
	}

	private void OnActionTokenUsed ()
	{
		//refresh if is available
	}

	private void OnClick ()
	{
		TurnManager.Instance.SetCurrentActionSelected(m_actionType);
	}


}
