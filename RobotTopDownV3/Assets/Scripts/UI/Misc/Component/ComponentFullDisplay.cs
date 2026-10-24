using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ComponentFullDisplay : MonoBehaviour
{
	[SerializeField] private CanvasGroup m_canvasGroup;
	//[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private Image m_componentTypeIcon;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private StatDisplay[] m_statDisplays;
	[SerializeField] private ScrollRect m_statsScrollRect;
	[SerializeField] private CurrencyDisplay m_priceDisplay;
	[SerializeField] private TextMeshProUGUI m_recyclingDurationTMP;
	[SerializeField] private Color m_recyclingPriceTextColor = new(0.47058827f, 0.9686275f, 0.28627452f, 1f);
	[SerializeField] private Color m_recyclingPriceIconColor = new(0.4784314f, 0.9843138f, 0.28627452f, 1f);

	private ComponentDisplay m_currentDisplayedComponent;
	private bool m_hasCachedPriceColors;
	private Color m_defaultPriceTextColor;
	private Color m_defaultPriceIconColor;

	private void Awake ()
	{
		ComponentDisplay.onDisplayHovered += OnComponentHovered;
	}

	public void Init ( GameDatas.PlayerSave.Component _componentSavedData, ComponentDisplay.DisplayMode _displayMode = ComponentDisplay.DisplayMode.Hangar )
	{
		EntityEquipmentData componentData = _componentSavedData != null ? _componentSavedData.GetData<EntityEquipmentData>() : null;
		m_componentTypeIcon.sprite = componentData == null ? null : GameAssets.current.ui.componentIcons[componentData.GetEquipmentType()];
		m_componentTypeIcon.gameObject.SetActive(componentData != null);
		m_corpIcon.sprite = componentData == null ? null : GameAssets.current.ui.corporationsIcons[componentData.faction];
		m_corpIcon.gameObject.SetActive(componentData != null);
		//m_icon.sprite = componentData.icon;
		m_titleTMP.text = componentData == null ? null : componentData.GetLocalizedName();
		bool isRecycling = _displayMode == ComponentDisplay.DisplayMode.RecyclingStation;
		RefreshPrice(componentData == null ? null : ComponentDisplay.GetDisplayedPrice(componentData, _displayMode), isRecycling);
		RefreshRecyclingDuration(isRecycling ? componentData : null);

		if (componentData != null)
		{
			EntityEquipmentData.StatDescription[] statsDescriptions = componentData.GetDesciption();
			EnsureStatDisplayCount(statsDescriptions.Length);
			for (int i = 0; i < m_statDisplays.Length; i++)
			{
				if (statsDescriptions.Length <= i)
					m_statDisplays[i].gameObject.SetActive(false);
				else
				{
					m_statDisplays[i].gameObject.SetActive(true);
					m_statDisplays[i].Init(statsDescriptions[i]);
				}
			}
		}
		else
		{
			foreach(StatDisplay statDisplay in m_statDisplays)
				statDisplay.gameObject.SetActive(false);
		}

		if (m_statsScrollRect != null)
		{
			m_statsScrollRect.StopMovement();
			m_statsScrollRect.content.anchoredPosition = new Vector2(m_statsScrollRect.content.anchoredPosition.x, 0f);
		}
	}

	private void EnsureStatDisplayCount ( int _count )
	{
		if (m_statDisplays.Length == 0 || m_statDisplays.Length >= _count)
			return;

		StatDisplay template = m_statDisplays[m_statDisplays.Length - 1];
		List<StatDisplay> statDisplays = new(m_statDisplays);
		while (statDisplays.Count < _count)
			statDisplays.Add(Instantiate(template, template.transform.parent));

		m_statDisplays = statDisplays.ToArray();
	}

	private void RefreshPrice ( System.Tuple<CurrencyType, ulong> _price, bool _isRecycling )
	{
		Image priceIcon = m_priceDisplay == null ? null : m_priceDisplay.Icon;
		if (!m_hasCachedPriceColors)
		{
			m_defaultPriceTextColor = m_priceTMP.color;
			m_defaultPriceIconColor = priceIcon == null ? Color.white : priceIcon.color;
			m_hasCachedPriceColors = true;
		}

		string priceText = _price == null ? null : (_isRecycling ? "+ " : "") + _price.Item2;
		if (m_priceDisplay != null && _price != null)
			m_priceDisplay.Init(_price.Item1, priceText, false);
		else
			m_priceTMP.text = priceText;

		m_priceTMP.color = _isRecycling ? m_recyclingPriceTextColor : m_defaultPriceTextColor;
		if (priceIcon != null)
			priceIcon.color = _isRecycling ? m_recyclingPriceIconColor : m_defaultPriceIconColor;
	}

	private void RefreshRecyclingDuration ( EntityEquipmentData _recycledComponentData )
	{
		if (m_recyclingDurationTMP == null)
			return;

		m_recyclingDurationTMP.gameObject.SetActive(_recycledComponentData != null);
		if (_recycledComponentData == null)
			return;

		int duration = _recycledComponentData.GetRecyclingDuration();
		LocalizationKey key = duration <= 0 ? LocalizationKey.hub_recycling_time_instant
			: duration == 1 ? LocalizationKey.hub_recycling_time_one : LocalizationKey.hub_recycling_time_many;
		m_recyclingDurationTMP.text = string.Format(LocalizationManager.Instance.Get(key), duration);
	}

	private void OnComponentHovered ( ComponentDisplay _display )
	{
		if (!gameObject.activeInHierarchy || _display == m_currentDisplayedComponent)
			return;

		m_currentDisplayedComponent = _display;

		Init(_display.SavedData, _display.CurrentDisplayMode);
	}
}
