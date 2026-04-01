using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureUpgradePopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private BaseButton m_closeBtn;

	public enum StructureType
	{
		Hangar,
		Recycler,
		Shop,
		RepairStation
	}

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

	public void Init ( StructureType _type )
	{
		switch (_type)
		{
			case StructureType.Hangar:
				m_titleTMP.text = "Hangar upgrade";
				break;
			case StructureType.Recycler:
				m_titleTMP.text = "Recycler upgrade";

				break;
			case StructureType.Shop:
				m_titleTMP.text = "Shop upgrade";

				break;
			case StructureType.RepairStation:
				m_titleTMP.text = "Repair Station upgrade";

				break;
		}
	}

}
