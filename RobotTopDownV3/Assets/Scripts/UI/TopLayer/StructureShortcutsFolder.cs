using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

public class StructureShortcutsFolder : MonoBehaviour
{
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EntityFaction, BuildingBtn> m_openShopBtns;
	[SerializeField] private BuildingBtn m_hangarBtn;
	[SerializeField] private BuildingBtn m_recycleShopBtn;
	[SerializeField] private BuildingBtn m_repairBtn;
	[SerializeField] private BuildingBtn m_missionSelectionPanelBtn;
	[SerializeField] private BaseButton m_shortenFolderToggleBtn;
	[SerializeField] private CollapsibleFolder m_structureFolder;

	private void Awake ()
	{
		foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BuildingBtn> shopBtn in m_openShopBtns)
			shopBtn.Value.Init(UIManager.Instance.GetPanel<ShopPanel>());

		m_hangarBtn.Init(UIManager.Instance.GetPanel<HangarPanel>());
		m_recycleShopBtn.Init(UIManager.Instance.GetPanel<RecyclePanel>());
		m_repairBtn.Init(UIManager.Instance.GetPanel<RepairStationPanel>());
		m_missionSelectionPanelBtn.Init(UIManager.Instance.GetPanel<SelectMissionPanel>());
		m_missionSelectionPanelBtn.SetLevel(0);
		m_shortenFolderToggleBtn.onClick += OnClickShortenFolderToggle;

		RefreshLevels();
		foreach (StructureUpgrade upgrade in GameAssets.current.game.structureUpgrades.Values)
			upgrade.onUpgradeDone += RefreshLevels;
	}

	private void OnDestroy ()
	{
		foreach (StructureUpgrade upgrade in GameAssets.current.game.structureUpgrades.Values)
			upgrade.onUpgradeDone -= RefreshLevels;
	}

	private void RefreshLevels ()
	{
		foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BuildingBtn> shopBtn in m_openShopBtns)
			shopBtn.Value.SetLevel(GameAssets.current.game.GetShopStructureUpgrade(shopBtn.Key).GetCurrentLevel() + 1);

		m_hangarBtn.SetLevel(GameAssets.current.game.HangarStructureUpgrade.GetCurrentLevel() + 1);
		m_recycleShopBtn.SetLevel(GameAssets.current.game.RecyclerStructureUpgrade.GetCurrentLevel() + 1);
		m_repairBtn.SetLevel(GameAssets.current.game.RepairStationStructureUpgrade.GetCurrentLevel() + 1);
	}

	public void SetVisibility(bool _isVisible )
	{
		gameObject.SetActive(_isVisible);
		if(_isVisible)
			RefreshVisual();
	}

	public void RefreshVisual ()
	{
		m_repairBtn.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockRepareStation);
		m_recycleShopBtn.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockRecycler);
		foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BuildingBtn> shopBtn in m_openShopBtns)
			shopBtn.Value.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockShops);
		m_missionSelectionPanelBtn.SetInteractability(GameDatas.current.currentPlayerSave.cycleCount > 0);
		//m_tournamentBtn.SetInteractability(isSquadValid);
	}

	private void OnClickShortenFolderToggle ()
	{
		m_structureFolder.Toggle();
	}

}
