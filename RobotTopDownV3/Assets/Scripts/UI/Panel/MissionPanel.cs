using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

public class MissionPanel : AUIPanel
{
	[Title("Missions")]
	[SerializeField] private MissionButton[] m_missionBtns;
	[SerializeField] private MissionButton m_tutoBtn;
	[SerializeField] private BaseButton m_startMissionBtn;
	
	[Title("Squad")]
	[SerializeField] private UnitMissionDisplay[] m_unitDisplays;
	[SerializeField] private UnitMissionDisplay m_hoveredUnitDisplay;
	[SerializeField] private Image[] m_hoveredUnitComponentIcons;
	[SerializeField] private StatDisplay[] m_hoveredUnitStatDisplays;

	[Title("SingleMissionDetails")]
	[SerializeField] private TextMeshProUGUI m_currentMissionNameTMP;
	[SerializeField] private TextMeshProUGUI m_currentMissionDescriptionTMP;
	[SerializeField] private ComponentRewardDisplay[] m_componentRewardDisplays;
	[SerializeField] private CurrencyRewardDisplay[] m_currencyRewardDisplays;

	private void Awake ()
	{
		m_tutoBtn.Init(MissionDataEnumID.Tuto);
		MissionButton.onAnyMissionSelected += OnAnyMissionSelected;
		UnitMissionDisplay.onAnyUnitHovered += OnAnyUnitHovered;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshMissionBtns();
	}

	private void RefreshMissionBtns ()
	{
		for (int i = 0; i < m_missionBtns.Length; i++)
		{
			if (GameDatas.current.currentPlayerSave.dayData.missionsIds.Count > i)
			{
				m_missionBtns[i].Show();
				m_missionBtns[i].Init(GameDatas.current.currentPlayerSave.dayData.missionsIds[i]);
			}
			else
				m_missionBtns[i].Hide();
		}

		int missionCount = 0;
		foreach(MissionButton btn in m_missionBtns)
		{
			btn.Init(GameDatas.current.currentPlayerSave.dayData.missionsIds[missionCount++]);
		}

		for(int i = 0; i < m_unitDisplays.Length; i++)
		{
			if (GameDatas.current.currentPlayerSave.squadUnits.Count > i)
			{
				m_unitDisplays[i].Show();
				m_unitDisplays[i].Init(GameDatas.current.currentPlayerSave.squadUnits[i]);
			}
			else
				m_unitDisplays[i].Hide();
		}

		OnAnyMissionSelected(m_missionBtns[0]);
		OnAnyUnitHovered(m_unitDisplays[0]);
	}

	private void OnAnyMissionSelected ( MissionButton _missionBtn)
	{
		if (!gameObject.activeInHierarchy)
			return;

		m_currentMissionNameTMP.text = _missionBtn.MissionData.missionName;
		m_currentMissionDescriptionTMP.text = _missionBtn.MissionData.GetDescription();

		for (int i = 0; i < m_componentRewardDisplays.Length; i++)
		{
			if (_missionBtn.MissionData.equipmentRewards.Count > i)
			{
				m_componentRewardDisplays[i].Show();
				m_componentRewardDisplays[i].Init(_missionBtn.MissionData.equipmentRewards[i]);
			}
			else
				m_componentRewardDisplays[i].Hide();
		}

		List<CurrencyType> keys = _missionBtn.MissionData.currencyRewards.Keys.ToList();
		for (int i = 0; i < m_currencyRewardDisplays.Length; i++)
		{
			if (_missionBtn.MissionData.currencyRewards.Keys.Count> i)
			{
				m_currencyRewardDisplays[i].Show();
				m_currencyRewardDisplays[i].Init(keys[i], _missionBtn.MissionData.currencyRewards[keys[i]]);
			}
			else
				m_currencyRewardDisplays[i].Hide();
		}		
	}

	private void OnAnyUnitHovered( UnitMissionDisplay _display )
	{
		if (_display == null || _display.Data == null)
			return;

		m_hoveredUnitDisplay.Init(_display.Data);

		//set unit stats
		SerializableDictionary<EntityEquipmentData.StatBonus.StatType, EntityEquipmentData.StatDescription> statsDescriptions = _display.Data.GetStatsDesciptions();
		List<EntityEquipmentData.StatBonus.StatType> keys = statsDescriptions.Keys.ToList();
		for (int i = 0; i < m_hoveredUnitStatDisplays.Length; i++)
		{
			if (keys.Count <= i)
				m_hoveredUnitStatDisplays[i].gameObject.SetActive(false);
			else
			{
				m_hoveredUnitStatDisplays[i].gameObject.SetActive(true);
				m_hoveredUnitStatDisplays[i].Init(statsDescriptions[keys[i]]);
			}
		}

		List<GameDatas.PlayerSave.Equipment> mainComponents = _display.Data.GetAllMainEquipments();
		for (int i = 0; i < m_hoveredUnitComponentIcons.Length; i++)
		{
			if (mainComponents.Count <= i)
				m_hoveredUnitComponentIcons[i].gameObject.SetActive(false);
			else
			{
				m_hoveredUnitComponentIcons[i].gameObject.SetActive(true);
				m_hoveredUnitComponentIcons[i].sprite = mainComponents[i].GetData<EntityEquipmentData>().icon;
			}
		}
	}
}
