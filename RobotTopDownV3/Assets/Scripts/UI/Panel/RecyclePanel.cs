using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RecyclePanel : AUIPanel
{
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private ComponentSlot[] m_recyclingSlots;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	private System.Func<GameDatas.PlayerSave.Component, bool> InventoryGridPredicate => item => item != null /*&& item.isDamaged*/;

	private void Awake ()
	{
		for(int i = 0; i < m_recyclingSlots.Length; i++)
		{
			m_recyclingSlots[i].onItemAdded += OnItemAddedOnSlot;
			m_recyclingSlots[i].onItemRemoved += OnItemRemovedOnSlot;
		}
	}

	public void Init ()
	{
		int maxSlotAmount = GameAssets.current.game.RecyclerStructureUpgrade.GetCurrentMaxRecyclingSlotAmount();

		for (int currentSlotInSaveAmount = GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Count ; currentSlotInSaveAmount < maxSlotAmount; currentSlotInSaveAmount++)
			GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Add(new());

		for (int i = 0; i < m_recyclingSlots.Length; i++)
		{
			bool hasUnlockedSlot = maxSlotAmount > i;
			GameDatas.PlayerSave.DayData.RecyclingComponentData recyclingComponent = hasUnlockedSlot
				? GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents[i] : null;

			if (!hasUnlockedSlot)
			{
				m_recyclingSlots[i].gameObject.SetActive(false);
			}
			else
			{
				//slotIndex, not i: a for loop variable is a single captured variable, so every closure would read
				//the value it holds after the loop. Reading the live entry also avoids the predicate answering
				//from the snapshot taken at Init time, long after the slot has changed.
				int slotIndex = i;

				m_recyclingSlots[i].gameObject.SetActive(true);
				m_recyclingSlots[i].Init(m_inventoryGrid, null, IsSlotFree(recyclingComponent) ? null : recyclingComponent.component
					, item => item != null && IsSlotFree(GetRecyclingDataAt(slotIndex))
					, ComponentDisplay.DisplayMode.RecyclingStation, i);
				m_recyclingSlots[i].InitRecyclingData(recyclingComponent);
			}
		}

		m_inventoryGrid.Init(null, null, null, InventoryGridPredicate, ComponentDisplay.DisplayMode.RecyclingStation);
	}

	//A recycling slot only frees up when ReturnToHubPopup pays its content out and clears the entry, so holding
	//a component is what makes a slot busy. The old test asked Predicate(null), and every predicate here starts
	//with "item != null", so it could never return a slot at all.
	public ComponentSlot GetFreeContainer ()
	{
		for (int i = 0; i < m_recyclingSlots.Length; i++)
		{
			if (m_recyclingSlots[i].gameObject.activeSelf && IsSlotFree(GetRecyclingDataAt(i)))
				return m_recyclingSlots[i];
		}

		return null;
	}

	private static GameDatas.PlayerSave.DayData.RecyclingComponentData GetRecyclingDataAt ( int _index )
	{
		List<GameDatas.PlayerSave.DayData.RecyclingComponentData> recyclingComponents = GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents;

		return _index >= 0 && _index < recyclingComponents.Count ? recyclingComponents[_index] : null;
	}

	private static bool IsSlotFree ( GameDatas.PlayerSave.DayData.RecyclingComponentData _data )
	{
		return _data == null || _data.component == null || string.IsNullOrEmpty(_data.component.ID);
	}

	private void OnItemAddedOnSlot (ComponentContainer _container, ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents[_container.Index] = new() { component = _display.SavedData, remainingTime = _display.ComponentData.recyclingDurationAmount };
		GameDatas.current.currentPlayerSave.equipmentInventory.Remove(_display.SavedData);
		m_recyclingSlots[_container.Index].InitRecyclingData(GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents[_container.Index]);
	}

	private void OnItemRemovedOnSlot ( ComponentContainer _container, ComponentDisplay _display )
	{
		//Taking a component back out cancels its recycling, so it returns to the inventory. Guarded against a
		//component that is somehow still listed there: the list must never hold the same one twice.
		GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents[_container.Index] = new();

		if (!GameDatas.current.currentPlayerSave.equipmentInventory.Contains(_display.SavedData))
			GameDatas.current.currentPlayerSave.AddEquipmentToInventory(_display.SavedData);
		m_recyclingSlots[_container.Index].Cleanup();
	}

}
