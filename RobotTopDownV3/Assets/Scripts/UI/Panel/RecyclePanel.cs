using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RecyclePanel : AUIPanel
{
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private RecyclingComponentSlot[] m_recyclingSlots;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	private System.Func<GameDatas.PlayerSave.Component, bool> InventoryGridPredicate => item => item != null /*&& item.isDamaged*/;

	public void Init ()
	{
		int maxSlotAmount = GameAssets.current.game.RecyclerStructureUpgrade.GetCurrentMaxRecyclingSlotAmount();

		for (int currentSlotInSaveAmount = GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Count ; currentSlotInSaveAmount < maxSlotAmount; currentSlotInSaveAmount++)
			GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Add(new());

		for (int i = 0; i < m_recyclingSlots.Length; i++)
		{
			bool hasUnlockedSlot = maxSlotAmount > i;

			m_recyclingSlots[i].gameObject.SetActive(hasUnlockedSlot);

			if (hasUnlockedSlot)
				m_recyclingSlots[i].Init(m_inventoryGrid, i);
		}

		m_inventoryGrid.Cleanup();
		m_inventoryGrid.Init(null, null, null, InventoryGridPredicate, ComponentDisplay.DisplayMode.RecyclingStation);
	}

	public ComponentSlot GetFreeContainer ()
	{
		for (int i = 0; i < m_recyclingSlots.Length; i++)
		{
			if (m_recyclingSlots[i].gameObject.activeSelf && m_recyclingSlots[i].IsFree)
				return m_recyclingSlots[i];
		}

		return null;
	}
}
