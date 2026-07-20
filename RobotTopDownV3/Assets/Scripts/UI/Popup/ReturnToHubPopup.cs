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
			GameDatas.PlayerSave.DayData.RepairingComponentData data = GameDatas.current.currentPlayerSave.dayData.repairingComponents[i];
			if (data != null && data.component != null && !string.IsNullOrEmpty(data.component.ID))
			{
				EntityEquipmentData componentData = data.component.GetData<EntityEquipmentData>();
				if (data.remainingTime <= 0)
				{
					content += componentData.displayName + " finished repairing\n";
					data.component.isDamaged = false;
					GameDatas.current.currentPlayerSave.equipmentInventory.Add(data.component);
					GameDatas.current.currentPlayerSave.dayData.repairingComponents[i] = null;
				}
			}
		}

		m_contentTMP.text = content;
	}

}
