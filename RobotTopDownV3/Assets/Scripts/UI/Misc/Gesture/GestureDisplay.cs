using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public abstract class GestureDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	private static GestureDisplay s_dragged;

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

	[SerializeField] protected CanvasGroup m_canvasGroup;
	[SerializeField] private RectTransform m_visual;

	public GestureContainer CurrentContainer;

	private float m_lastClickTime = float.MinValue;
	private bool m_isDragBlocked;
	private Gesture? m_pendingRefusal;

	private Tween m_liftTween;
	private Tween m_visualTween;
	private Tween m_flightTween;
	private Tween m_restoreCall;
	private VisualHome m_visualHome;
	private bool m_isVisualAway;
	private bool m_isChangingActivation;
	private bool m_isDestroyed;
	private bool m_isDiscarded;

	public abstract object Content { get; }
	public abstract bool IsPlaceholder { get; }
	protected virtual bool CanLift => true;

	public bool IsDragging => s_dragged == this;
	public Rect VisualWorldRect => (m_visual != null ? m_visual : (RectTransform)transform).GetWorldRect();

	public virtual Tuple<CurrencyType, ulong> GetGesturePrice ( Gesture.Kind _kind )
	{
		return null;
	}

	protected virtual void OnHoverChanged ( bool _isHovered )
	{
	}

	protected virtual void OnEnable ()
	{
		if (!m_isVisualAway)
			return;

		m_restoreCall?.Kill();
		m_restoreCall = DOVirtual.DelayedCall(0f, RestoreVisual, true);
	}

	protected virtual void OnDisable ()
	{
		m_isChangingActivation = true;
		m_flightTween?.Kill();
		m_isChangingActivation = false;
		m_visualTween?.Kill(true);
		m_liftTween?.Kill();
		if (CanLift)
			transform.localScale = Vector3.one;

		if (s_dragged == this)
		{
			s_dragged = null;
			GestureContainer.ClearDropTargets();
		}
	}

	protected virtual void OnDestroy ()
	{
		m_isDestroyed = true;
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

	#region Feedback

	protected void ResetLift ()
	{
		m_liftTween?.Kill();
		transform.localScale = Vector3.one;
	}

	private void SetLifted ( bool _isLifted )
	{
		if (!CanLift)
			return;

		m_liftTween?.Kill();
		m_liftTween = transform.DOScale(_isLifted ? GestureFeedback.LiftScale : 1f, .12f).SetUpdate(true);
	}

	public void Shake ()
	{
		if (m_visual == null || m_isVisualAway)
			return;

		m_visualTween?.Kill(true);
		m_visualTween = m_visual.DOPunchAnchorPos(new Vector2(GestureFeedback.ShakeStrength, 0f), .3f, 12, 0f).SetUpdate(true);
	}

	public void Punch ()
	{
		if (m_visual == null || m_isVisualAway)
			return;

		m_visualTween?.Kill(true);
		m_visualTween = m_visual.DOPunchScale(Vector3.one * .12f, .3f, 6, 1f).SetUpdate(true);
	}

	public void FlyFrom ( Rect _from, Action _onLanded )
	{
		m_flightTween?.Kill();
		m_visualTween?.Kill(true);
		float duration = GestureFeedback.FlightDuration;
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
		float arc = GestureFeedback.FlightArc;
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

	public void FlyAway ( Rect _from, Rect _to, Action _onGone )
	{
		m_flightTween?.Kill();
		m_visualTween?.Kill(true);
		RectTransform visual = m_visual;
		float duration = GestureFeedback.FlightDuration;
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

		float arc = GestureFeedback.FlightArc * layer.lossyScale.x;
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
		Gesture.Refusal release = CurrentContainer != null ? CurrentContainer.GetReleaseRefusal(this) : Gesture.Refusal.None;
		m_isDragBlocked = IsPlaceholder || release != Gesture.Refusal.None;

		if (m_isDragBlocked)
		{
			if (release != Gesture.Refusal.None)
				GestureFeedback.Refuse(this, Gesture.Refused(release));
			return;
		}

		m_flightTween?.Kill();
		GestureFeedback.HideHint(this);
		s_dragged = this;
		m_pendingRefusal = null;

		transform.SetParent(UIManager.Instance.TopLayer);
		transform.SetAsLastSibling();

		m_canvasGroup.blocksRaycasts = false;
		GestureContainer.ShowDropTargets(this);

		SoundManager.PlayUISafe(SfxId.None, _config => _config.dragPickUp);
	}

	public void OnDrag ( PointerEventData eventData )
	{
		if (m_isDragBlocked)
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

		s_dragged = null;
		GestureContainer.ClearDropTargets();
		if (m_isDiscarded)
			return;

		m_canvasGroup.blocksRaycasts = true;
		SetLifted(false);

		Gesture? refusal = m_pendingRefusal;
		m_pendingRefusal = null;
		if (transform.parent != UIManager.Instance.TopLayer)
			return;

		Rect from = VisualWorldRect;
		ReturnToOrigin();
		FlyFrom(from, () =>
		{
			if (refusal.HasValue)
				GestureFeedback.Refuse(this, refusal.Value);
			else
				SoundManager.PlayUISafe(SfxId.None, _config => _config.dragDrop);
		});
	}

	public void ReceiveDrop ( Gesture _gesture )
	{
		if (_gesture.IsValid)
			ApplyGesture(_gesture, VisualWorldRect);
		else if (!_gesture.IsSilent)
			m_pendingRefusal = _gesture;
	}

	public void OnPointerClick ( PointerEventData eventData )
	{
		if (IsPlaceholder || eventData.button != PointerEventData.InputButton.Left)
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
		if (CurrentContainer != null)
			ApplyGesture(CurrentContainer.ResolveGesture(this), null);
	}

	public void ApplyGesture ( Gesture _gesture, Rect? _from )
	{
		if (!_gesture.IsValid)
		{
			GestureFeedback.Refuse(this, _gesture);
			return;
		}

		GestureFeedback.HideHint(this);
		SetLifted(false);

		GestureContainer origin = CurrentContainer;
		GestureDisplay occupant = _gesture.target.Occupant;
		object occupantContent = occupant != null ? occupant.Content : null;
		Rect occupantFrom = occupant != null ? occupant.VisualWorldRect : default;
		object content = Content;
		Rect from = _from ?? VisualWorldRect;

		long amount = _gesture.IsTransaction ? _gesture.GetSignedAmount(this) : 0;
		CurrencyType currency = amount != 0 ? _gesture.GetPrice(this).Item1 : default;
		if (amount != 0)
			GestureFeedback.HoldCurrency(currency);

		_gesture.target.RemoveFromOrigin(this);
		_gesture.target.RegisterInteraction(this);

		if (amount != 0)
			GestureFeedback.PlayMoney(from.center, currency, amount);

		Color color = GestureFeedback.ColorOf(_gesture.kind);
		if (m_isDiscarded)
			GestureFeedback.PlayDeparture(this, _gesture.target, from, color);
		else
			GestureFeedback.PlayArrival(_gesture.target, content, from, color);
		if (occupantContent != null && origin != null)
			GestureFeedback.PlayArrival(origin, occupantContent, occupantFrom, GestureFeedback.ColorOf(Gesture.Kind.Swap));
	}

	public void ReturnToOrigin ()
	{
		if (CurrentContainer != null)
			CurrentContainer.ReturnDisplay(this);
	}

	#endregion

	#region Hover

	public void OnPointerEnter ( PointerEventData eventData )
	{
		OnHoverChanged(true);

		if (s_dragged != null || IsPlaceholder)
			return;

		SetLifted(true);
		GestureFeedback.ShowHint(this);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		OnHoverChanged(false);

		if (s_dragged == this)
			return;

		SetLifted(false);
		GestureFeedback.HideHint(this);
	}

	#endregion
}
