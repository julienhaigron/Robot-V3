using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndLevelPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_texte;
	[SerializeField] private BaseButton m_continueButton;

	private void Awake ()
	{
		m_continueButton.onClick += OnClickContinue;
	}

	private void OnDestroy ()
	{
		m_continueButton.onClick -= OnClickContinue;
	}

	/*protected override void OnShowFinished ()
	{

	}*/

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}

	public void Init ( bool _didWin, MissionData _missionData )
	{
		m_texte.text = _didWin ? "You win" : "You loose";

		for (int i = 0; i < GameManager.Instance.PlayersEntityAnchor[0].Entities.Count; i++)
		{
			//TODO: display squad entities dead or alive status
			Entity entity = GameManager.Instance.PlayersEntityAnchor[0].Entities[i];

			//Quand un unité arrive à 50%, 25% et 0% hp, un, deux ou trois de ses slots tiré au hasard sont cassés.
			float remainingHealthPercentage = (float)entity.Equipment.CurrentHealth / (float)entity.Equipment.MaxHealth;
			int destroiedComponentAmount = remainingHealthPercentage > .5f ? 0 : remainingHealthPercentage > .25f ? 1 : remainingHealthPercentage > 0 ? 2 : 3;

			if (destroiedComponentAmount > 0)
				DamageRandomComponents(GameDatas.current.currentPlayerSave.squadUnits[i], destroiedComponentAmount);
		}

		//TODO : display rewards
		if (_didWin)
		{
			if (!_missionData.areRewardsRandom)
			{
				foreach (CurrencyType currencyType in _missionData.currencyRewards.Keys)
				{
					GameDatas.current.currentPlayerSave.AddCurrency(currencyType, _missionData.currencyRewards[currencyType]);
				}

				foreach (EntityEquipmentData equipmentData in _missionData.equipmentRewards)
				{
					GameDatas.current.currentPlayerSave.AddEquipmentToInventory(equipmentData);
				}
			}
			else
			{
				//TODO : design rng rules
			}
		}
	}

	public void DamageRandomComponents ( EntitySavedData _entity, int _count )
	{
		Debug.Log(_count + " sub component destroyed");
		List<GameDatas.PlayerSave.Equipment> available = _entity.GetAllSubEquipments();
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
		Close();
		GameManager.Instance.GoBackToHub();
	}

}
