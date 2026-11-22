using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using MPUIKIT;
using DG.Tweening;

public class HubTopCanvas : AUITopCanvas
{
	[Title("Depedencies")]
	[SerializeField] private BaseButton m_upgradeStructureBtn;
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private SerializableDictionary<CurrencyType, CurrencyDisplay> m_currencyDisplays = new();
	[SerializeField] private GameObject[] m_dayDisplays;
	[SerializeField] private TextMeshProUGUI m_cycleTMP;
	[SerializeField] private Image m_returnBtnIMG;
	[SerializeField] private Sprite m_defaultReturnIcon;
	[SerializeField] private Sprite m_returnToStartMenuIcon;
	[SerializeField] private StructureShortcutsFolder m_structureShortcutsFolder;

	[Title("Shop")]
	[SerializeField] private CurrencyDisplay m_shopFactionCurrencyDisplay;

	[Title("Currency feedback")]
	[SerializeField] private float m_currencyRollDuration = .45f;
	[SerializeField] private float m_currencyBumpScale = .15f;

	private bool m_isStructureUpgradePopupOpened = false;
	private CurrencyType m_shopCurrency = CurrencyType.Unknown;
	private readonly Dictionary<CurrencyType, ulong> m_shownCurrencies = new();
	private readonly Dictionary<CurrencyType, Tween> m_currencyRolls = new();
	private readonly Dictionary<CurrencyType, Tween> m_currencyBumps = new();
	private readonly HashSet<CurrencyType> m_deferredCurrencies = new();

	protected override void Awake ()
	{
		base.Awake();
		GameDatas.onCurrencyChanged += OnCurrencyChange;
		GameDatas.onNewDay += OnNewDay;
		GameDatas.onNewCycle += OnNewCycle;
		m_upgradeStructureBtn.onClick += OnClickUpgradeStructure;
		m_returnBtn.onClick += OnClickReturn;
		EntityConfigPanel.onConfigChanged += RefreshReturnBtnLabel;
		UIManager.onFocusedWindowChanged += RefreshReturnBtnLabel;
		UIManager.onFocusedWindowChanged += OnFocusedWindowChanged;
		ShopPanel.onShopInitialized += OnShopInitialized;
	}

	#region Callbacks

	private void OnShopInitialized ( ShopPanel _shop )
	{
		if (m_visible && UIManager.Instance.currentPanel == _shop)
			Init(true, _shop);
	}

	private void OnFocusedWindowChanged ()
	{
		if (!m_visible)
			return;

		if (m_isStructureUpgradePopupOpened)
		{
			UIManager.Instance.ClosePopup<StructureUpgradePopup>();
			m_isStructureUpgradePopupOpened = false;
		}

		if (UIManager.Instance.currentPanel is ShopPanel shop)
		{
			Init(true, shop);
			m_structureShortcutsFolder.SetVisibility(true);
		}
		else
		{
			if(UIManager.Instance.currentPanel is RepairStationPanel or RecyclePanel or SoloHubPanel)
				m_structureShortcutsFolder.SetVisibility(true);
			else
				m_structureShortcutsFolder.SetVisibility(false);
			Init(false, null);
		}
	}

	private void OnCurrencyChange ( CurrencyType _type )
	{
		if (m_currencyDisplays.ContainsKey(_type) && !m_deferredCurrencies.Contains(_type))
			RollCurrency(_type, false);
	}

	public RectTransform GetCurrencyAnchor ( CurrencyType _type )
	{
		CurrencyDisplay display = GetShownDisplay(_type);
		return display != null ? (RectTransform)display.transform : null;
	}

	private CurrencyDisplay GetShownDisplay ( CurrencyType _type )
	{
		if (_type == m_shopCurrency && m_shopFactionCurrencyDisplay.gameObject.activeInHierarchy)
			return m_shopFactionCurrencyDisplay;

		return m_currencyDisplays.TryGetValue(_type, out CurrencyDisplay display) && display.gameObject.activeInHierarchy ? display : null;
	}

	public void DeferRoll ( CurrencyType _type )
	{
		m_deferredCurrencies.Add(_type);
	}

	public void SettleCurrency ( CurrencyType _type, bool _bump )
	{
		m_deferredCurrencies.Remove(_type);
		if (m_currencyDisplays.ContainsKey(_type))
			RollCurrency(_type, _bump);
	}

	private void RollCurrency ( CurrencyType _type, bool _bump )
	{
		CurrencyDisplay display = GetShownDisplay(_type);
		ulong target = GameDatas.current.currentPlayerSave.currencies[_type];
		ulong shown = m_shownCurrencies.TryGetValue(_type, out ulong value) ? value : target;

		if (m_currencyRolls.TryGetValue(_type, out Tween roll))
			roll?.Kill();

		if (!m_visible || display == null || shown == target)
		{
			SetCurrencyText(_type, target);
			return;
		}

		m_currencyRolls[_type] = DOVirtual.Float(shown, target, m_currencyRollDuration, _value => SetCurrencyText(_type, (ulong)Mathf.Round(_value)))
			.SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => SetCurrencyText(_type, target));

		if (!_bump)
			return;

		if (m_currencyBumps.TryGetValue(_type, out Tween bump))
			bump?.Kill(true);
		m_currencyBumps[_type] = display.transform.DOPunchScale(Vector3.one * m_currencyBumpScale, .35f, 6, 1f).SetUpdate(true);
	}

	private void SetCurrencyText ( CurrencyType _type, ulong _value )
	{
		m_shownCurrencies[_type] = _value;
		m_currencyDisplays[_type].Text.text = _type == CurrencyType.SoftCurrency ? _value.ToString() : _value + "r";
		if (_type == m_shopCurrency)
			m_shopFactionCurrencyDisplay.Init(_type, _value.ToString(), true);
	}

	private void OnNewDay ()
	{
		RefreshDayDisplay();
	}

	private void OnNewCycle ()
	{
		RefreshDayDisplay();
	}

	private void OnClickUpgradeStructure ()
	{
		if (m_isStructureUpgradePopupOpened)
		{
			UIManager.Instance.ClosePopup<StructureUpgradePopup>();
			m_isStructureUpgradePopupOpened = false;
			return;
		}
		else if (UIManager.Instance.currentPanel is HangarPanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Hangar);
		else if (UIManager.Instance.currentPanel is ShopPanel shop)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(GetShopStructureType(shop.CurrentFaction));
		else if (UIManager.Instance.currentPanel is RecyclePanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Recycler);
		else if (UIManager.Instance.currentPanel is RepairStationPanel)
			UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.RepairStation);

		m_isStructureUpgradePopupOpened = true;
	}

	private void OnClickReturn ()
	{
		if (UIManager.Instance.currentPanel is SelectMissionPanel)
		{
			if (GameDatas.current.currentPlayerSave.cycleData.didSelectMissions)
				UIManager.Instance.OpenPanel<SoloHubPanel>();

			//wait for player to selected required minimum mission amount
		}
		else if (UIManager.Instance.currentPanel is HangarPanel or ShopPanel or RecyclePanel or RepairStationPanel or TournamentPanel or MissionPanel)
		{
			UIManager.Instance.OpenPanel<SoloHubPanel>();
		}
		else if (UIManager.Instance.currentPanel is EntityConfigPanel entityConfigPanel)
		{
			if (entityConfigPanel.IsNewUnit)
				entityConfigPanel.DiscardUnit();

			if (entityConfigPanel.DoesComeFromMissionPanel)
			{
				if (entityConfigPanel.DoesComeFromTournamentPanel)
					UIManager.Instance.OpenPanel<TournamentPanel>();
				else
					UIManager.Instance.OpenPanel<MissionPanel>();
			}
			else
				UIManager.Instance.OpenPanel<HangarPanel>();
		}
		else if (UIManager.Instance.currentPanel is SoloHubPanel)
		{
			UIManager.Instance.ClosePanel<SoloHubPanel>();
			GameManager.Instance.GoToStartScreen();
		}

		if (m_isStructureUpgradePopupOpened)
		{
			UIManager.Instance.ClosePopup<StructureUpgradePopup>();
			m_isStructureUpgradePopupOpened = false;
		}
	}

	#endregion

	private static StructureUpgradePopup.StructureType GetShopStructureType ( EntityEquipmentData.EntityFaction _faction )
	{
		switch (_faction)
		{
			case EntityEquipmentData.EntityFaction.Psy:
				return StructureUpgradePopup.StructureType.PsyShop;
			case EntityEquipmentData.EntityFaction.Commando:
				return StructureUpgradePopup.StructureType.CommandoShop;
			default:
				return StructureUpgradePopup.StructureType.PaladinShop;
		}
	}

	private void RefreshReturnBtnLabel ()
	{
		if (UIManager.Instance.currentPanel is not EntityConfigPanel configPanel)
		{
			m_returnBtn.SetLabel(null);
			return;
		}

		m_returnBtn.SetLabel(LocalizationManager.Instance.Get(configPanel.IsCurrentUnitValid && !configPanel.IsNewUnit
			? LocalizationKey.entity_config_save_and_quit
			: LocalizationKey.entity_config_cancel));
	}

	private void Init ( bool _isInShop, ShopPanel _shopPanel )
	{
		m_returnBtnIMG.sprite = UIManager.Instance.currentPanel is SoloHubPanel ? m_returnToStartMenuIcon : m_defaultReturnIcon;
		// show/hide unneeded currency displays
		if (_isInShop)
		{
			foreach (KeyValuePair<CurrencyType, CurrencyDisplay> displayPair in m_currencyDisplays)
				if (displayPair.Key != CurrencyType.SoftCurrency)
					displayPair.Value.Hide();

			m_upgradeStructureBtn.gameObject.SetActive(true);
			m_shopFactionCurrencyDisplay.Show();
			m_shopCurrency = _shopPanel.AssociatedCurrency;
			SetCurrencyText(m_shopCurrency, GameDatas.current.currentPlayerSave.currencies[m_shopCurrency]);
			/*int percentage = 0;
			m_factionProgressionTMP.text = percentage + " %";
			m_factionProgressionFill.fillAmount = (float)percentage / 100f;*/
		}
		else
		{
			m_shopCurrency = CurrencyType.Unknown;
			foreach (CurrencyDisplay display in m_currencyDisplays.Values)
				display.Show();
			m_shopFactionCurrencyDisplay.Hide();

			if(UIManager.Instance.currentPanel is RecyclePanel or RepairStationPanel or HangarPanel)
				m_upgradeStructureBtn.gameObject.SetActive(true);
			else
				m_upgradeStructureBtn.gameObject.SetActive(false);

			foreach (CurrencyType type in new[] { CurrencyType.SoftCurrency, CurrencyType.CommandoCredit, CurrencyType.PaladinCredit, CurrencyType.PsyCredit })
				SetCurrencyText(type, GameDatas.current.currentPlayerSave.currencies[type]);
		}

		RefreshDayDisplay();
	}

	private void RefreshDayDisplay ()
	{
		m_cycleTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.hub_cycle), GameDatas.current.currentPlayerSave.cycleCount + 1);

		for (int i = 0; i < m_dayDisplays.Length; i++)
		{
			m_dayDisplays[i].SetActive(GameDatas.current.currentPlayerSave.dayCount >= i);
		}
	}

}
