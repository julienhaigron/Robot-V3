using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class EndLevelPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_texte;
	[SerializeField] private TextMeshProUGUI m_rewardPointsTMP;
	[SerializeField] private BaseButton m_continueButton;
	[SerializeField] private CurrencyRewardDisplay[] m_rewardCurrencyDisplays;
	[SerializeField] private ComponentRewardDisplay[] m_rewardComponentDisplays;
	[SerializeField] private UnitRewardDisplay[] m_rewardUnitsDisplays;
	[SerializeField] private EndLevelEntityDisplay[] m_unitDisplay;
	[SerializeField] private BaseButton m_shortenFolderToggleBtn;
	[SerializeField] private CollapsibleFolder m_structureFolder;

	public enum GameResult { Win, Draw, Loose }

	private int m_allocatedRewardPoint;
	private readonly List<GameDatas.PlayerSave.Component> m_pendingDamagedComponents = new();

	private void Awake ()
	{
		m_continueButton.onClick += OnClickContinue;
		m_shortenFolderToggleBtn.onClick += OnClickShortenFolderToggle;
	}

	private void OnDestroy ()
	{
		m_continueButton.onClick -= OnClickContinue;
		LocalizationManager.onLanguageChanged -= RefreshResultLabel;
	}

	private GameResult m_gameResult;
	private MissionData m_missionData;

	private void RefreshResultLabel ()
	{
		m_texte.text = LocalizationManager.Instance.Get(m_gameResult == GameResult.Win ? LocalizationKey.endlevel_victory
			: m_gameResult == GameResult.Draw ? LocalizationKey.endlevel_draw : LocalizationKey.endlevel_defeat);
	}

	public void Init ( GameResult _gameResult, MissionData _missionData )
	{
		m_gameResult = _gameResult;
		m_missionData = _missionData;
		LocalizationManager.onLanguageChanged -= RefreshResultLabel;
		LocalizationManager.onLanguageChanged += RefreshResultLabel;
		RefreshResultLabel();

		//damaged units
		m_pendingDamagedComponents.Clear();
		for (int i = 0; i < m_unitDisplay.Length; i++)
		{
			if (i >= GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities.Count
				|| i >= GameDatas.current.currentPlayerSave.squadUnitsIndex.Count)
				m_unitDisplay[i].Hide();
			else
			{
				Entity entity = GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities[i];

				float remainingHealthPercentage = (float)entity.Equipment.CurrentHealth / (float)entity.Equipment.MaxHealth;
				int destroiedComponentAmount = remainingHealthPercentage > .75f ? 0 : remainingHealthPercentage > .5f ? 1 : remainingHealthPercentage > .25f ? 2 : 3;

				List<GameDatas.PlayerSave.Component> damagedComponent = new();
				if (destroiedComponentAmount > 0)
					damagedComponent = PickRandomComponents(GameDatas.current.currentPlayerSave.allBuiltUnits[GameDatas.current.currentPlayerSave.squadUnitsIndex[i]], destroiedComponentAmount);
				m_pendingDamagedComponents.AddRange(damagedComponent);

				m_unitDisplay[i].Init(GameDatas.current.currentPlayerSave.allBuiltUnits[GameDatas.current.currentPlayerSave.squadUnitsIndex[i]], damagedComponent);
				m_unitDisplay[i].Show();
			}
		}

		//rewards
		MissionData.RewardSet rewardSet = GetRewardSet(_gameResult, _missionData, out m_allocatedRewardPoint);
		bool isWin = _gameResult == GameResult.Win;

		for (int i = 0; i < m_rewardCurrencyDisplays.Length; i++)
		{
			if (rewardSet.creditAmounts.Count > i)
			{
				m_rewardCurrencyDisplays[i].Show();
				m_rewardCurrencyDisplays[i].Init(CurrencyType.SoftCurrency, rewardSet.creditAmounts[i], true, OnInterractWithRewardBtn);
				m_rewardCurrencyDisplays[i].SetIsSelected(isWin);
			}
			else
				m_rewardCurrencyDisplays[i].Hide();
		}

		for (int i = 0; i < m_rewardComponentDisplays.Length; i++)
		{
			int blueprintIndex = i - rewardSet.components.Count;
			if (rewardSet.components.Count > i || rewardSet.blueprints.Count > blueprintIndex)
			{
				bool isBlueprint = blueprintIndex >= 0;
				m_rewardComponentDisplays[i].Show();
				m_rewardComponentDisplays[i].Init(isBlueprint ? rewardSet.blueprints[blueprintIndex] : rewardSet.components[i], OnInterractWithRewardBtn, false, isBlueprint);
				m_rewardComponentDisplays[i].SetIsSelected(isWin);
			}
			else
				m_rewardComponentDisplays[i].Hide();
		}

		for (int i = 0; i < m_rewardUnitsDisplays.Length; i++)
		{
			if (rewardSet.units.Count > i)
			{
				m_rewardUnitsDisplays[i].Show();
				m_rewardUnitsDisplays[i].Init(rewardSet.units[i], OnInterractWithRewardBtn);
				m_rewardUnitsDisplays[i].SetIsSelected(isWin);
			}
			else
				m_rewardUnitsDisplays[i].Hide();
		}

		OnInterractWithRewardBtn();

	}

	private static MissionData.RewardSet GetRewardSet ( GameResult _gameResult, MissionData _missionData, out int _allocatedRewardPoint )
	{
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		bool isWin = _gameResult == GameResult.Win;

		if (!save.IsInTournament)
		{
			MissionData.RewardSet rewardSet = _missionData.GetRewardSet();
			_allocatedRewardPoint = isWin
				? rewardSet.GetTotalPoints()
				: _gameResult == GameResult.Draw ? MissionData.drawRewardPoints : MissionData.defeatRewardPoints;
			return rewardSet;
		}

		int round = save.TournamentRound;
		bool isFinal = round >= 2;
		MissionData.RewardSet tournamentReward = save.cycleData.GetTournamentRewardSet();
		if (isWin)
			_allocatedRewardPoint = isFinal ? tournamentReward.GetTotalPoints() : 0;
		else
			_allocatedRewardPoint = isFinal ? MissionData.tournamentFinalLossRewardPoints
				: round == 1 ? MissionData.tournamentSecondMatchLossRewardPoints : 0;

		return _allocatedRewardPoint > 0 ? tournamentReward : new MissionData.RewardSet();
	}

	private void OnInterractWithRewardBtn ()
	{
		int totalUsedRewardPoint = 0;
		foreach (CurrencyRewardDisplay display in m_rewardCurrencyDisplays)
			if (display.IsSelected && display.IsVisible)
				totalUsedRewardPoint += MissionData.creditPointValue;

		foreach (ComponentRewardDisplay display in m_rewardComponentDisplays)
		{
			if (display.IsSelected && display.IsVisible)
			{
				switch (display.Component.GetEquipmentType())
				{
					case EntityEquipmentData.EquipmentType.Frame:
					case EntityEquipmentData.EquipmentType.Brain:
					case EntityEquipmentData.EquipmentType.Reactor:
					case EntityEquipmentData.EquipmentType.NeuronalMembrane:
						totalUsedRewardPoint += MissionData.mainComponentPointValue;
						break;
					default:
						totalUsedRewardPoint += MissionData.secondaryComponentPointValue;
						break;
				}
			}
		}

		foreach (UnitRewardDisplay display in m_rewardUnitsDisplays)
			if (display.IsSelected && display.IsVisible)
				totalUsedRewardPoint += MissionData.unitPointValue;

		//m_rewardPointsTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.endlevel_reward_points), m_allocatedRewardPoint - totalUsedRewardPoint);
		m_rewardPointsTMP.text = (m_allocatedRewardPoint - totalUsedRewardPoint).ToString();

		m_continueButton.SetInteractability(m_allocatedRewardPoint == 5 || totalUsedRewardPoint <= m_allocatedRewardPoint);
	}

	public List<GameDatas.PlayerSave.Component> PickRandomComponents ( EntitySavedData _entity, int _count )
	{
		List<GameDatas.PlayerSave.Component> available = _entity.GetAllEquipments();
		List<GameDatas.PlayerSave.Component> pickedComponents = new();
		_count = Mathf.Min(_count, available.Count);

		for (int i = 0; i < _count; i++)
		{
			int index = UnityEngine.Random.Range(0, available.Count);
			pickedComponents.Add(available[index]);
			available.RemoveAt(index);
		}

		return pickedComponents;
	}

	private void OnClickContinue ()
	{
		foreach (ComponentRewardDisplay display in m_rewardComponentDisplays)
		{
			if (display.IsSelected && display.IsVisible && display.IsBlueprint)
				GameDatas.current.currentPlayerSave.UnlockBlueprint(display.Component.name);
		}

		GameManager.Instance.ApplyMatchProgression(m_gameResult);

		foreach (GameDatas.PlayerSave.Component component in m_pendingDamagedComponents)
			component.isDamaged = true;
		m_pendingDamagedComponents.Clear();

		//give rewards
		foreach (CurrencyRewardDisplay display in m_rewardCurrencyDisplays)
			if (display.IsSelected && display.IsVisible)
				GameDatas.current.currentPlayerSave.AddCurrency(display.CurrencyType, display.Value);

		foreach (ComponentRewardDisplay display in m_rewardComponentDisplays)
		{
			if (display.IsSelected && display.IsVisible && !display.IsBlueprint)
				GameDatas.current.currentPlayerSave.AddComponentToInventory(display.Component);
		}

		foreach (UnitRewardDisplay display in m_rewardUnitsDisplays)
			if (display.IsSelected && display.IsVisible)
				display.UnitPreset.AddToUnits(false);

		if (m_missionData != null)
		{
			m_missionData.ClearRewardSet();
			m_missionData = null;
		}

		GameManager.Instance.GoBackToHub();
		Close();
	}

	private void OnClickShortenFolderToggle ()
	{
		m_structureFolder.Toggle();
	}

}
