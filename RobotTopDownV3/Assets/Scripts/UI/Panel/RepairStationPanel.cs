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
				m_repairingSlots[i].SetInteractability(!IsSlotUsedToday(i));
				m_repairingSlots[i].Init(m_inventoryGrid, repairingComponent != null && repairingComponent.unit != null && !string.IsNullOrEmpty(repairingComponent.unit.name) ? repairingComponent.unit : null
					, item => item != null && (repairingComponent == null || repairingComponent.unit == null)
					, i);
			}
		}

		m_inventoryGrid.Init(null, null, InventoryGridPredicate);
	}

	protected override void OnHideStarted ()
	{
		base.OnHideStarted();

		ReturnUnrepairedUnits();
	}

	private void ReturnUnrepairedUnits ()
	{
		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			if (m_repairingSlots[i].CurrentDisplay == null)
				continue;

			if (m_repairingSlots[i].CurrentDisplay.SavedData != null)
				m_repairingSlots[i].CurrentDisplay.SavedData.isRepairing = false;

			SetSlotData(i, null);
			m_repairingSlots[i].Cleanup();
		}
	}

	private bool IsSlotUsedToday ( int _index )
	{
		List<GameDatas.PlayerSave.DayData.RepairingUnitData> slots = GameDatas.current.currentPlayerSave.dayData.repairingComponents;

		return slots.Count > _index && slots[_index] != null && slots[_index].wasUsedToday;
	}

	private void SetSlotData ( int _index, EntitySavedData _unit, bool _doLockSlot = false )
	{
		List<GameDatas.PlayerSave.DayData.RepairingUnitData> slots = GameDatas.current.currentPlayerSave.dayData.repairingComponents;

		if (slots.Count <= _index)
			return;

		slots[_index] = new() { unit = _unit, wasUsedToday = _doLockSlot || IsSlotUsedToday(_index) };
	}

	public RepareUnitSlot GetFreeContainer ()
	{
		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			if (m_repairingSlots[i].gameObject.activeSelf && m_repairingSlots[i].CurrentDisplay == null && !IsSlotUsedToday(i))
				return m_repairingSlots[i];
		}

		return null;
	}

	private void OnItemAddedOnSlot ( RepareUnitContainer _container, RepareUnitDisplay _display )
	{
		SetSlotData(_container.Index, _display.SavedData);
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
		SetSlotData(_slot.Index, null, _doLockSlot: true);
		_slot.Cleanup();
		_slot.SetInteractability(false, _isInstant: false);

		Init();
	}

	private void OnItemRemovedOnSlot ( RepareUnitContainer _container, RepareUnitDisplay _display)
	{
		SetSlotData(_container.Index, null);
		_display.SavedData.isRepairing = false;
		m_repairingSlots[_container.Index].Cleanup();
	}

}
