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

		m_currentMissionNameTMP.text = _missionBtn.MissionData.missionName;
		m_currentMissionDescriptionTMP.text = _missionBtn.MissionData.GetDescription();

		for (int i = 0; i < m_componentRewardDisplays.Length; i++)
		{
			if (_missionBtn.MissionData.equipmentRewards.Count > i)
			{
				m_componentRewardDisplays[i].Show();
				m_componentRewardDisplays[i].Init(_missionBtn.MissionData.equipmentRewards[i], null);
			}
			else
				m_componentRewardDisplays[i].Hide();
		}

		List<CurrencyType> keys = _missionBtn.MissionData.currencyRewards.Keys.ToList();
		for (int i = 0; i < m_currencyRewardDisplays.Length; i++)
		{
			if (_missionBtn.MissionData.currencyRewards.Keys.Count > i)
			{
				m_currencyRewardDisplays[i].Show();
				m_currencyRewardDisplays[i].Init(keys[i], _missionBtn.MissionData.currencyRewards[keys[i]], true, null);
			}
			else
				m_currencyRewardDisplays[i].Hide();
		}
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
