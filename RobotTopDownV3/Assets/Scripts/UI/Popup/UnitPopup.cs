using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_texte;
	[SerializeField] private BaseButton m_closeBtn;

	private Entity m_entity;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}

	public void Init (Entity _entity)
	{
		m_entity = _entity;
		m_texte.text = _entity.Data.name;
	}

	private void OnClickClose ()
	{
		Close();
	}

}
