using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;


public class ComponentDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public static System.Action<ComponentDisplay> onDisplayHovered;
	public static System.Action<ComponentDisplay> onDisplayUnhovered;

	private static ComponentDisplay s_dragged;

	private readonly struct VisualHome
	{
		public readonly Vector2 anchorMin;
		public readonly Vector2 anchorMax;
		public readonly Vector2 pivot;
		public readonly Vector2 sizeDelta;
		public readonly Vector2 anchoredPosition;
		public readonly Vector3 localScale;
		public readonly int siblingIndex;

		public VisualHome ( RectTransform _rect )
		{
			anchorMin = _rect.anchorMin;
			anchorMax = _rect.anchorMax;
			pivot = _rect.pivot;
			sizeDelta = _rect.sizeDelta;
			anchoredPosition = _rect.anchoredPosition;
			localScale = _rect.localScale;
			siblingIndex = _rect.GetSiblingIndex();
		}

		public void ApplyTo ( RectTransform _rect )
		{
			_rect.SetSiblingIndex(siblingIndex);
			_rect.anchorMin = anchorMin;
			_rect.anchorMax = anchorMax;
			_rect.pivot = pivot;
			_rect.sizeDelta = sizeDelta;
			_rect.anchoredPosition = anchoredPosition;
			_rect.localScale = localScale;
		}

		public Rect GetWorldRectIn ( RectTransform _parent )
		{
			Rect parentRect = _parent.rect;
			Vector2 anchorMinPosition = parentRect.min + Vector2.Scale(parentRect.size, anchorMin);
			Vector2 anchorMaxPosition = parentRect.min + Vector2.Scale(parentRect.size, anchorMax);
			Vector2 size = anchorMaxPosition - anchorMinPosition + sizeDelta;
			Vector2 min = anchorMinPosition + Vector2.Scale(anchorMaxPosition - anchorMinPosition, pivot) + anchoredPosition - Vector2.Scale(size, pivot);
			Vector3 worldMin = _parent.TransformPoint(min);
			Vector3 worldMax = _parent.TransformPoint(min + size);
			return Rect.MinMaxRect(worldMin.x, worldMin.y, worldMax.x, worldMax.y);
		}
	}

	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private RectTransform m_visual;
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

	public ComponentContainer CurrentContainer;

	private float m_lastClickTime = float.MinValue;
	private bool m_isDragBlocked;
	private ComponentGesture? m_pendingRefusal;

	private static readonly Color s_tutorialHighlightColor = Color.white;
	private const float TUTORIAL_HIGHLIGHT_DURATION = 0.55f;
	private const float TUTORIAL_HIGHLIGHT_SCALE = 1.08f;

	private Color m_baseOutlineColor = Color.white;
	private Sequence m_tutorialHighlightSequence;
	private bool m_isTutorialHighlighted;

	private Color? m_basePriceColor;
	private Tween m_liftTween;
	private Tween m_visualTween;
	private Tween m_flightTween;
	private Tween m_restoreCall;
	private VisualHome m_visualHome;
	private bool m_isVisualAway;
	private bool m_isChangingActivation;
	private bool m_isDestroyed;
	private bool m_isDiscarded;

	public enum DisplayMode { Hangar, RepairStation, RecyclingStation, ShopBuying, ShopSelling, Empty, Unit }
	private DisplayMode m_currentDisplayMode;
	public DisplayMode CurrentDisplayMode => m_currentDisplayMode;

	public bool IsDragging => s_dragged == this;
	public Rect VisualWorldRect => (m_visual != null ? m_visual : (RectTransform)transform).GetWorldRect();

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
					m_outlineImg.enabled = false;
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
		SetOutlineColor(_shopData.isFrozen ? ComponentGestureFeedback.FrozenColor : Color.white);
	}

	public void SetOutlineColor(Color _color )
	{
		m_baseOutlineColor = _color;

		if (!m_isTutorialHighlighted)
			m_outlineImg.color = _color;
	}

	public void SetDimmed ( bool _isDimmed )
	{
		m_canvasGroup.alpha = _isDimmed ? ComponentGestureFeedback.DimmedAlpha : 1f;
	}

	public void SetAffordable ( bool _isAffordable )
	{
		SetDimmed(!_isAffordable);
		m_basePriceColor ??= m_priceTMP.color;
		m_priceTMP.color = _isAffordable ? m_basePriceColor.Value : ComponentGestureFeedback.RefusalColor;
	}

	public void SetTutorialHighlighted ( bool _isHighlighted )
	{
		if (m_isTutorialHighlighted == _isHighlighted)
			return;

		m_isTutorialHighlighted = _isHighlighted;

		m_tutorialHighlightSequence?.Kill();
		m_tutorialHighlightSequence = null;
		m_liftTween?.Kill();
		transform.localScale = Vector3.one;

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

	private void OnEnable ()
	{
		if (!m_isVisualAway)
			return;

		m_restoreCall?.Kill();
		m_restoreCall = DOVirtual.DelayedCall(0f, RestoreVisual, true);
	}

	private void OnDisable ()
	{
		m_isChangingActivation = true;
		m_flightTween?.Kill();
		m_isChangingActivation = false;
		m_visualTween?.Kill(true);
		m_liftTween?.Kill();
		if (!m_isTutorialHighlighted)
			transform.localScale = Vector3.one;

		if (s_dragged == this)
		{
			s_dragged = null;
			ComponentContainer.ClearDropTargets();
		}
	}

	private void OnDestroy ()
	{
		m_isDestroyed = true;
		m_tutorialHighlightSequence?.Kill();
		m_flightTween?.Kill();
		m_restoreCall?.Kill();
		if (m_isVisualAway && m_visual != null)
			Destroy(m_visual.gameObject);
	}

	public void Discard ()
	{
		m_isDiscarded = true;
		CurrentContainer = null;
		Destroy(gameObject);
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

	#region Feedback

	private void SetLifted ( bool _isLifted )
	{
		if (m_isTutorialHighlighted)
			return;

		m_liftTween?.Kill();
		m_liftTween = transform.DOScale(_isLifted ? ComponentGestureFeedback.LiftScale : 1f, .12f).SetUpdate(true);
	}

	public void Shake ()
	{
		if (m_visual == null || m_isVisualAway)
			return;

		m_visualTween?.Kill(true);
		m_visualTween = m_visual.DOPunchAnchorPos(new Vector2(ComponentGestureFeedback.ShakeStrength, 0f), .3f, 12, 0f).SetUpdate(true);
	}

	public void Punch ()
	{
		if (m_visual == null || m_isVisualAway)
			return;

		m_visualTween?.Kill(true);
		m_visualTween = m_visual.DOPunchScale(Vector3.one * .12f, .3f, 6, 1f).SetUpdate(true);
	}

	public void FlyFrom ( Rect _from, System.Action _onLanded )
	{
		m_flightTween?.Kill();
		m_visualTween?.Kill(true);
		float duration = ComponentGestureFeedback.FlightDuration;
		if (m_visual == null || duration <= 0f || !isActiveAndEnabled)
		{
			_onLanded?.Invoke();
			return;
		}

		RectTransform layer = (RectTransform)UIManager.Instance.TopLayer;
		m_visualHome = new VisualHome(m_visual);
		m_visual.SetParent(layer, true);
		m_isVisualAway = true;
		m_visual.anchorMin = m_visual.anchorMax = m_visual.pivot = new Vector2(.5f, .5f);
		m_visual.localScale = Vector3.one;

		float scale = layer.lossyScale.x;
		float arc = ComponentGestureFeedback.FlightArc;
		PlaceVisualInFlight(_from, 0f, scale, arc);
		m_flightTween = DOVirtual.Float(0f, 1f, duration, _progress => PlaceVisualInFlight(_from, _progress, scale, arc))
			.SetEase(Ease.OutCubic).SetUpdate(true)
			.OnKill(RestoreVisual)
			.OnComplete(() =>
			{
				RestoreVisual();
				_onLanded?.Invoke();
			});
	}

	private void PlaceVisualInFlight ( Rect _from, float _progress, float _scale, float _arc )
	{
		Rect home = m_visualHome.GetWorldRectIn((RectTransform)transform);
		Vector2 center = Vector2.LerpUnclamped(_from.center, home.center, _progress) + Vector2.up * (_arc * _scale * 4f * _progress * (1f - _progress));
		m_visual.position = new Vector3(center.x, center.y, m_visual.position.z);
		m_visual.sizeDelta = Vector2.LerpUnclamped(_from.size, home.size, _progress) / _scale;
	}

	public void FlyAway ( Rect _from, Rect _to, System.Action _onGone )
	{
		m_flightTween?.Kill();
		m_visualTween?.Kill(true);
		RectTransform visual = m_visual;
		float duration = ComponentGestureFeedback.FlightDuration;
		if (visual == null || duration <= 0f)
		{
			_onGone?.Invoke();
			return;
		}

		m_visual = null;
		RectTransform layer = (RectTransform)UIManager.Instance.TopLayer;
		visual.SetParent(layer, true);
		visual.anchorMin = visual.anchorMax = visual.pivot = new Vector2(.5f, .5f);
		visual.sizeDelta = _from.size / layer.lossyScale.x;

		float arc = ComponentGestureFeedback.FlightArc * layer.lossyScale.x;
		PlaceVisualAway(visual, _from, _to, 0f, arc);
		DOVirtual.Float(0f, 1f, duration, _progress => PlaceVisualAway(visual, _from, _to, _progress, arc))
			.SetUpdate(true).SetLink(visual.gameObject)
			.OnComplete(() => _onGone?.Invoke())
			.OnKill(() =>
			{
				if (visual != null)
					Destroy(visual.gameObject);
			});
	}

	private static void PlaceVisualAway ( RectTransform _visual, Rect _from, Rect _to, float _progress, float _arc )
	{
		float travel = DOVirtual.EasedValue(0f, 1f, _progress, Ease.OutCubic);
		Vector2 center = Vector2.LerpUnclamped(_from.center, _to.center, travel) + Vector2.up * (_arc * 4f * travel * (1f - travel));
		_visual.position = new Vector3(center.x, center.y, _visual.position.z);
		_visual.localScale = Vector3.one * (1f - _progress * _progress);
	}

	private void RestoreVisual ()
	{
		m_flightTween = null;
		if (m_isDestroyed || !m_isVisualAway || m_visual == null)
			return;

		if (m_isChangingActivation)
		{
			m_visual.gameObject.SetActive(false);
			m_restoreCall?.Kill();
			m_restoreCall = DOVirtual.DelayedCall(0f, RestoreVisual, true);
			return;
		}

		m_isVisualAway = false;
		m_visual.SetParent(transform, false);
		m_visualHome.ApplyTo(m_visual);
		m_visual.gameObject.SetActive(true);
	}

	#endregion

	#region Interactions

	public void OnBeginDrag ( PointerEventData eventData )
	{
		ComponentGesture.Refusal release = CurrentContainer != null ? CurrentContainer.GetReleaseRefusal(this) : ComponentGesture.Refusal.None;
		m_isDragBlocked = m_currentDisplayMode == DisplayMode.Empty || release != ComponentGesture.Refusal.None;

		if (m_isDragBlocked)
		{
			if (release != ComponentGesture.Refusal.None)
				ComponentGestureFeedback.Refuse(this, ComponentGesture.Refused(release));
			return;
		}

		m_flightTween?.Kill();
		ComponentGestureFeedback.HideHint(this);
		s_dragged = this;
		m_pendingRefusal = null;

		transform.SetParent(UIManager.Instance.TopLayer);
		transform.SetAsLastSibling();

		m_canvasGroup.blocksRaycasts = false;
		ComponentContainer.ShowDropTargets(this);

		SoundManager.PlayUISafe(SfxId.None, _config => _config.dragPickUp);
	}

	public void OnDrag ( PointerEventData eventData )
	{
		if (m_isDragBlocked || m_currentDisplayMode == DisplayMode.Empty)
			return;

		transform.position = eventData.position;
	}

	public void OnEndDrag ( PointerEventData eventData )
	{
		if (m_isDragBlocked)
		{
			m_isDragBlocked = false;
			return;
		}

		if (m_currentDisplayMode == DisplayMode.Empty)
			return;

		s_dragged = null;
		ComponentContainer.ClearDropTargets();
		if (m_isDiscarded)
			return;

		m_canvasGroup.blocksRaycasts = true;
		SetLifted(false);

		ComponentGesture? refusal = m_pendingRefusal;
		m_pendingRefusal = null;
		if (transform.parent != UIManager.Instance.TopLayer)
			return;

		Rect from = VisualWorldRect;
		ReturnToOrigin();
		FlyFrom(from, () =>
		{
			if (refusal.HasValue)
				ComponentGestureFeedback.Refuse(this, refusal.Value);
			else
				SoundManager.PlayUISafe(SfxId.None, _config => _config.dragDrop);
		});
	}

	public void ReceiveDrop ( ComponentGesture _gesture )
	{
		if (_gesture.IsValid)
			ApplyGesture(_gesture, VisualWorldRect);
		else if (!_gesture.IsSilent)
			m_pendingRefusal = _gesture;
	}

	public void OnPointerClick ( PointerEventData eventData )
	{
		if (m_currentDisplayMode == DisplayMode.Empty || eventData.button != PointerEventData.InputButton.Left)
			return;

		if (Time.unscaledTime - m_lastClickTime < GameConfig.current.ui.doubleClickDelay)
		{
			m_lastClickTime = float.MinValue;
			OnDoubleClick();
			return;
		}

		m_lastClickTime = Time.unscaledTime;
	}

	private void OnDoubleClick ()
	{
		if (m_componentData == null || CurrentContainer == null)
			return;

		ApplyGesture(CurrentContainer.ResolveGesture(this), null);
	}

	public void ApplyGesture ( ComponentGesture _gesture, Rect? _from )
	{
		if (!_gesture.IsValid)
		{
			ComponentGestureFeedback.Refuse(this, _gesture);
			return;
		}

		ComponentGestureFeedback.HideHint(this);
		SetLifted(false);

		ComponentContainer origin = CurrentContainer;
		ComponentDisplay occupant = _gesture.target.Occupant;
		GameDatas.PlayerSave.Component occupantData = occupant != null ? occupant.SavedData : null;
		Rect occupantFrom = occupant != null ? occupant.VisualWorldRect : default;
		GameDatas.PlayerSave.Component data = m_savedData;
		Rect from = _from ?? VisualWorldRect;

		long amount = _gesture.IsTransaction ? _gesture.GetSignedAmount(m_componentData) : 0;
		CurrencyType currency = amount != 0 ? _gesture.GetPrice(m_componentData).Item1 : default;
		if (amount != 0)
			ComponentGestureFeedback.HoldCurrency(currency);

		_gesture.target.RemoveFromOrigin(this);
		_gesture.target.RegisterInteraction(this);

		if (amount != 0)
			ComponentGestureFeedback.PlayMoney(from.center, currency, amount);

		Color color = ComponentGestureFeedback.ColorOf(_gesture.kind);
		if (m_isDiscarded)
			ComponentGestureFeedback.PlayDeparture(this, _gesture.target, from, color);
		else
			ComponentGestureFeedback.PlayArrival(_gesture.target, data, from, color);
		if (occupantData != null && origin != null)
			ComponentGestureFeedback.PlayArrival(origin, occupantData, occupantFrom, ComponentGestureFeedback.ColorOf(ComponentGesture.Kind.Swap));
	}

	public void ReturnToOrigin ()
	{
		if (CurrentContainer != null)
			CurrentContainer.ReturnDisplay(this);
	}

	#endregion

	#region Tooltip

	public void OnPointerEnter ( PointerEventData eventData )
	{
		onDisplayHovered?.Invoke(this);

		if (s_dragged != null || m_componentData == null)
			return;

		SetLifted(true);
		ComponentGestureFeedback.ShowHint(this);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		onDisplayUnhovered?.Invoke(this);

		if (s_dragged == this)
			return;

		SetLifted(false);
		ComponentGestureFeedback.HideHint(this);
	}

	#endregion

}
