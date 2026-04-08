using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShopPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private BaseButton m_upgradeBtn;

	[SerializeField] private ComponentDisplayGrid m_shopGrid;
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
		m_upgradeBtn.onClick += OnClickOpenUpgradePopup;

		m_shopGrid.onItemAdded += SellItem;
		m_inventoryGrid.onItemAdded += BuyItem;
	}

	public void Init ()
	{
		m_shopGrid.Init(m_inventoryGrid, null, null, item => true, ComponentDisplay.DisplayMode.ShopBuying);
		m_shopGrid.Cleanup();

		foreach (GameDatas.PlayerSave.Equipment equipmentData in GetTodayBuyableEquipments())
			m_shopGrid.CreateNewDisplay(null, equipmentData, ComponentDisplay.DisplayMode.ShopBuying);

		m_inventoryGrid.Init(m_shopGrid, null, null, item =>
		{
			System.Tuple<CurrencyType, ulong> price = item.GetData<EntityEquipmentData>().GetPrice();
			return GameDatas.current.currentPlayerSave.currencies[price.Item1] >= price.Item2;
		}, ComponentDisplay.DisplayMode.ShopSelling);
		m_inventoryGrid.Cleanup();
		foreach (GameDatas.PlayerSave.Equipment eq in GameDatas.current.currentPlayerSave.equipmentInventory)
			m_inventoryGrid.CreateNewDisplay(null, eq, ComponentDisplay.DisplayMode.ShopSelling);
	}

	private List<GameDatas.PlayerSave.Equipment> GetTodayBuyableEquipments ()
	{
		List<GameDatas.PlayerSave.Equipment> equipment = new();

		for (int i = 0; i < (GameAssets.current.game.structureUpgrades[StructureUpgradePopup.StructureType.Shop] as ShopStructureUpgrade).GetMaxItemAmount(); i++)
		{
			EntityEquipmentData equipmentData = GameAssets.current.equipments.Values.ToArray().RandomElement();
			equipment.Add(new() { ID = equipmentData.name + GameDatas.current.currentPlayerSave.equipmentCounter++, dataID = equipmentData.name });
		}

		return equipment;
	}

	private void SellItem(ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Remove(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.AddCurrency(price.Item1, price.Item2);
	}
	
	private void BuyItem(ComponentDisplay _display )
	{
		GameDatas.current.currentPlayerSave.equipmentInventory.Add(_display.SavedData);

		System.Tuple<CurrencyType, ulong> price = _display.ComponentData.GetPrice();
		GameDatas.current.RemoveCurrency(price.Item1, price.Item2, GameDatas.CurrencyRemoveMode.Spent);
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloHubPanel>();
	}

	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Shop);
	}
}
