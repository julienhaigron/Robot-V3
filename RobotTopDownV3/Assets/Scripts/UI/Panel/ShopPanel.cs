using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShopPanel : AUIPanel
{
	[SerializeField] private ComponentDisplayGrid m_shopGrid;
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	private EntityEquipmentData.EntityFaction m_currentFaction;
	public EntityEquipmentData.EntityFaction CurrentFaction => m_currentFaction;

	private void Awake ()
	{
		m_shopGrid.onItemAdded += SellItem;
		m_inventoryGrid.onItemAdded += BuyItem;
		GameDatas.onNewDay += OnNewDay;
	}

	public void Init ( EntityEquipmentData.EntityFaction _faction)
	{
		m_currentFaction = _faction;

		RefreshShopBuyableItems();

		m_inventoryGrid.Init(m_shopGrid, null, null, null, ComponentDisplay.DisplayMode.ShopSelling);
		m_inventoryGrid.Cleanup();
		foreach (GameDatas.PlayerSave.Equipment eq in GameDatas.current.currentPlayerSave.equipmentInventory)
			m_inventoryGrid.CreateNewDisplay(null, eq, ComponentDisplay.DisplayMode.ShopSelling);
	}

	private void SellItem(ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Remove(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.currentPlayerSave.AddCurrency(price.Item1, price.Item2);
	}
	
	private void BuyItem(ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Add(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.currentPlayerSave.RemoveCurrency(price.Item1, price.Item2, GameDatas.PlayerSave.CurrencyRemoveMode.Spent);
	}

	public void RerollItem(ComponentDisplay _display )
	{
		int itemIndex = GameDatas.current.currentPlayerSave.dayData.itemsInShop.IndexOf(_display.SavedData);

		EntityEquipmentData equipmentData = GameAssets.current.equipments.Values.ToArray().RandomElement();
		GameDatas.current.currentPlayerSave.dayData.itemsInShop[itemIndex] = new() { ID = equipmentData.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = equipmentData.name };

		_display.Init(null, GameDatas.current.currentPlayerSave.dayData.itemsInShop[itemIndex], ComponentDisplay.DisplayMode.ShopBuying);
	}

	private void RefreshShopBuyableItems ()
	{
		m_shopGrid.Init(m_inventoryGrid, null, null, null, ComponentDisplay.DisplayMode.ShopBuying);
		m_shopGrid.Cleanup();

		foreach (GameDatas.PlayerSave.Equipment equipmentData in GameDatas.current.currentPlayerSave.dayData.itemsInShop)
			m_shopGrid.CreateNewDisplay(null, equipmentData, ComponentDisplay.DisplayMode.ShopBuying);
	}

	private void OnNewDay ()
	{
		RefreshShopBuyableItems();
	}

}
