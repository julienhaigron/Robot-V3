using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using MPUIKIT;

public class HubTopCanvas : AUITopCanvas
{
    [Title("Depedencies")]
    [SerializeField] private BaseButton m_upgradeStructureBtn;
    [SerializeField] private BaseButton m_returnBtn;
    [SerializeField] private SerializableDictionary<CurrencyType, CurrencyDisplay> m_currencyDisplays = new();
	[SerializeField] private GameObject[] m_dayDisplays;
	[SerializeField] private TextMeshProUGUI m_cycleTMP;

	[Title("Shop")]
	[SerializeField] private CurrencyDisplay m_shopFactionCurrencyDisplay;
	[SerializeField] private TextMeshProUGUI m_factionProgressionTMP;
	[SerializeField] private MPImage m_factionProgressionFill;

	protected override void Awake ()
	{
		base.Awake();
		GameDatas.onCurrencyChanged += OnCurrencyChange;
		GameDatas.onNewDay += OnNewDay;
		m_upgradeStructureBtn.onClick += OnClickUpgradeStructure;
		m_returnBtn.onClick += OnClickReturn;
		UIManager.onFocusedWindowChanged += OnFocusedWindowChanged;
	}

	#region Callbacks

	private void OnFocusedWindowChanged()
	{
		if (!m_visible)
			return;

		Init(UIManager.Instance.currentPanel is ShopPanel);
	}

	private void OnCurrencyChange(CurrencyType _type )
	{
		if (!m_currencyDisplays.ContainsKey(_type))
			return;

		switch (_type)
		{
			case CurrencyType.SoftCurrency:
				m_currencyDisplays[_type].Text.text = GameDatas.current.currentPlayerSave.currencies[_type].ToString() + " -";
				break;
			case CurrencyType.PsyCredit:
			case CurrencyType.CommandoCredit:
			case CurrencyType.PaladinCredit:
				//int percentage = 0;
				m_currencyDisplays[_type].Text.text = /*percentage + " % - " + */GameDatas.current.currentPlayerSave.currencies[_type].ToString() + "r";
				break;
		}
	}

	private void OnNewDay ()
	{
		RefreshDayDisplay();
	}

	private void OnClickUpgradeStructure ()
	{
		if(UIManager.Instance.currentPanel is HangarPanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Hangar);
		else if(UIManager.Instance.currentPanel is ShopPanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Shop);
		else if (UIManager.Instance.currentPanel is RecyclePanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Recycler);
		else if (UIManager.Instance.currentPanel is RepairStationPanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.RepairStation);
	}

	private void OnClickReturn ()
	{
		if (UIManager.Instance.currentPanel is SelectMissionPanel)
		{
			if(GameDatas.current.currentPlayerSave.cycleData.didSelectMissions)
				UIManager.Instance.OpenPanel<SoloHubPanel>();

			//wait for player to selected required minimum mission amount
		}
		else if(UIManager.Instance.currentPanel is HangarPanel or ShopPanel or RecyclePanel or RepairStationPanel or TournamentPanel or MissionPanel)
		{
			UIManager.Instance.OpenPanel<SoloHubPanel>();
		}
		else if(UIManager.Instance.currentPanel is EntityConfigPanel entityConfigPanel)
		{
			if (entityConfigPanel.DoesComeFromMissionPanel)
			{
				if (GameDatas.current.currentPlayerSave.dayCount <= 3)
					UIManager.Instance.OpenPanel<MissionPanel>();
				else
					UIManager.Instance.OpenPanel<TournamentPanel>();
			}
			else
				UIManager.Instance.OpenPanel<HangarPanel>();
		}
		else if (UIManager.Instance.currentPanel is SoloHubPanel)
		{
			UIManager.Instance.ClosePanel<SoloHubPanel>();
			GameManager.Instance.GoToStartScreen();
		}
	}

	#endregion

	private void Init(bool _isInShop )
	{
		// show/hide unneeded currency displays
		if (_isInShop)
		{
			foreach (KeyValuePair<CurrencyType, CurrencyDisplay> displayPair in m_currencyDisplays)
				if(displayPair.Key != CurrencyType.SoftCurrency)
					displayPair.Value.Hide();

			EntityEquipmentData.EntityFaction selectedFaction = UIManager.Instance.GetPanel<ShopPanel>().CurrentFaction;

			switch (selectedFaction)
			{
				case EntityEquipmentData.EntityFaction.Psy:
					m_shopFactionCurrencyDisplay.Init(CurrencyType.PsyCredit, GameDatas.current.currentPlayerSave.currencies[CurrencyType.PsyCredit].ToString(), true);
					break;
				case EntityEquipmentData.EntityFaction.Paladin:
					m_shopFactionCurrencyDisplay.Init(CurrencyType.PaladinCredit, GameDatas.current.currentPlayerSave.currencies[CurrencyType.PaladinCredit].ToString(), true);
					break;
				case EntityEquipmentData.EntityFaction.Commando:
					m_shopFactionCurrencyDisplay.Init(CurrencyType.CommandoCredit, GameDatas.current.currentPlayerSave.currencies[CurrencyType.CommandoCredit].ToString(), true);
					break;
			}
			m_upgradeStructureBtn.gameObject.SetActive(true);
			m_shopFactionCurrencyDisplay.Show();
			/*int percentage = 0;
			m_factionProgressionTMP.text = percentage + " %";
			m_factionProgressionFill.fillAmount = (float)percentage / 100f;*/
		}
		else
		{
			foreach (CurrencyDisplay display in m_currencyDisplays.Values)
				display.Show();
			m_shopFactionCurrencyDisplay.Hide();

			m_upgradeStructureBtn.gameObject.SetActive(UIManager.Instance.currentPanel is not TournamentPanel or MissionPanel);
			m_currencyDisplays[CurrencyType.SoftCurrency].Text.text = GameDatas.current.currentPlayerSave.currencies[CurrencyType.SoftCurrency].ToString();

			//int percentage = 0;
			m_currencyDisplays[CurrencyType.CommandoCredit].Text.text = /*percentage + " % - " + */GameDatas.current.currentPlayerSave.currencies[CurrencyType.CommandoCredit].ToString() + "r";
			m_currencyDisplays[CurrencyType.PaladinCredit].Text.text = /*percentage + " % - " + */GameDatas.current.currentPlayerSave.currencies[CurrencyType.PaladinCredit].ToString() + "r";
			m_currencyDisplays[CurrencyType.PsyCredit].Text.text = /*percentage + " % - " + */GameDatas.current.currentPlayerSave.currencies[CurrencyType.PsyCredit].ToString() + "r";
		}

		RefreshDayDisplay();
	}

	private void RefreshDayDisplay ()
	{
		m_cycleTMP.text = "CYCLE " + (GameDatas.current.currentPlayerSave.cycleCount + 1);

		for (int i = 0; i < m_dayDisplays.Length; i++)
		{
			m_dayDisplays[i].SetActive(GameDatas.current.currentPlayerSave.dayCount >= i);
		}
	}

}
