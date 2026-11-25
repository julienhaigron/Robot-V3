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

	private Color? m_energyBaseColor;

	private void Awake ()
	{
		m_addNewEntityBtn.onClick += OnClickCreateNewEntity;
	}

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += RefreshDisplay;
		GameAssets.current.game.HangarStructureUpgrade.onUpgradeDone += RefreshDisplay;
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= RefreshDisplay;
		GameAssets.current.game.HangarStructureUpgrade.onUpgradeDone -= RefreshDisplay;
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

	protected override void OnHideStarted ()
	{
		base.OnHideStarted();
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
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		HangarStructureUpgrade hangar = GameAssets.current.game.HangarStructureUpgrade;
		int builtUnitCount = save.allBuiltUnits.Count;
		int maxHangarUnit = hangar.GetCurrentMaxHangarUnit();
		m_addNewEntityBtn.SetInteractability(!save.IsHangarFull);
		m_maxUnitInSquadTMP.text = save.squadUnitsIndex.Count + "/" + hangar.GetCurrentMaxUnitAmount();
		m_maxUnitInHangarTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.hangar_inactive_unit), builtUnitCount, maxHangarUnit);

		m_energyBaseColor ??= m_maxEnergyCostInSquadTMP.color;
		m_maxEnergyCostInSquadTMP.text = save.GetSquadEnergyUsed() + "/" + hangar.GetCurrentMaxSquadEnergyAmount();
		m_maxEnergyCostInSquadTMP.color = save.IsSquadEnergyValid ? m_energyBaseColor.Value : GestureFeedback.RefusalColor;
	}
}
