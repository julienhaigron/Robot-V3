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

	public enum GameResult { Win, Draw, Loose }

	private int m_allocatedRewardPoint;

	private void Awake ()
	{
		m_continueButton.onClick += OnClickContinue;
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
		for (int i = 0; i < m_unitDisplay.Length; i++)
		{
			if (i >= GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities.Count)
				m_unitDisplay[i].Hide();
			else
			{
				Entity entity = GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities[i];

				float remainingHealthPercentage = (float)entity.Equipment.CurrentHealth / (float)entity.Equipment.MaxHealth;
				int destroiedComponentAmount = remainingHealthPercentage > .75f ? 0 : remainingHealthPercentage > .5f ? 1 : remainingHealthPercentage > .25f ? 2 : 3;

				if (destroiedComponentAmount > 0)
					DamageRandomComponents(GameDatas.current.currentPlayerSave.allBuiltUnits[GameDatas.current.currentPlayerSave.squadUnitsIndex[i]], destroiedComponentAmount);

				m_unitDisplay[i].Init(GameDatas.current.currentPlayerSave.allBuiltUnits[GameDatas.current.currentPlayerSave.squadUnitsIndex[i]]);
				m_unitDisplay[i].Show();
			}
		}

		//rewards
		MissionData.RewardSet rewardSet = _missionData.GetRewardSet();
		bool isWin = _gameResult == GameResult.Win;
		m_allocatedRewardPoint = isWin
			? rewardSet.GetTotalPoints()
			: _gameResult == GameResult.Draw ? MissionData.drawRewardPoints : MissionData.defeatRewardPoints;

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
			if (rewardSet.components.Count > i)
			{
				m_rewardComponentDisplays[i].Show();
				m_rewardComponentDisplays[i].Init(rewardSet.components[i], OnInterractWithRewardBtn);
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

		m_rewardPointsTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.endlevel_reward_points), m_allocatedRewardPoint - totalUsedRewardPoint);

		m_continueButton.SetInteractability(m_allocatedRewardPoint == 5 || totalUsedRewardPoint <= m_allocatedRewardPoint);
	}

	public void DamageRandomComponents ( EntitySavedData _entity, int _count )
	{
		List<GameDatas.PlayerSave.Component> available = _entity.GetAllEquipments();
		_count = Mathf.Min(_count, available.Count);

		for (int i = 0; i < _count; i++)
		{
			int index = UnityEngine.Random.Range(0, available.Count);
			available[index].isDamaged = true;
			available.RemoveAt(index);
		}
	}

	private void OnClickContinue ()
	{
		//give rewards
		foreach (CurrencyRewardDisplay display in m_rewardCurrencyDisplays)
			if (display.IsSelected && display.IsVisible)
				GameDatas.current.currentPlayerSave.AddCurrency(display.CurrencyType, display.Value);

		foreach (ComponentRewardDisplay display in m_rewardComponentDisplays)
		{
			if (display.IsSelected && display.IsVisible)
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

}
