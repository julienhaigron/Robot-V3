using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ShopPanel : AUIPanel
{
	[SerializeField] private ComponentDisplayGrid m_shopGrid;
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;
	[SerializeField] private Image m_corpIcon;

	private EntityEquipmentData.EntityFaction m_currentFaction;
	public EntityEquipmentData.EntityFaction CurrentFaction => m_currentFaction;
	public CurrencyType AssociatedCurrency
	{
		get
		{
			switch (m_currentFaction)
			{
				case EntityEquipmentData.EntityFaction.Psy:
					return CurrencyType.PsyCredit;
				case EntityEquipmentData.EntityFaction.Paladin:
					return CurrencyType.PaladinCredit;
				case EntityEquipmentData.EntityFaction.Commando:
					return CurrencyType.CommandoCredit;
				case EntityEquipmentData.EntityFaction.Dummy:
				case EntityEquipmentData.EntityFaction.Noone:
				case EntityEquipmentData.EntityFaction.Starting:
				case EntityEquipmentData.EntityFaction.Scout:
				default:
					return CurrencyType.SoftCurrency;
			}
		}
	}

	private void Awake ()
	{
		m_shopGrid.onItemAdded += SellItem;
		m_inventoryGrid.onItemAdded += BuyItem;
		GameDatas.onNewDay += OnNewDay;
	}

	public void Init ( EntityEquipmentData.EntityFaction _faction)
	{
		m_currentFaction = _faction;
		m_corpIcon.sprite = GameAssets.current.ui.corporationsIcons[_faction];
		m_corpIcon.color = GameAssets.current.ui.corporationsColors[_faction];
		RefreshShopBuyableItems();

		CurrencyType associatedCurrency = AssociatedCurrency;
		m_inventoryGrid.Init(m_shopGrid, null, null, item => item.GetData<EntityEquipmentData>().GetPrice().Item2 <= GameDatas.current.currentPlayerSave.currencies[associatedCurrency]
			, ComponentDisplay.DisplayMode.ShopSelling);
		m_inventoryGrid.Cleanup();

		for(int i = 0; i < GameConfig.current.game.maxInventoryCapacity; i++)
		{
			if(GameDatas.current.currentPlayerSave.equipmentInventory.Count > i)
				m_inventoryGrid.CreateNewDisplay(null, GameDatas.current.currentPlayerSave.equipmentInventory[i], ComponentDisplay.DisplayMode.ShopSelling);
			else
			{
				m_inventoryGrid.CreateNewDisplay(null, null, ComponentDisplay.DisplayMode.Empty);
			}
		}

		/*foreach (ComponentDisplay display in m_shopGrid.Items)
		{
			if (display.SavedData.GetData<EntityEquipmentData>().GetPrice().Item2 >= GameDatas.current.currentPlayerSave.currencies[associatedCurrency])
				display.SetInteractability(false);
			else
				display.SetInteractability(true);
		}*/
	}

	private void SellItem( ComponentContainer _container, ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Remove(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.currentPlayerSave.AddCurrency(price.Item1, price.Item2);
	}
	
	private void BuyItem( ComponentContainer _container, ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Add(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.currentPlayerSave.RemoveCurrency(price.Item1, price.Item2, GameDatas.PlayerSave.CurrencyRemoveMode.Spent);
	}

	public void RerollItem(ComponentDisplay _display )
	{
		int itemIndex = GameDatas.current.currentPlayerSave.dayData.itemsInShop.IndexOf(_display.ShopSavedData);

		EntityEquipmentData equipmentData = GameAssets.current.equipments.Values.ToArray().RandomElement();
		GameDatas.current.currentPlayerSave.dayData.itemsInShop.Add(new() { component = new() { ID = equipmentData.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = equipmentData.name, isDamaged = false }, isFrozen = false });

		_display.Init(null, GameDatas.current.currentPlayerSave.dayData.itemsInShop[itemIndex].component, ComponentDisplay.DisplayMode.ShopBuying);
		_display.SetShopData(GameDatas.current.currentPlayerSave.dayData.itemsInShop[itemIndex]);
	}

	private void RefreshShopBuyableItems ()
	{
		m_shopGrid.Init(m_inventoryGrid, null, null, null, ComponentDisplay.DisplayMode.ShopBuying);
		m_shopGrid.Cleanup();

		foreach (GameDatas.PlayerSave.DayData.ShopComponentData shopData in GameDatas.current.currentPlayerSave.dayData.itemsInShop)
			m_shopGrid.CreateNewDisplay(null, shopData.component, ComponentDisplay.DisplayMode.ShopBuying).SetShopData(shopData);
	}

	private void OnNewDay ()
	{
		RefreshShopBuyableItems();
	}

}
