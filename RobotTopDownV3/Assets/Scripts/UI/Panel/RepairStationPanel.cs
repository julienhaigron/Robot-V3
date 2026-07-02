using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RepairStationPanel : AUIPanel
{
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private ComponentSlot[] m_repairingSlots;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	RepairStationStructureUpgrade StructureUpgrade => GameAssets.current.game.structureUpgrades[StructureUpgradePopup.StructureType.RepairStation] as RepairStationStructureUpgrade;

	private System.Func<GameDatas.PlayerSave.Equipment, bool> InventoryGridPredicate => item => item != null && item.isDamaged;

	private void Awake ()
	{
		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			m_repairingSlots[i].onItemAdded += OnItemAddedOnSlot;
			m_repairingSlots[i].onItemRemoved += OnItemRemovedOnSlot;
		}
	}

	public void Init ()
	{
		int maxSlotAmount = StructureUpgrade.GetCurrentMaxRepairedComponentSlotAmountPerLevel();

		for (int currentSlotInSaveAmount = GameDatas.current.currentPlayerSave.dayData.repairingComponents.Count; currentSlotInSaveAmount < maxSlotAmount; currentSlotInSaveAmount++)
			GameDatas.current.currentPlayerSave.dayData.repairingComponents.Add(new());

		for (int i = 0; i < m_repairingSlots.Length; i++)
		{
			bool hasUnlockedSlot = maxSlotAmount > i;
			GameDatas.PlayerSave.DayData.RepairingComponentData repairingComponent = maxSlotAmount > i
				? GameDatas.current.currentPlayerSave.dayData.repairingComponents[i] : null;

			if (!hasUnlockedSlot)
			{
				m_repairingSlots[i].gameObject.SetActive(false);
			}
			else
			{
				m_repairingSlots[i].gameObject.SetActive(true);
				m_repairingSlots[i].Init(m_inventoryGrid, null, repairingComponent != null && repairingComponent.component != null && !string.IsNullOrEmpty(repairingComponent.component.ID) ? repairingComponent.component : null
					, item => item != null && (repairingComponent == null || string.IsNullOrEmpty(repairingComponent.component.ID) || repairingComponent.remainingTime <= 0)
					, ComponentDisplay.DisplayMode.RepairStation, i);
				m_repairingSlots[i].InitRepairData(repairingComponent);
			}
		}

		m_inventoryGrid.Init(null, null, null, InventoryGridPredicate, ComponentDisplay.DisplayMode.RepairStation);
	}

	public ComponentSlot GetFreeContainer ()
	{
		foreach (ComponentSlot slot in m_repairingSlots)
		{
			if (slot.Predicate(null))
				return slot;
		}

		return null;
	}

	private void OnItemAddedOnSlot ( ComponentContainer _container, ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.dayData.repairingComponents[_container.Index] = new() { component = _display.SavedData, remainingTime = _display.ComponentData.reparingDurationAmount };
		GameDatas.current.currentPlayerSave.equipmentInventory.Remove(_display.SavedData);
		m_repairingSlots[_container.Index].InitRepairData(GameDatas.current.currentPlayerSave.dayData.repairingComponents[_container.Index]);
	}

	private void OnItemRemovedOnSlot ( ComponentContainer _container, ComponentDisplay _display)
	{
		GameDatas.current.currentPlayerSave.dayData.repairingComponents[_container.Index] = null;
		GameDatas.current.currentPlayerSave.equipmentInventory.Add(_display.SavedData);
		m_repairingSlots[_container.Index].Cleanup();
	}

}
