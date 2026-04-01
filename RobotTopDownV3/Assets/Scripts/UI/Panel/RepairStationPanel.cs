using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RepairStationPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private BaseButton m_upgradeHangarBtn;


	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_upgradeHangarBtn.onClick += OnClickOpenUpgradePopup;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloHubPanel>();
	}
	
	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.RepairStation);
	}

}
