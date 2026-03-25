using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SquadConfigPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private BaseButton m_openForgeBtn;
	[SerializeField] private BaseButton m_openInventoryBtn;
	[SerializeField] private Transform m_unitsParent;

	[SerializeField] private UnitDisplay m_unitDisplayPrefab;

	private List<UnitDisplay> unitDisplays = new();

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_openInventoryBtn.onClick += OnClickOpenInventory;
		m_openForgeBtn.onClick += OnClickOpenForge;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloCampainPanel>();
	}
	
	private void OnClickOpenInventory ()
	{
		UIManager.Instance.OpenPanel<InventoryPanel>();
	}
	
	private void OnClickOpenForge ()
	{
		UIManager.Instance.OpenPanel<ForgePanel>();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshUnits();
	}

	private void RefreshUnits ()
	{
		for(int i = 0; i < GameDatas.current.currentPlayerSave.units.Count; i++)
		{
			if(unitDisplays.Count < i)
				unitDisplays.Add(Instantiate(m_unitDisplayPrefab, m_unitsParent));

			unitDisplays[i].Init(GameDatas.current.currentPlayerSave.units[i]);
		}
	}
}
