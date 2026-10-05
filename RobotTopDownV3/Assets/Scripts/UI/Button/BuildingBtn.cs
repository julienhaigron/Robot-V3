using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class BuildingBtn : MonoBehaviour
{
	[SerializeField] private BaseButton m_openPanelBtn;
	[SerializeField] private BaseButton m_upgradeBuildingBtn;
	[SerializeField] private EntityEquipmentData.EntityFaction m_shopFaction;

	private AUIPanel m_panel;

	public void Init( AUIPanel _panel )
	{
		m_panel = _panel;
		m_openPanelBtn.onClick += OnClickOpenPanel;
		m_upgradeBuildingBtn.onClick += OnClickUpgradeBuilding;
	}

	private void OnClickOpenPanel ()
	{
		if(m_panel is SelectMissionPanel or HangarPanel)
			UIManager.Instance.OpenPanel(m_panel);
		else if (m_panel is RecyclePanel)
			UIManager.Instance.OpenPanel<RecyclePanel>().Init();
		else if (m_panel is RepairStationPanel)
			UIManager.Instance.OpenPanel<RepairStationPanel>().Init();
		else if(m_panel is ShopPanel)
			UIManager.Instance.OpenPanel<ShopPanel>().Init(m_shopFaction);

	}

	private void OnClickUpgradeBuilding ()
	{
		//UIManager.Instance.OpenPanel(m_panel);
	}

	public void SetInteractability (bool _isInstaractable)
	{
		m_openPanelBtn.SetInteractability(_isInstaractable);
		m_upgradeBuildingBtn.SetInteractability(_isInstaractable);
	}

}
