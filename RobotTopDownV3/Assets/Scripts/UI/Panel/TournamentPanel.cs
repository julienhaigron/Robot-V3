using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

public class TournamentPanel : AUIPanel
{
	[SerializeField] private BaseButton m_startMissionBtn;

	[Title("Squad")]
	[SerializeField] private UnitMissionDisplay[] m_unitDisplays;
	[SerializeField] private UnitMissionDisplay m_hoveredUnitDisplay;
	[SerializeField] private Image[] m_hoveredUnitComponentIcons;
	[SerializeField] private StatDisplay[] m_hoveredUnitStatDisplays;

	[Title("Other Squads")]
	[SerializeField] private Image[] m_cursors;
	[SerializeField] private UnitMissionDisplay[] m_round1SquadUnits;
	[SerializeField] private UnitMissionDisplay[] m_round2SquadUnits;
	[SerializeField] private UnitMissionDisplay[] m_round3SquadUnits;
	[SerializeField] private Image[] m_squadsIcons;

	[Title("Rewards")]
	[SerializeField] private ComponentRewardDisplay[] m_componentRewardDisplays;
	[SerializeField] private CurrencyRewardDisplay[] m_currencyRewardDisplays;

	private int CurrentRound => GameDatas.current.currentPlayerSave.dayCount - 4;

	private void Awake ()
	{
		UnitMissionDisplay.onAnyUnitHovered += OnAnyUnitHovered;
		m_startMissionBtn.onClick += OnClickStartMission;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshSquads();

		SetCurrentMatchInfo(GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[CurrentRound]]);
	}

	private void RefreshSquads ()
	{
		if (!GameDatas.current.currentPlayerSave.cycleData.hasInitTournament)
			GameDatas.current.currentPlayerSave.cycleData.StartTournament();

		for (int i = 0; i < m_cursors.Length; i++)
			m_cursors[i].gameObject.SetActive(i == CurrentRound);

		foreach (Image image in m_squadsIcons)
			image.gameObject.SetActive(false);

		for (int i = 0; i < m_unitDisplays.Length; i++)
		{
			if (GameDatas.current.currentPlayerSave.squadUnits.Count > i)
			{
				m_unitDisplays[i].Show();
				m_unitDisplays[i].Init(GameDatas.current.currentPlayerSave.squadUnits[i]);
			}
			else
				m_unitDisplays[i].Hide();
		}

		if (CurrentRound >= 0)
		{
			MissionData missiondata = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[0]];
			for (int i = 0; i < m_round1SquadUnits.Length; i++)
			{
				if (missiondata.levelMission.enemies.Length > i)
				{
					m_round1SquadUnits[i].Init(missiondata.levelMission.enemies[i].GetSavedData());
					m_round1SquadUnits[i].Show();
				}
				else
					m_round1SquadUnits[i].Hide();
			}

			EntityEquipmentData.EntityFaction dominentCorpo = GetDominentSquadFaction(missiondata.levelMission.enemies.ToList());
			m_squadsIcons[0].gameObject.SetActive(true);
			m_squadsIcons[0].sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
			m_squadsIcons[0].color = GameAssets.current.ui.corporationsColors[dominentCorpo];
		}
		if (CurrentRound >= 1)
		{
			MissionData missiondata = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[1]];
			for (int i = 0; i < m_round2SquadUnits.Length; i++)
			{
				if (missiondata.levelMission.enemies.Length > i)
				{
					m_round2SquadUnits[i].Init(missiondata.levelMission.enemies[i].GetSavedData());
					m_round2SquadUnits[i].Show();
				}
				else
					m_round2SquadUnits[i].Hide();
			}
			EntityEquipmentData.EntityFaction dominentCorpo = GetDominentSquadFaction(missiondata.levelMission.enemies.ToList());
			m_squadsIcons[1].gameObject.SetActive(true);
			m_squadsIcons[1].sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
			m_squadsIcons[1].color = GameAssets.current.ui.corporationsColors[dominentCorpo];
		}
		if (CurrentRound >= 2)
		{
			MissionData missiondata = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[2]];
			for (int i = 0; i < m_round3SquadUnits.Length; i++)
			{
				if (missiondata.levelMission.enemies.Length > i)
				{
					m_round3SquadUnits[i].Init(missiondata.levelMission.enemies[i].GetSavedData());
					m_round3SquadUnits[i].Show();
				}
				else
					m_round3SquadUnits[i].Hide();
			}
			EntityEquipmentData.EntityFaction dominentCorpo = GetDominentSquadFaction(missiondata.levelMission.enemies.ToList());
			m_squadsIcons[2].gameObject.SetActive(true);
			m_squadsIcons[2].sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
			m_squadsIcons[2].color = GameAssets.current.ui.corporationsColors[dominentCorpo];
		}

		OnAnyUnitHovered(m_unitDisplays[0]);
	}

	private void SetCurrentMatchInfo ( MissionData _data )
	{
		for (int i = 0; i < m_componentRewardDisplays.Length; i++)
		{
			if (_data.equipmentRewards.Count > i)
			{
				m_componentRewardDisplays[i].Show();
				m_componentRewardDisplays[i].Init(_data.equipmentRewards[i]);
			}
			else
				m_componentRewardDisplays[i].Hide();
		}

		List<CurrencyType> keys = _data.currencyRewards.Keys.ToList();
		for (int i = 0; i < m_currencyRewardDisplays.Length; i++)
		{
			if (_data.currencyRewards.Keys.Count > i)
			{
				m_currencyRewardDisplays[i].Show();
				m_currencyRewardDisplays[i].Init(keys[i], _data.currencyRewards[keys[i]]);
			}
			else
				m_currencyRewardDisplays[i].Hide();
		}
	}

	private EntityEquipmentData.EntityFaction GetDominentSquadFaction ( List<UnitPreset> _units )
	{
		Dictionary<EntityEquipmentData.EntityFaction, int> count = new();
		foreach (UnitPreset unit in _units)
		{
			EntityEquipmentData.EntityFaction faction = unit.GetSavedData().GetDominentFaction();

			if (!count.ContainsKey(faction))
				count.Add(faction, 0);
			count[faction]++;
		}

		EntityEquipmentData.EntityFaction dominentFaction = EntityEquipmentData.EntityFaction.Noone;
		int biggestAmount = -1;
		foreach (EntityEquipmentData.EntityFaction faction in count.Keys)
		{
			if (count[faction] > biggestAmount)
			{
				dominentFaction = faction;
				biggestAmount = count[faction];
			}
		}

		return dominentFaction;
	}

	private void OnClickStartMission ()
	{
		MissionData missionData = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[CurrentRound]];
		if (missionData.preMissionDialogue != null)
			DialogueManager.Instance.PlayDialogue(missionData.preMissionDialogue, () => GameManager.Instance.SetupLevel(missionData));
		else
			GameManager.Instance.SetupLevel(missionData);
	}

	private void OnAnyUnitHovered ( UnitMissionDisplay _display )
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
