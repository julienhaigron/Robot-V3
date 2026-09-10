using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

public class SelectMissionPanel : AUIPanel
{
	[SerializeField] private MissionButton[] m_missionBtns;

	[Title("SingleMissionDetails")]
	[SerializeField] private TextMeshProUGUI m_currentMissionNameTMP;
	[SerializeField] private TextMeshProUGUI m_currentMissionDescriptionTMP;
	[SerializeField] private ComponentRewardDisplay[] m_componentRewardDisplays;
	[SerializeField] private CurrencyRewardDisplay[] m_currencyRewardDisplays;
	[SerializeField] private UnitRewardDisplay[] m_unitRewardDisplays;

	private MissionButton m_currentMissionHovered;

	private void Awake ()
	{
		MissionButton.onAnyMissionHovered += OnAnyMissionHovered;
		MissionButton.onAnyMissionSelected += OnAnyMissionSelected;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshMissions();
	}

	private void RefreshMissions ()
	{
		for (int i = 0; i < m_missionBtns.Length; i++)
		{
			if (GameDatas.current.currentPlayerSave.cycleData.availableMissionsIds.Count > i)
			{
				m_missionBtns[i].SetVisible(true, true);
				m_missionBtns[i].Init(GameDatas.current.currentPlayerSave.cycleData.availableMissionsIds[i]);
			}
			else
				m_missionBtns[i].SetVisible(false, true);
		}

		foreach(MissionDataEnumID enumID in GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds)
		{
			foreach(MissionButton btn in m_missionBtns)
			{
				if (!btn.IsSelected && btn.IsVisible && btn.MissionData.enumID == enumID)
					btn.SetHasSelected();
			}
		}
	}

	private void OnAnyMissionHovered ( MissionButton _missionBtn )
	{
		if (_missionBtn == null || !gameObject.activeInHierarchy || _missionBtn == m_currentMissionHovered)
			return;

		m_currentMissionHovered = _missionBtn;

		m_currentMissionNameTMP.text = _missionBtn.MissionData.GetLocalizedName();
		m_currentMissionDescriptionTMP.text = _missionBtn.MissionData.GetDescription();

		MissionData.RewardSet rewardSet = _missionBtn.MissionData.GetRewardSet();

		for (int i = 0; i < m_componentRewardDisplays.Length; i++)
		{
			if (rewardSet.components.Count > i)
			{
				m_componentRewardDisplays[i].Show();
				m_componentRewardDisplays[i].Init(rewardSet.components[i], null);
			}
			else
				m_componentRewardDisplays[i].Hide();
		}

		for (int i = 0; i < m_currencyRewardDisplays.Length; i++)
		{
			if (rewardSet.creditAmounts.Count > i)
			{
				m_currencyRewardDisplays[i].Show();
				m_currencyRewardDisplays[i].Init(CurrencyType.SoftCurrency, rewardSet.creditAmounts[i], true, null);
			}
			else
				m_currencyRewardDisplays[i].Hide();
		}

		UnitRewardDisplay.RefreshPreview(m_unitRewardDisplays, rewardSet);
	}

	private void OnAnyMissionSelected(MissionButton _missionBtn )
	{
		if (!gameObject.activeInHierarchy)
			return;

		if (_missionBtn.IsSelected)
			GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds.Add(_missionBtn.MissionData.enumID);
		else
			GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds.Remove(_missionBtn.MissionData.enumID);
	}
}
