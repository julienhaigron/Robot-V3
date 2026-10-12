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
	[SerializeField] private HangarEntityDisplay m_hoveredUnitDisplay;
	[SerializeField] private StatDisplay[] m_hoveredUnitStatDisplays;

	[Title("Other Squads")]
	[SerializeField] private Image[] m_cursors;
	[SerializeField] private UnitMissionDisplay[] m_round1SquadUnits;
	[SerializeField] private Image m_squadIcon;

	[Title("Enemy")]
	[SerializeField] private HangarEntityDisplay m_hoveredEnemyUnitDisplay;
	[SerializeField] private StatDisplay[] m_hoveredEnemyUnitStatDisplays;
	[SerializeField] private ComponentFullDisplay m_hoveredEnemyComponentDisplay;

	[Title("Rewards")]
	[SerializeField] private ComponentRewardDisplay[] m_componentRewardDisplays;
	[SerializeField] private CurrencyRewardDisplay[] m_currencyRewardDisplays;
	[SerializeField] private UnitRewardDisplay[] m_unitRewardDisplays;

	private int CurrentRound => GameDatas.current.currentPlayerSave.dayCount - 4;

	private void Awake ()
	{
		UnitMissionDisplay.onAnyUnitHovered += OnAnyUnitHovered;
		m_startMissionBtn.onClick += OnClickStartMission;
		m_hoveredEnemyUnitDisplay.onComponentHovered += OnEnemyComponentHovered;
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

		HideSquadDisplays(m_round1SquadUnits);

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

		if (CurrentRound == 0)
		{
			MissionData missiondata = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[0]];
			for (int i = 0; i < m_round1SquadUnits.Length; i++)
			{
				if (missiondata.enemies.Length > i)
				{
					m_round1SquadUnits[i].Init(missiondata.enemies[i].GetSavedData(), -1, false);
					m_round1SquadUnits[i].Show();
				}
				else
					m_round1SquadUnits[i].Hide();
			}

			EntityEquipmentData.EntityFaction dominentCorpo = GetDominentSquadFaction(missiondata.enemies.ToList());
			m_squadIcon.gameObject.SetActive(true);
			m_squadIcon.sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
			m_squadIcon.color = GameAssets.current.ui.corporationsColors[dominentCorpo];
		}
		else if (CurrentRound == 1)
		{
			MissionData missiondata = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[1]];
			for (int i = 0; i < m_round1SquadUnits.Length; i++)
			{
				if (missiondata.enemies.Length > i)
				{
					m_round1SquadUnits[i].Init(missiondata.enemies[i].GetSavedData(), -1, false);
					m_round1SquadUnits[i].Show();
				}
				else
					m_round1SquadUnits[i].Hide();
			}
			EntityEquipmentData.EntityFaction dominentCorpo = GetDominentSquadFaction(missiondata.enemies.ToList());
			m_squadIcon.gameObject.SetActive(true);
			m_squadIcon.sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
			m_squadIcon.color = GameAssets.current.ui.corporationsColors[dominentCorpo];
		}
		else if (CurrentRound == 2)
		{
			MissionData missiondata = GameAssets.current.game.missions[GameDatas.current.currentPlayerSave.cycleData.roundsDatas[2]];
			for (int i = 0; i < m_round1SquadUnits.Length; i++)
			{
				if (missiondata.enemies.Length > i)
				{
					m_round1SquadUnits[i].Init(missiondata.enemies[i].GetSavedData(), -1, false);
					m_round1SquadUnits[i].Show();
				}
				else
					m_round1SquadUnits[i].Hide();
			}
			EntityEquipmentData.EntityFaction dominentCorpo = GetDominentSquadFaction(missiondata.enemies.ToList());
			m_squadIcon.gameObject.SetActive(true);
			m_squadIcon.sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
			m_squadIcon.color = GameAssets.current.ui.corporationsColors[dominentCorpo];
		}

		m_startMissionBtn.SetInteractability(GameDatas.current.currentPlayerSave.squadUnitsIndex.Count > 0);

		OnAnyUnitHovered(m_unitDisplays[0]);

		UnitMissionDisplay firstEnemy = m_round1SquadUnits.FirstOrDefault(display => display != null && display.gameObject.activeSelf);
		if (firstEnemy != null)
			OnAnyUnitHovered(firstEnemy);
	}

	private void HideSquadDisplays ( UnitMissionDisplay[] _displays )
	{
		if (_displays == null)
			return;

		foreach (UnitMissionDisplay display in _displays)
			if (display != null)
				display.Hide();
	}

	private void SetCurrentMatchInfo ( MissionData _data )
	{
		MissionData.RewardSet rewardSet = _data.GetRewardSet();

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

	private EntityEquipmentData.EntityFaction GetDominentSquadFaction ( List<UnitPreset> _units )
	{
		Dictionary<EntityEquipmentData.EntityFaction, int> count = new();
		foreach (UnitPreset unit in _units)
		{
			EntityEquipmentData.EntityFaction faction = unit.GetSavedData().GetDominentFaction(out float percentage);

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
		if (GameDatas.current.currentPlayerSave.squadUnitsIndex.Count == 0)
			return;

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

		if (m_round1SquadUnits.Contains(_display))
		{
			m_hoveredEnemyUnitDisplay.Init(_display.Data, -1, true, true);
			InitStatDisplays(m_hoveredEnemyUnitStatDisplays, _display.Data);
			m_hoveredEnemyComponentDisplay.Init(null);
		}
		else if (m_unitDisplays.Contains(_display))
		{
			m_hoveredUnitDisplay.Init(_display.Data, _display.Index, false);
			InitStatDisplays(m_hoveredUnitStatDisplays, _display.Data);
		}
	}

	private void OnEnemyComponentHovered ( GameDatas.PlayerSave.Component _component )
	{
		m_hoveredEnemyComponentDisplay.Init(_component);
	}

	private static void InitStatDisplays ( StatDisplay[] _statDisplays, EntitySavedData _data )
	{
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDescriptions = _data.GetStatsDesciptions();
		List<EntityEquipmentData.SecondaryStat.StatType> keys = statsDescriptions.Keys.ToList();
		for (int i = 0; i < _statDisplays.Length; i++)
		{
			if (keys.Count <= i)
				_statDisplays[i].gameObject.SetActive(false);
			else
			{
				_statDisplays[i].gameObject.SetActive(true);
				_statDisplays[i].Init(statsDescriptions[keys[i]]);
			}
		}
	}
}
