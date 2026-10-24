using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class HangarPanel : AUIPanel
{
	[SerializeField] private HangarEntityDisplay[] m_hangarUnits;
	[SerializeField] private BaseButton m_addNewEntityBtn;

	[SerializeField] private TextMeshProUGUI m_maxUnitInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxEnergyCostInSquadTMP;
	[SerializeField] private TextMeshProUGUI m_maxUnitInHangarTMP;

	private void Awake ()
	{
		m_addNewEntityBtn.onClick += OnClickCreateNewEntity;
	}

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += RefreshTexts;
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= RefreshTexts;
	}

	private void OnClickCreateNewEntity ()
	{
		/*EntitySavedData newUnit = new();
		newUnit.name = "New Unit";
		HubManager.Instance.AddEntity(GameDatas.current.currentPlayerSave.AddNewUnit(newUnit, false));

		RefreshDisplay();*/

		UIManager.Instance.OpenPanel<EntityConfigPanel>().InitNewUnit();
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

	public void RefreshDisplay ()
	{
		GameDatas.PlayerSave playerSave = GameDatas.current.currentPlayerSave;
		List<int> unitIndices = playerSave.GetUnitIndicesSquadFirst();
		for(int i = 0; i < m_hangarUnits.Length; i++)
		{
			if (i >= unitIndices.Count)
				m_hangarUnits[i].Hide();
			else
			{
				int unitIndex = unitIndices[i];
				m_hangarUnits[i].Init(playerSave.allBuiltUnits[unitIndex], unitIndex, playerSave.squadUnitsIndex.Contains(unitIndex));
				m_hangarUnits[i].Show();
			}
		}

		RefreshTexts();
	}

	public void RefreshTexts ()
	{
		int builtUnitCount = GameDatas.current.currentPlayerSave.allBuiltUnits.Count;
		int maxHangarUnit = GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxHangarUnit();
		m_addNewEntityBtn.SetInteractability(builtUnitCount < maxHangarUnit);
		int squadCount = GameDatas.current.currentPlayerSave.squadUnitsIndex.Count;
		m_maxUnitInSquadTMP.text = squadCount + "/" + GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxUnitAmount();
		m_maxUnitInHangarTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.hangar_inactive_unit), builtUnitCount, maxHangarUnit);

		int totalEnergyUsed = 0;
		foreach (EntitySavedData savedEntity in GameDatas.current.currentPlayerSave.GetSquadEntitiesData())
			totalEnergyUsed += savedEntity.GetTotalEnergyUsed();
		m_maxEnergyCostInSquadTMP.text = totalEnergyUsed + "/" + GameAssets.current.game.HangarStructureUpgrade.GetCurrentMaxSquadEnergyAmount();
	}
}
