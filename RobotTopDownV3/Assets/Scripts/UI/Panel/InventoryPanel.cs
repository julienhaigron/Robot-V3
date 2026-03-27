using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private Transform m_inventoryParent;

	[SerializeField] private EntityEquipmentDisplay m_equipmentDisplayPrefab;

	private List<EntityEquipmentDisplay> equipmentDisplay = new();

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<HangarPanel>();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshInventory();
	}

	private void RefreshInventory ()
	{
		for (int i = 0; i < GameDatas.current.currentPlayerSave.equipmentInventory.Count; i++)
		{
			if (equipmentDisplay.Count < i)
				equipmentDisplay.Add(Instantiate(m_equipmentDisplayPrefab, m_inventoryParent));

			equipmentDisplay[i].Init(GameDatas.current.currentPlayerSave.equipmentInventory[i].GetData<EntityEquipmentData>());
		}
	}
}
