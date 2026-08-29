using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReturnToHubPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private TextMeshProUGUI m_contentTMP;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
	}

	private void OnClickClose ()
	{
		UIManager.Instance.GetPanel<SoloHubPanel>().RefreshVisual();
		Close();
	}

	public void Init ()
	{
		string content = "content:\n";
		for (int i = 0; i < GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents.Count; i++)
		{
			GameDatas.PlayerSave.DayData.RecyclingComponentData data = GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents[i];
			if (data != null && data.component != null && !string.IsNullOrEmpty(data.component.ID))
			{
				EntityEquipmentData componentData = data.component.GetData<EntityEquipmentData>();
				if (data.remainingTime <= 0)
				{
					content += componentData.displayName + " finished recycling \n";
					System.Tuple<CurrencyType, ulong> sellingPrice = componentData.GetSellingPrice();
					GameDatas.current.currentPlayerSave.AddCurrency(sellingPrice.Item1, sellingPrice.Item2);
					GameDatas.current.currentPlayerSave.dayData.currentlyRecyclingComponents[i] = null;
				}
			}
		}

		for (int i = 0; i < GameDatas.current.currentPlayerSave.dayData.repairingComponents.Count; i++)
		{
			GameDatas.PlayerSave.DayData.RepairingUnitData data = GameDatas.current.currentPlayerSave.dayData.repairingComponents[i];
			if (data != null && data.unit != null && !string.IsNullOrEmpty(data.unit.name))
			{
				//EntityEquipmentData componentData = data.unit.GetData<EntityEquipmentData>();
				if (data.remainingTime <= 0)
				{
					content += data.unit.name + " finished repairing\n";
					foreach(GameDatas.PlayerSave.Component eq in data.unit.GetAllEquipments())
					{
						eq.isDamaged = false;
					}
					data.unit.isRepairing = false;
					GameDatas.current.currentPlayerSave.dayData.repairingComponents[i] = null;
				}
			}
		}

		m_contentTMP.text = content;
	}

}
