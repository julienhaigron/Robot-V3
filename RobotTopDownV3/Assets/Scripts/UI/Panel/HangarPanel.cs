using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class HangarPanel : AUIPanel
{
	[SerializeField] private UnitMissionDisplay[] m_hangarUnits;
	[SerializeField] private BaseButton m_addNewEntityBtn;

	[SerializeField] private TextMeshProUGUI m_maxUnitInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxEnergyCostInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxUnitInHangarTMP;

	private void Awake ()
	{
		m_addNewEntityBtn.onClick += OnClickCreateNewEntity;
	}

	private void OnClickCreateNewEntity ()
	{
		//FrameEquipmentData baseFrame = GameAssets.current.game.frames[0];
		EntitySavedData newUnit = new();
		newUnit.name = "New Unit";
		HubManager.Instance.AddEntity(GameDatas.current.currentPlayerSave.AddNewUnit(newUnit, false));

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
		for(int i = 0; i < m_hangarUnits.Length; i++)
		{
			if (i >= GameDatas.current.currentPlayerSave.allBuiltUnits.Count)
				m_hangarUnits[i].Hide();
			else
			{
				EntitySavedData entityData = GameDatas.current.currentPlayerSave.allBuiltUnits[i];
				bool isInSquad = GameDatas.current.currentPlayerSave.squadUnits.Contains(entityData);
				m_hangarUnits[i].Init(entityData, isInSquad);
				m_hangarUnits[i].Show();
			}
		}

		RefreshTexts();
	}

	public void RefreshTexts ()
	{
		m_addNewEntityBtn.SetInteractability(GameDatas.current.currentPlayerSave.squadUnits.Count < GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxHangarUnit());
		int squadCount = GameDatas.current.currentPlayerSave.squadUnits.Count;
		m_maxUnitInSquadTMP.text = "Active squad: " + squadCount + "/" + GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxHangarUnit();
		m_maxUnitInHangarTMP.text = "Inactive unit: " + (GameDatas.current.currentPlayerSave.allBuiltUnits.Count - GameDatas.current.currentPlayerSave.squadUnits.Count) + "/" + GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxUnitAmount();

		int totalEnergyUsed = 0;
		foreach (EntitySavedData savedEntity in GameDatas.current.currentPlayerSave.squadUnits)
			totalEnergyUsed += savedEntity.GetTotalEnergyUsed();
		m_maxEnergyCostInSquadTMP.text = "Energy: " + totalEnergyUsed + "/" + GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxSquadEnergyAmount();
	}
}
