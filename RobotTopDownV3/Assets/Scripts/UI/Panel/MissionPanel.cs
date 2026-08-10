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

	private MissionButton m_currentMissionHovered;
	private MissionButton m_currentMissionSelected;

	private void Awake ()
	{
		m_tutoBtn.Init(MissionDataEnumID.Tuto);
		MissionButton.onAnyMissionHovered += OnAnyMissionHovered;
		MissionButton.onAnyMissionSelected += OnAnyMissionSelected;
		UnitMissionDisplay.onAnyUnitHovered += OnAnyUnitHovered;
		m_startMissionBtn.onClick += OnClickStartMission;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshMissionBtns();

		m_currentMissionSelected = m_missionBtns[0];
		m_currentMissionSelected.SetHasSelected();
	}

	private void OnClickStartMission ()
	{
		if (m_currentMissionSelected.MissionData.preMissionDialogue != null)
			DialogueManager.Instance.PlayDialogue(m_currentMissionSelected.MissionData.preMissionDialogue, () => GameManager.Instance.SetupLevel(m_currentMissionSelected.MissionData));
		else
			GameManager.Instance.SetupLevel(m_currentMissionSelected.MissionData);
	}

	private void RefreshMissionBtns ()
	{
		bool doTutoMission = false;
#if UNITY_EDITOR
		doTutoMission = !GameConfig.current.debug.skipFTUE;
#endif
		for (int i = 0; i < m_missionBtns.Length; i++)
		{
			if (doTutoMission)
			{
				if (i == 0)
				{
					m_missionBtns[i].SetVisible(true, true);
					m_missionBtns[i].Init(GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds[GameDatas.current.currentPlayerSave.dayCount]);
				}
				else
					m_missionBtns[i].SetVisible(false, true);
			}
			else
			{
				if (GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds.Count > i)
				{
					m_missionBtns[i].SetVisible(true, true);
					m_missionBtns[i].Init(GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds[i]);
				}
				else
					m_missionBtns[i].SetVisible(false, true);
			}
		}

		for (int i = 0; i < m_unitDisplays.Length; i++)
		{
			if (GameDatas.current.currentPlayerSave.squadUnitsIndex.Count > i)
			{
				m_unitDisplays[i].Show();
				m_unitDisplays[i].Init(GameDatas.current.currentPlayerSave.allBuiltUnits[GameDatas.current.currentPlayerSave.squadUnitsIndex[i]], i, false);
			}
			else
				m_unitDisplays[i].Hide();
		}

		OnAnyMissionHovered(m_missionBtns[0]);
		OnAnyUnitHovered(m_unitDisplays[0]);
	}

	private void OnAnyMissionHovered ( MissionButton _missionBtn )
	{
		if(_missionBtn == null)
		{
			if (m_currentMissionSelected != null)
				OnAnyMissionHovered(m_currentMissionSelected);
			return;
		}
		if (!gameObject.activeInHierarchy || _missionBtn == m_currentMissionHovered)
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

		//List<CurrencyType> keys = _missionBtn.MissionData.currencyRewards;
		for (int i = 0; i < m_currencyRewardDisplays.Length; i++)
		{
			if (_missionBtn.MissionData.currencyRewards.Length > i)
			{
				m_currencyRewardDisplays[i].Show();
				m_currencyRewardDisplays[i].Init(_missionBtn.MissionData.currencyRewards[i].type, _missionBtn.MissionData.currencyRewards[i].amount, true, null);
			}
			else
				m_currencyRewardDisplays[i].Hide();
		}
	}

	private void OnAnyMissionSelected (MissionButton _missionButton)
	{
		m_currentMissionSelected = _missionButton;
	}

	private void OnAnyUnitHovered ( UnitMissionDisplay _display )
	{
		if (_display == null || _display.Data == null)
			return;

		m_hoveredUnitDisplay.Init(_display.Data, _display.Index, false);

		//set unit stats
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDescriptions = _display.Data.GetStatsDesciptions();
		List<EntityEquipmentData.SecondaryStat.StatType> keys = statsDescriptions.Keys.ToList();
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
