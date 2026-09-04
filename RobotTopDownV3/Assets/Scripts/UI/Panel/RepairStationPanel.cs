using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RepairStationPanel : AUIPanel
{
	[SerializeField] private RepareUnitDisplayGrid m_inventoryGrid;
	[SerializeField] private RepareUnitSlot[] m_repairingSlots;
	[SerializeField] private RepareUnitFullDisplay m_hoveredComponentFullInfoDisplay;

	private System.Func<EntitySavedData, bool> InventoryGridPredicate => item => item != null && item.IsDamaged();

	private void Awake ()
	{
		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			m_repairingSlots[i].onItemAdded += OnItemAddedOnSlot;
			m_repairingSlots[i].onItemRemoved += OnItemRemovedOnSlot;
			m_repairingSlots[i].onRepairClicked += OnClickRepair;
		}
	}

	private void OnDestroy ()
	{
		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			m_repairingSlots[i].onItemAdded -= OnItemAddedOnSlot;
			m_repairingSlots[i].onItemRemoved -= OnItemRemovedOnSlot;
			m_repairingSlots[i].onRepairClicked -= OnClickRepair;
		}
	}

	public void Init ()
	{
		int maxSlotAmount = GameAssets.current.game.RepairStationStructureUpgrade.GetCurrentMaxRepairedComponentSlotAmountPerLevel();

		for (int currentSlotInSaveAmount = GameDatas.current.currentPlayerSave.dayData.repairingComponents.Count; currentSlotInSaveAmount < maxSlotAmount; currentSlotInSaveAmount++)
			GameDatas.current.currentPlayerSave.dayData.repairingComponents.Add(new());

		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			bool hasUnlockedSlot = maxSlotAmount > i;
			GameDatas.PlayerSave.DayData.RepairingUnitData repairingComponent = maxSlotAmount > i
				? GameDatas.current.currentPlayerSave.dayData.repairingComponents[i] : null;

			if (!hasUnlockedSlot)
			{
				m_repairingSlots[i].gameObject.SetActive(false);
			}
			else
			{
				m_repairingSlots[i].gameObject.SetActive(true);
				m_repairingSlots[i].Init(m_inventoryGrid, repairingComponent != null && repairingComponent.unit != null && !string.IsNullOrEmpty(repairingComponent.unit.name) ? repairingComponent.unit : null
					, item => item != null && (repairingComponent == null || repairingComponent.unit == null)
					, i);
			}
		}

		m_inventoryGrid.Init(null, null, InventoryGridPredicate);
	}

	public RepareUnitSlot GetFreeContainer ()
	{
		foreach (RepareUnitSlot slot in m_repairingSlots)
		{
			if (slot.gameObject.activeSelf && slot.CurrentDisplay == null)
				return slot;
		}

		return null;
	}

	private void OnItemAddedOnSlot ( RepareUnitContainer _container, RepareUnitDisplay _display )
	{
		GameDatas.current.currentPlayerSave.dayData.repairingComponents[_container.Index] = new() { unit = _display.SavedData };
		_display.SavedData.isRepairing = true;
		GameDatas.current.currentPlayerSave.squadUnitsIndex.Remove(_display.SavedData.index);
		m_repairingSlots[_container.Index].RefreshRepairData();
	}

	private void OnClickRepair ( RepareUnitSlot _slot )
	{
		EntitySavedData unit = _slot.CurrentDisplay == null ? null : _slot.CurrentDisplay.SavedData;

		if (unit == null || !unit.IsDamaged())
			return;

		System.Tuple<CurrencyType, ulong> price = unit.GetRepairPrice();

		if (GameDatas.current.currentPlayerSave.currencies[price.Item1] < price.Item2)
			return;

		GameDatas.current.currentPlayerSave.RemoveCurrency(price.Item1, price.Item2, GameDatas.PlayerSave.CurrencyRemoveMode.Spent);

		foreach (GameDatas.PlayerSave.Component eq in unit.GetAllEquipments())
			eq.isDamaged = false;

		unit.isRepairing = false;
		GameDatas.current.currentPlayerSave.dayData.repairingComponents[_slot.Index] = new();
		_slot.Cleanup();

		Init();
	}

	private void OnItemRemovedOnSlot ( RepareUnitContainer _container, RepareUnitDisplay _display)
	{
		GameDatas.current.currentPlayerSave.dayData.repairingComponents[_container.Index] = null;
		_display.SavedData.isRepairing = false;
		m_repairingSlots[_container.Index].Cleanup();
	}

}
