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
	[SerializeField] private SlidingDoors m_hoveredUnitDoors;
	[SerializeField] private Image[] m_hoveredUnitComponentIcons;
	[SerializeField] private StatDisplay[] m_hoveredUnitStatDisplays;

	[Title("SingleMissionDetails")]
	[SerializeField] private TextMeshProUGUI m_currentMissionNameTMP;
	[SerializeField] private TextMeshProUGUI m_currentMissionDescriptionTMP;
	[SerializeField] private ComponentRewardDisplay[] m_componentRewardDisplays;
	[SerializeField] private CurrencyRewardDisplay[] m_currencyRewardDisplays;
	[SerializeField] private UnitRewardDisplay[] m_unitRewardDisplays;

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

		if (m_hoveredUnitDoors != null)
			m_hoveredUnitDoors.SetClosed(true, _isInstant: true);

		RefreshMissionBtns();

		m_currentMissionSelected = m_missionBtns[0];
		m_currentMissionSelected.SetHasSelected();
	}

	private void OnClickStartMission ()
	{
		if (m_currentMissionSelected == null || GameDatas.current.currentPlayerSave.squadUnitsIndex.Count == 0)
			return;

		if (m_currentMissionSelected.MissionData.preMissionDialogue != null)
			DialogueManager.Instance.PlayDialogue(m_currentMissionSelected.MissionData.preMissionDialogue, () => GameManager.Instance.SetupLevel(m_currentMissionSelected.MissionData));
		else
			GameManager.Instance.SetupLevel(m_currentMissionSelected.MissionData);
	}

	private void RefreshMissionBtns ()
	{
		//The buttons are reused from one opening to the next with a different mission on them, so a hovered
		//button kept from last time makes the guard in OnAnyMissionHovered short-circuit on stale rewards.
		m_currentMissionHovered = null;
		m_currentMissionSelected = null;

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

		m_startMissionBtn.SetInteractability(GameDatas.current.currentPlayerSave.squadUnitsIndex.Count > 0);

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

		m_currentMissionNameTMP.text = _missionBtn.MissionData.GetLocalizedName();
		m_currentMissionDescriptionTMP.text = _missionBtn.MissionData.GetDescription();

		MissionData.RewardSet rewardSet = _missionBtn.MissionData.GetRewardSet();

		for (int i = 0; i < m_componentRewardDisplays.Length; i++)
		{
			if (rewardSet.components.Count > i)
			{
				m_componentRewardDisplays[i].Show();
				m_componentRewardDisplays[i].Init(rewardSet.components[i], null);
				m_componentRewardDisplays[i].SetInteractable(false);
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
				m_currencyRewardDisplays[i].SetInteractable(false);
			}
			else
				m_currencyRewardDisplays[i].Hide();
		}

		UnitRewardDisplay.RefreshPreview(m_unitRewardDisplays, rewardSet);
	}

	private void OnAnyMissionSelected ( MissionButton _missionButton )
	{
		if (!gameObject.activeInHierarchy)
			return;

		if (_missionButton == m_currentMissionSelected)
		{
			_missionButton.SetHasSelected();
			return;
		}

		if (m_currentMissionSelected != null)
			m_currentMissionSelected.SetHasUnselected();

		m_currentMissionSelected = _missionButton;
		m_currentMissionSelected.SetHasSelected();
	}

	private void OnAnyUnitHovered ( UnitMissionDisplay _display )
	{
		if (_display == null || _display == m_hoveredUnitDisplay || _display.Data == null)
			return;

		if (m_hoveredUnitDoors == null)
			RefreshHoveredUnit(_display);
		else
			m_hoveredUnitDoors.CloseThenOpen(() => RefreshHoveredUnit(_display));
	}

	private void RefreshHoveredUnit ( UnitMissionDisplay _display )
	{
		m_hoveredUnitDisplay.Init(_display.Data, _display.Index, false);

		//set unit stats
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDescriptions = _display.Data.GetStatsDesciptions();
		List<EntityEquipmentData.SecondaryStat.StatType> keys = UIManager.Instance.GetPanel<EntityConfigPanel>().GetDisplayedStats(statsDescriptions);

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

		List<GameDatas.PlayerSave.Component> mainComponents = _display.Data.GetAllMainEquipments();
		for (int i = 0; i < m_hoveredUnitComponentIcons.Length; i++)
		{
			if (mainComponents.Count <= i || !mainComponents[i].TryGetData(out EntityEquipmentData data))
				m_hoveredUnitComponentIcons[i].gameObject.SetActive(false);
			else
			{
				m_hoveredUnitComponentIcons[i].gameObject.SetActive(true);
				m_hoveredUnitComponentIcons[i].sprite = data.icon;
			}
		}
	}
}
