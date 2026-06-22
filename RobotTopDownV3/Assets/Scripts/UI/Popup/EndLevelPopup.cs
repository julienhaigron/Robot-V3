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

		//TODO: display squad entities dead or alive status
		//+ register destroied stuff

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

	private void OnClickContinue ()
	{
		Close();
		GameManager.Instance.GoBackToHub();
	}

}
