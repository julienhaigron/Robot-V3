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
		m_openShopBtns[EntityEquipmentData.EntityFaction.Psy].Init(UIManager.Instance.GetPanel<ShopPanel>(), GameAssets.current.game.PsyShopStructureUpgrade.GetCurrentLevel() + 1 );
		m_openShopBtns[EntityEquipmentData.EntityFaction.Commando].Init(UIManager.Instance.GetPanel<ShopPanel>(), GameAssets.current.game.CommandoShopStructureUpgrade.GetCurrentLevel() + 1 );
		m_openShopBtns[EntityEquipmentData.EntityFaction.Paladin].Init(UIManager.Instance.GetPanel<ShopPanel>(), GameAssets.current.game.PaladinShopStructureUpgrade.GetCurrentLevel() + 1 );

		m_hangarBtn.Init(UIManager.Instance.GetPanel<HangarPanel>(), GameAssets.current.game.HangarStructureUpgrade.GetCurrentLevel() + 1 );
		m_recycleShopBtn.Init(UIManager.Instance.GetPanel<RecyclePanel>(), GameAssets.current.game.RecyclerStructureUpgrade.GetCurrentLevel() + 1 );
		m_repairBtn.Init(UIManager.Instance.GetPanel<RepairStationPanel>(), GameAssets.current.game.RepairStationStructureUpgrade.GetCurrentLevel() + 1);
		m_missionSelectionPanelBtn.Init(UIManager.Instance.GetPanel<SelectMissionPanel>(), 0 );
		m_shortenFolderToggleBtn.onClick += OnClickShortenFolderToggle;
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
