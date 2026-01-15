using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StateButton : BaseButton
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;

	private Entity.EntityState m_state;

	public void Init( Entity.EntityState _state )
	{
		m_state = _state;
		m_name.text = _state.ToString();
		m_icon.material = GameAssets.current.ui.entityStateMaterials[_state];
	}

	protected override void OnClick ()
	{
		TurnManager.Instance.SetCurrentStateSelected(m_state);
		base.OnClick();
	}


}
