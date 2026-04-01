using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RecyclePanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private BaseButton m_openInventoryBtn;
	[SerializeField] private BaseButton m_upgradeBtn;


	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_openInventoryBtn.onClick += OnClickOpenInventory;
		m_upgradeBtn.onClick += OnClickOpenUpgradePopup;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloHubPanel>();
	}

	private void OnClickOpenInventory ()
	{
		UIManager.Instance.OpenPanel<InventoryPanel>();
	}

	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Recycler);
	}
}
