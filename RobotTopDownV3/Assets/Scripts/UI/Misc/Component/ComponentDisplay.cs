using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;


public class ComponentDisplay : GestureDisplay
{
	public static System.Action<ComponentDisplay> onDisplayHovered;
	public static System.Action<ComponentDisplay> onDisplayUnhovered;

	[SerializeField] private Image m_outlineImg;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_componentTypeIcon;
	[SerializeField] private GameObject m_priceBackground;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;
	[SerializeField] private GameObject m_damagedGO;

	[Title("Shop")]
	[SerializeField] private BaseButton m_freezeBtn;
	[SerializeField] private Image m_freezeImg;
	[SerializeField] private Sprite[] m_freezeOnOffSpriteArray;

	private EntityEquipmentData m_componentData;
	public EntityEquipmentData ComponentData => m_componentData;
	private GameDatas.PlayerSave.Component m_savedData;
	public GameDatas.PlayerSave.Component SavedData => m_savedData;

	private GameDatas.PlayerSave.DayData.ShopComponentData m_shopSavedData;
	public GameDatas.PlayerSave.DayData.ShopComponentData ShopSavedData => m_shopSavedData;

	private static readonly Color s_tutorialHighlightColor = Color.white;
	private const float TUTORIAL_HIGHLIGHT_DURATION = 0.55f;
	private const float TUTORIAL_HIGHLIGHT_SCALE = 1.08f;

	private Color m_baseOutlineColor = Color.white;
	private Sequence m_tutorialHighlightSequence;
	private bool m_isTutorialHighlighted;

	private Color? m_basePriceColor;

	public enum DisplayMode { Hangar, RepairStation, RecyclingStation, ShopBuying, ShopSelling, Empty, Unit }
	private DisplayMode m_currentDisplayMode;
	public DisplayMode CurrentDisplayMode => m_currentDisplayMode;

	public override object Content => m_savedData;
	public override bool IsPlaceholder => m_currentDisplayMode == DisplayMode.Empty || m_componentData == null;
	protected override bool CanLift => !m_isTutorialHighlighted;

	public static System.Tuple<CurrencyType, ulong> GetDisplayedPrice ( EntityEquipmentData _componentData, DisplayMode _displayMode )
	{
		if (_componentData == null)
			return null;

		switch (_displayMode)
		{
			case DisplayMode.RecyclingStation:
				return _componentData.GetRecyclingPrice();
			case DisplayMode.ShopSelling:
				return _componentData.GetShopSellingPrice();
			case DisplayMode.ShopBuying:
				return _componentData.GetShopBuyingPrice();
			default:
				return _componentData.GetPrice();
		}
	}

	public void Init ( EntitySavedData _unitData, GameDatas.PlayerSave.Component _componentSavedData, DisplayMode _displayMode )
	{
		m_savedData = _componentSavedData;
		SetOutlineColor(Color.white);
		SetDimmed(false);
		if (m_basePriceColor.HasValue)
			m_priceTMP.color = m_basePriceColor.Value;

		if (_componentSavedData != null && _componentSavedData.TryGetData(out EntityEquipmentData data))
		{
			m_componentData = data;
			m_componentTypeIcon.sprite = GameAssets.current.ui.componentIcons[m_componentData.GetEquipmentType()];
			m_icon.sprite = m_componentData.icon;
			if(m_damagedGO != null)
				m_damagedGO.SetActive(_componentSavedData.isDamaged);
			m_canvasGroup.blocksRaycasts = true;
			m_canvasGroup.interactable = true;
		}
		else
		{
			if (m_damagedGO != null)
				m_damagedGO.SetActive(false);
			m_canvasGroup.blocksRaycasts = false;
			m_canvasGroup.interactable = false;
		}

		if (m_freezeBtn != null)
		{
			m_freezeBtn.onClick = OnClickFreeze;
		}

		m_currentDisplayMode = _displayMode;
		switch (m_currentDisplayMode)
		{
			case DisplayMode.Unit:
				m_titleTMP.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(false);
				m_priceTMP.gameObject.SetActive(false);
				m_descriptionTMP.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(false);
				if (m_outlineImg != null)
					m_outlineImg.enabled = false;
				break;
			case DisplayMode.Hangar:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.GetLocalizedName();
				m_titleTMP.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(false);
				m_priceTMP.gameObject.SetActive(false);
				m_descriptionTMP.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(false);
				if (m_outlineImg != null)
					m_outlineImg.enabled = true;
				break;
			case DisplayMode.RepairStation:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.GetLocalizedName();
				m_titleTMP.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : GetDisplayedPrice(m_componentData, m_currentDisplayMode).Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				if (m_outlineImg != null)
					m_outlineImg.enabled = false;
				m_descriptionTMP.text = LocalizationManager.Instance.Get(LocalizationKey.component_repair);
				break;
			case DisplayMode.RecyclingStation:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.GetLocalizedName();
				m_titleTMP.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : GetDisplayedPrice(m_componentData, m_currentDisplayMode).Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				if (m_outlineImg != null)
					m_outlineImg.enabled = false;
				m_descriptionTMP.text = LocalizationManager.Instance.Get(LocalizationKey.component_recycle);
				break;
			case DisplayMode.ShopBuying:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.GetLocalizedName();
				m_titleTMP.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(true);
				m_priceBackground.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : GetDisplayedPrice(m_componentData, m_currentDisplayMode).Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				if (m_outlineImg != null)
					m_outlineImg.enabled = true;
				m_descriptionTMP.text = LocalizationManager.Instance.Get(LocalizationKey.component_buy);
				break;
			case DisplayMode.ShopSelling:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.GetLocalizedName();
				m_titleTMP.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(true);
				m_priceBackground.gameObject.SetActive(false);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : GetDisplayedPrice(m_componentData, m_currentDisplayMode).Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(false);
				if (m_outlineImg != null)
					m_outlineImg.enabled = true;
				break;
			case DisplayMode.Empty:
				m_titleTMP.gameObject.SetActive(false);
				m_icon.gameObject.SetActive(false);
				m_componentTypeIcon.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(false);
				m_priceTMP.gameObject.SetActive(false);
				m_descriptionTMP.gameObject.SetActive(false);
				if (m_outlineImg != null)
					m_outlineImg.enabled = true;
				break;
		}
	}

	public void SetShopData ( GameDatas.PlayerSave.DayData.ShopComponentData _shopData )
	{
		m_shopSavedData = _shopData;
		m_freezeImg.sprite = m_freezeOnOffSpriteArray[_shopData.isFrozen ? 0 : 1];
		SetOutlineColor(_shopData.isFrozen ? GestureFeedback.FrozenColor : Color.white);
	}

	public void SetOutlineColor(Color _color )
	{
		m_baseOutlineColor = _color;

		if (!m_isTutorialHighlighted)
			m_outlineImg.color = _color;
	}

	public void SetDimmed ( bool _isDimmed )
	{
		m_canvasGroup.alpha = _isDimmed ? GestureFeedback.DimmedAlpha : 1f;
	}

	public void SetAffordable ( bool _isAffordable )
	{
		SetDimmed(!_isAffordable);
		m_basePriceColor ??= m_priceTMP.color;
		m_priceTMP.color = _isAffordable ? m_basePriceColor.Value : GestureFeedback.RefusalColor;
	}

	public void SetTutorialHighlighted ( bool _isHighlighted )
	{
		if (m_isTutorialHighlighted == _isHighlighted)
			return;

		m_isTutorialHighlighted = _isHighlighted;

		m_tutorialHighlightSequence?.Kill();
		m_tutorialHighlightSequence = null;
		ResetLift();

		if (!_isHighlighted)
		{
			if (m_outlineImg != null)
				m_outlineImg.color = m_baseOutlineColor;
			return;
		}

		m_tutorialHighlightSequence = DOTween.Sequence().SetUpdate(true).SetLoops(-1, LoopType.Yoyo);

		if (m_outlineImg != null)
		{
			m_outlineImg.color = m_baseOutlineColor;
			m_tutorialHighlightSequence.Join(m_outlineImg.DOColor(s_tutorialHighlightColor, TUTORIAL_HIGHLIGHT_DURATION).SetEase(Ease.InOutSine));
		}

		m_tutorialHighlightSequence.Join(transform.DOScale(TUTORIAL_HIGHLIGHT_SCALE, TUTORIAL_HIGHLIGHT_DURATION).SetEase(Ease.InOutSine));
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();
		m_tutorialHighlightSequence?.Kill();
	}

	public override System.Tuple<CurrencyType, ulong> GetGesturePrice ( Gesture.Kind _kind )
	{
		if (m_componentData == null)
			return null;

		return _kind switch
		{
			Gesture.Kind.Buy => m_componentData.GetShopBuyingPrice(),
			Gesture.Kind.Sell => m_componentData.GetShopSellingPrice(),
			Gesture.Kind.Deposit => m_componentData.GetRecyclingPrice(),
			_ => null
		};
	}

	protected override void OnHoverChanged ( bool _isHovered )
	{
		if (_isHovered)
			onDisplayHovered?.Invoke(this);
		else
			onDisplayUnhovered?.Invoke(this);
	}

	private void OnClickFreeze ()
	{
		UIManager.Instance.GetPanel<ShopPanel>().ToggleFreeze(this);
	}

	public void SetFreezeInteractability ( bool _isInteractable )
	{
		if (m_freezeBtn != null)
			m_freezeBtn.SetInteractability(_isInteractable);
	}
}
