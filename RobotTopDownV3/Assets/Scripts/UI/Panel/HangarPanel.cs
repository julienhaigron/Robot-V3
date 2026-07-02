using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class HangarPanel : AUIPanel
{
	[SerializeField] private BaseButton m_addNewEntityBtn;

	[SerializeField] private TextMeshProUGUI m_maxUnitInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxEnergyCostInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxUnitInHangarTMP;

	HangarStructureUpgrade HangarUpgrade => GameAssets.current.game.structureUpgrades[StructureUpgradePopup.StructureType.Hangar] as HangarStructureUpgrade;

	private void Awake ()
	{
		m_addNewEntityBtn.onClick += OnClickCreateNewEntity;
	}

	private void OnClickCreateNewEntity ()
	{
		//FrameEquipmentData baseFrame = GameAssets.current.game.frames[0];
		HubManager.Instance.AddEntity(GameDatas.current.currentPlayerSave.AddNewUnit());

		RefreshDisplay();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		HubManager.Instance.ShowHangar();
		RefreshDisplay();
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
		HubManager.Instance.HideHangar();
	}

	private void RefreshDisplay ()
	{
		m_addNewEntityBtn.SetInteractability(GameDatas.current.currentPlayerSave.squadUnits.Count < HangarUpgrade.GetCurrentMaxHangarUnit());	
		m_maxUnitInSquadTMP.text = "Active squad: " + GameDatas.current.currentPlayerSave.squadUnits.Count +  "/" + HangarUpgrade.GetCurrentMaxHangarUnit();
		m_maxUnitInHangarTMP.text = "Inactive unit: " + GameDatas.current.currentPlayerSave.allBuiltUnits.Count + "/" + HangarUpgrade.GetCurrentMaxUnitAmount();

		int totalEnergyUsed = 0;
		foreach (EntitySavedData savedEntity in GameDatas.current.currentPlayerSave.squadUnits)
			totalEnergyUsed += savedEntity.GetTotalEnergyUsed();
		m_maxEnergyCostInSquadTMP.text = "Energy: " + totalEnergyUsed + "/" + HangarUpgrade.GetCurrentMaxSquadEnergyAmount();

	}
}
