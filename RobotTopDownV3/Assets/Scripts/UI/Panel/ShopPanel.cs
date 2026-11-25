using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class ShopPanel : AUIPanel
{
	public static System.Action<ShopPanel> onShopInitialized;

	[SerializeField] private ComponentDisplayGrid m_shopGrid;
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] BaseButton m_rerollBtn;
	[SerializeField] private TextMeshProUGUI m_rerollTMP;
	[SerializeField] private TextMeshProUGUI m_freezeTMP;
	[SerializeField] private BlueprintDisplay[] m_blueprintDisplays;
	[SerializeField] private GameObject m_noBlueprintGO;

	private EntityEquipmentData.EntityFaction m_currentFaction;
	public EntityEquipmentData.EntityFaction CurrentFaction => m_currentFaction;
	public CurrencyType AssociatedCurrency => EntityEquipmentData.GetFactionCurrency(m_currentFaction);

	private void Awake ()
	{
		m_shopGrid.onItemAdded += SellItem;
		m_inventoryGrid.onItemAdded += BuyItem;
		GameDatas.onNewDay += OnNewDay;
		m_rerollBtn.onClick += OnClickReroll;
	}

	private void OnEnable ()
	{
		foreach (EntityEquipmentData.EntityFaction faction in GameDatas.PlayerSave.DayData.ShopFactions)
			GameAssets.current.game.GetShopStructureUpgrade(faction).onUpgradeDone += OnShopUpgraded;
	}

	private void OnDisable ()
	{
		foreach (EntityEquipmentData.EntityFaction faction in GameDatas.PlayerSave.DayData.ShopFactions)
			GameAssets.current.game.GetShopStructureUpgrade(faction).onUpgradeDone -= OnShopUpgraded;
	}

	private static GameDatas.PlayerSave.DayData CurrentDayData => GameDatas.current.currentPlayerSave.dayData;

	public void Init ( EntityEquipmentData.EntityFaction _faction)
	{
		m_currentFaction = _faction;
		m_corpIcon.sprite = GameAssets.current.ui.corporationsIcons[_faction];
		m_corpIcon.color = GameAssets.current.ui.corporationsColors[_faction];
		GameDatas.current.currentPlayerSave.EnsureShopsAreSplit();
		RefreshShopBuyableItems();
		RefreshInventory();
		RefreshShopActions();
		onShopInitialized?.Invoke(this);
	}

	private void RefreshInventory ()
	{
		m_inventoryGrid.Init(m_shopGrid, null, null, item => !GameDatas.current.currentPlayerSave.IsInventoryFull && item.TryGetData(out EntityEquipmentData data)
			&& GameDatas.current.currentPlayerSave.CanAfford(data.GetShopBuyingPrice()), ComponentDisplay.DisplayMode.ShopSelling);
		m_inventoryGrid.Cleanup();

		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		foreach (GameDatas.PlayerSave.Component component in save.equipmentInventory)
			m_inventoryGrid.CreateNewDisplay(null, component, ComponentDisplay.DisplayMode.ShopSelling);
		m_inventoryGrid.Pad(save.InventoryCapacity);
	}

	private void OnShopUpgraded ()
	{
		RefreshInventory();
		RefreshShopActions();
	}

	private void SellItem( ComponentContainer _container, ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Remove(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetShopSellingPrice(m_currentFaction);
		GameDatas.current.currentPlayerSave.AddCurrency(price.Item1, price.Item2);
		_container.Discard(_display);
		RefreshShopActions();
	}

	private void BuyItem( ComponentContainer _container, ComponentDisplay _display )
	{
		RemoveFromShop(_display.ShopSavedData);
		GameDatas.current.currentPlayerSave.AddEquipmentToInventory(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetShopBuyingPrice();
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
		GameDatas.current.currentPlayerSave.RefillShop(m_currentFaction);
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
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		int remainingRerolls = GetRemainingRerolls();
		int remainingFreezes = GetRemainingFreezes();

		m_rerollTMP.text = remainingRerolls + "/" + GetMaxRerolls();
		m_freezeTMP.text = remainingFreezes + "/" + GetMaxFreezes();
		m_rerollBtn.SetInteractability(remainingRerolls > 0);
		m_inventoryGrid.SetPredicateRefusal(save.IsInventoryFull ? Gesture.Refusal.InventoryFull : Gesture.Refusal.TooExpensive);

		foreach (ComponentDisplay display in m_shopGrid.Items)
		{
			GameDatas.PlayerSave.DayData.ShopComponentData shopData = display.ShopSavedData;
			display.SetFreezeInteractability(shopData != null && CurrentDayData.itemsInShop.Contains(shopData) && (shopData.isFrozen || remainingFreezes > 0));
			display.SetAffordable(display.ComponentData == null || save.CanAfford(display.ComponentData.GetShopBuyingPrice()));
		}

		RefreshBlueprints();
	}

	private void RefreshBlueprints ()
	{
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		List<EntityEquipmentData> blueprints = save.ownedBlueprints
			.Select(name => GameAssets.current.equipments.TryGetValue(name, out EntityEquipmentData data) ? data : null)
			.Where(data => data != null && data.faction == m_currentFaction)
			.ToList();

		for (int i = 0; i < m_blueprintDisplays.Length; i++)
		{
			if (i < blueprints.Count)
				m_blueprintDisplays[i].Show(blueprints[i], save.CanAfford(blueprints[i].GetBlueprintProcessingPrice()), ProcessBlueprint);
			else
				m_blueprintDisplays[i].Hide();
		}

		m_noBlueprintGO.SetActive(blueprints.Count == 0);
	}

	private void ProcessBlueprint ( EntityEquipmentData _component )
	{
		if (GameDatas.current.currentPlayerSave.TryProcessBlueprint(_component))
			RefreshShopActions();
	}

	private void RefreshShopBuyableItems ()
	{
		m_shopGrid.Init(m_inventoryGrid, null, null, null, ComponentDisplay.DisplayMode.ShopBuying);
		m_shopGrid.Cleanup();

		foreach (GameDatas.PlayerSave.DayData.ShopComponentData shopData in GameDatas.current.currentPlayerSave.dayData.itemsInShop)
		{
			if (shopData != null && shopData.shopFaction == m_currentFaction)
				m_shopGrid.CreateNewDisplay(null, shopData.component, ComponentDisplay.DisplayMode.ShopBuying).SetShopData(shopData);
		}
	}

	private void OnNewDay ()
	{
		RefreshShopBuyableItems();
		RefreshShopActions();
	}

}
