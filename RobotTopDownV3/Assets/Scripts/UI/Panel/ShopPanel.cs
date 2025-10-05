using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShopPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private BaseButton m_createUnitBtn;
	[SerializeField] private BaseButton m_openInventoryBtn;

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_openInventoryBtn.onClick += OnClickOpenInventory;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloCampainPanel>();
	}
	
	private void OnClickOpenInventory ()
	{
		UIManager.Instance.OpenPanel<InventoryPanel>();
	}
}
