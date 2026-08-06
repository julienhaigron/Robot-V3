using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class EndLevelPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_texte;
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
	}

	public void Init ( GameResult _gameResult, MissionData _missionData )
	{
		m_texte.text = _gameResult == GameResult.Win ? "Victory" : _gameResult == GameResult.Draw ? "Draw" : "Defeat";

		//damaged units
		for (int i = 0; i < m_unitDisplay.Length; i++)
		{
			if (i >= GameManager.Instance.PlayersEntityAnchor[0].Entities.Count)
				m_unitDisplay[i].Hide();
			else
			{
				Entity entity = GameManager.Instance.PlayersEntityAnchor[0].Entities[i];

				float remainingHealthPercentage = (float)entity.Equipment.CurrentHealth / (float)entity.Equipment.MaxHealth;
				int destroiedComponentAmount = remainingHealthPercentage > .5f ? 0 : remainingHealthPercentage > .25f ? 1 : remainingHealthPercentage > 0 ? 2 : 3;

				if (destroiedComponentAmount > 0)
					DamageRandomComponents(GameDatas.current.currentPlayerSave.squadUnits[i], destroiedComponentAmount);

				m_unitDisplay[i].Init(GameDatas.current.currentPlayerSave.squadUnits[i]);
				m_unitDisplay[i].Show();
			}
		}

		//rewards
		m_allocatedRewardPoint = _gameResult == GameResult.Win ? 5 : _gameResult == GameResult.Draw ? 3 : 2;

		for (int i = 0; i < m_rewardCurrencyDisplays.Length; i++)
		{
			if (_missionData.currencyRewards.Length > i)
			{
				m_rewardCurrencyDisplays[i].Show();
				m_rewardCurrencyDisplays[i].Init(_missionData.currencyRewards[i].type, _missionData.currencyRewards[i].amount, true, OnInterractWithRewardBtn);
				if (_gameResult == GameResult.Win)
					m_rewardCurrencyDisplays[i].SetIsSelected(true);
			}
			else
				m_rewardCurrencyDisplays[i].Hide();
		}

		for (int i = 0; i < m_rewardComponentDisplays.Length; i++)
		{
			if (_missionData.equipmentRewards.Count > i)
			{
				m_rewardComponentDisplays[i].Show();
				m_rewardComponentDisplays[i].Init(_missionData.equipmentRewards[i], OnInterractWithRewardBtn);
				if (_gameResult == GameResult.Win)
					m_rewardComponentDisplays[i].SetIsSelected(true);
			}
			else
				m_rewardComponentDisplays[i].Hide();
		}

		for (int i = 0; i < m_rewardUnitsDisplays.Length; i++)
		{
			if (_missionData.unitReward.Count > i)
			{
				m_rewardUnitsDisplays[i].Show();
				m_rewardUnitsDisplays[i].Init(_missionData.unitReward[i], OnInterractWithRewardBtn);
				if (_gameResult == GameResult.Win)
					m_rewardUnitsDisplays[i].SetIsSelected(true);
			}
			else
				m_rewardUnitsDisplays[i].Hide();
		}

	}

	private void OnInterractWithRewardBtn ()
	{
		bool canLeave = m_allocatedRewardPoint == 5;

		if (!canLeave)
		{
			int totalUsedRewardPoint = 0;
			foreach (CurrencyRewardDisplay display in m_rewardCurrencyDisplays)
				if (display.IsSelected)
					totalUsedRewardPoint += 1;

			foreach (ComponentRewardDisplay display in m_rewardComponentDisplays)
			{
				if (display.IsSelected)
				{
					switch (display.Component.GetEquipmentType())
					{
						case EntityEquipmentData.EquipmentType.Frame:
						case EntityEquipmentData.EquipmentType.Brain:
						case EntityEquipmentData.EquipmentType.Reactor:
						case EntityEquipmentData.EquipmentType.NeuronalMembrane:
							totalUsedRewardPoint += 3;
							break;
						default:
							totalUsedRewardPoint += 2;
							break;
					}
				}
			}

			foreach (UnitRewardDisplay display in m_rewardUnitsDisplays)
				if (display.IsSelected)
					totalUsedRewardPoint += 5;
		}

		m_continueButton.SetInteractability(canLeave);
	}

	public void DamageRandomComponents ( EntitySavedData _entity, int _count )
	{
		List<GameDatas.PlayerSave.Equipment> available = _entity.GetAllEquipments();
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
			if (display.IsSelected)
				GameDatas.current.currentPlayerSave.AddCurrency(display.CurrencyType, display.Value);

		foreach (ComponentRewardDisplay display in m_rewardComponentDisplays)
		{
			if (display.IsSelected)
				GameDatas.current.currentPlayerSave.AddEquipmentToInventory(display.Component);
		}

		foreach (UnitRewardDisplay display in m_rewardUnitsDisplays)
			if (display.IsSelected)
				display.UnitPreset.AddToUnits();

		GameManager.Instance.GoBackToHub();
		Close();
	}

}
