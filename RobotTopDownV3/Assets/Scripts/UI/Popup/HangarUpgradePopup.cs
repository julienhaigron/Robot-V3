using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HangarUpgradePopup : AUIPopup
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

	private void OnClickClose ()
	{
		Close();
	}

}
