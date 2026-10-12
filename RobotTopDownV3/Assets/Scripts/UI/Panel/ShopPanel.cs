using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class ShopPanel : AUIPanel
{
	[SerializeField] private ComponentDisplayGrid m_shopGrid;
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] BaseButton m_rerollBtn;
	[SerializeField] private TextMeshProUGUI m_rerollTMP;
	[SerializeField] private TextMeshProUGUI m_freezeTMP;

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
		m_rerollBtn.onClick += OnClickReroll;
	}

	private static GameDatas.PlayerSave.DayData CurrentDayData => GameDatas.current.currentPlayerSave.dayData;

	public void Init ( EntityEquipmentData.EntityFaction _faction)
	{
		m_currentFaction = _faction;
		m_corpIcon.sprite = GameAssets.current.ui.corporationsIcons[_faction];
		m_corpIcon.color = GameAssets.current.ui.corporationsColors[_faction];
		RefreshShopBuyableItems();

		CurrencyType associatedCurrency = AssociatedCurrency;
		m_inventoryGrid.Init(m_shopGrid, null, null, item => GameDatas.current.currentPlayerSave.currencies.ContainsKey(associatedCurrency) 
			&& item.TryGetData(out EntityEquipmentData data) && data.GetPrice().Item2 <= GameDatas.current.currentPlayerSave.currencies[associatedCurrency]
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

		RefreshShopActions();

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

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetShopSellingPrice();
		GameDatas.current.currentPlayerSave.AddCurrency(price.Item1, price.Item2);
		RefreshShopActions();
	}

	private void BuyItem( ComponentContainer _container, ComponentDisplay _display )
	{
		RemoveFromShop(_display.ShopSavedData);
		GameDatas.current.currentPlayerSave.AddEquipmentToInventory(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.currentPlayerSave.RemoveCurrency(price.Item1, price.Item2, GameDatas.PlayerSave.CurrencyRemoveMode.Spent);
		RefreshShopActions();
	}

	private void RemoveFromShop ( GameDatas.PlayerSave.DayData.ShopComponentData _shopData )
	{
		if (_shopData != null && CurrentDayData.itemsInShop.Remove(_shopData))
			RefundSameDayFreeze(_shopData);
	}

	private void OnClickReroll ()
	{
		if (GetRemainingRerolls() <= 0)
			return;

		AddUsage(CurrentDayData.shopRerollsUsed, m_currentFaction, 1);
		GameDatas.current.currentPlayerSave.RefillShop();
		RefreshShopBuyableItems();
		RefreshShopActions();
	}

	public void ToggleFreeze ( ComponentDisplay _display )
	{
		GameDatas.PlayerSave.DayData.ShopComponentData shopData = _display.ShopSavedData;

		if (shopData == null || !CurrentDayData.itemsInShop.Contains(shopData))
			return;

		if (shopData.isFrozen)
		{
			RefundSameDayFreeze(shopData);
			shopData.isFrozen = false;
		}
		else
		{
			if (GetRemainingFreezes() <= 0)
				return;

			AddUsage(CurrentDayData.shopFreezesUsed, m_currentFaction, 1);
			shopData.isFrozen = true;
			shopData.frozenTodayInShop = m_currentFaction;
		}

		_display.SetShopData(shopData);
		RefreshShopActions();
	}

	private void RefundSameDayFreeze ( GameDatas.PlayerSave.DayData.ShopComponentData _shopData )
	{
		if (_shopData.frozenTodayInShop == EntityEquipmentData.EntityFaction.Noone)
			return;

		AddUsage(CurrentDayData.shopFreezesUsed, _shopData.frozenTodayInShop, -1);
		_shopData.frozenTodayInShop = EntityEquipmentData.EntityFaction.Noone;
	}

	private int GetMaxRerolls ()
	{
		ShopStructureUpgrade upgrade = GameAssets.current.game.GetShopStructureUpgrade(m_currentFaction);
		return upgrade == null ? 0 : upgrade.GetMaxRerollAmount();
	}

	private int GetMaxFreezes ()
	{
		ShopStructureUpgrade upgrade = GameAssets.current.game.GetShopStructureUpgrade(m_currentFaction);
		return upgrade == null ? 0 : upgrade.GetMaxLockAmount();
	}

	private int GetRemainingRerolls ()
	{
		return Mathf.Max(0, GetMaxRerolls() - GetUsage(CurrentDayData.shopRerollsUsed, m_currentFaction));
	}

	private int GetRemainingFreezes ()
	{
		return Mathf.Max(0, GetMaxFreezes() - GetUsage(CurrentDayData.shopFreezesUsed, m_currentFaction));
	}

	private static int GetUsage ( SerializableDictionary<EntityEquipmentData.EntityFaction, int> _usages, EntityEquipmentData.EntityFaction _faction )
	{
		return _usages.TryGetValue(_faction, out int used) ? used : 0;
	}

	private static void AddUsage ( SerializableDictionary<EntityEquipmentData.EntityFaction, int> _usages, EntityEquipmentData.EntityFaction _faction, int _amount )
	{
		_usages[_faction] = Mathf.Max(0, GetUsage(_usages, _faction) + _amount);
	}

	private void RefreshShopActions ()
	{
		int remainingRerolls = GetRemainingRerolls();
		int remainingFreezes = GetRemainingFreezes();

		m_rerollTMP.text = remainingRerolls + "/" + GetMaxRerolls();
		m_freezeTMP.text = remainingFreezes + "/" + GetMaxFreezes();
		m_rerollBtn.SetInteractability(remainingRerolls > 0);

		foreach (ComponentDisplay display in m_shopGrid.Items)
		{
			GameDatas.PlayerSave.DayData.ShopComponentData shopData = display.ShopSavedData;
			display.SetFreezeInteractability(shopData != null && CurrentDayData.itemsInShop.Contains(shopData) && (shopData.isFrozen || remainingFreezes > 0));
		}
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
		RefreshShopActions();
	}

}
