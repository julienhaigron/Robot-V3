using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestureFeedback : MonoBehaviour
{
	[Serializable]
	private struct KindColor
	{
		public Gesture.Kind kind;
		public Color color;
	}

	private static GestureFeedback s_instance;

	[Header("Hint")]
	[SerializeField] private CanvasGroup m_hintGroup;
	[SerializeField] private RectTransform m_hint;
	[SerializeField] private Image m_hintFill;
	[SerializeField] private Image m_hintFrame;
	[SerializeField] private TextMeshProUGUI m_hintTMP;
	[SerializeField] private float m_hintGap = 6f;
	[SerializeField] private float m_hintFadeDuration = .1f;
	[SerializeField] private float m_refusalHold = 1.5f;

	[Header("Money")]
	[SerializeField] private TextMeshProUGUI m_moneyTMP;
	[SerializeField] private Color m_gainColor = new(.32f, .89f, .49f);
	[SerializeField] private Color m_spendColor = new(1f, .62f, .26f);
	[SerializeField] private float m_moneyRise = 26f;
	[SerializeField] private float m_moneyRiseDuration = .18f;
	[SerializeField] private float m_moneyTravelDuration = .45f;

	[Header("Colors")]
	[SerializeField] private KindColor[] m_kindColors;
	[SerializeField] private Color m_refusalColor = new(1f, .42f, .42f);
	[SerializeField] private Color m_dropValidColor = new(.32f, .89f, .49f, .55f);
	[SerializeField] private Color m_dropOverColor = new(.32f, .89f, .49f);
	[SerializeField] private Color m_lockedColor = new(1f, .42f, .42f, .8f);
	[SerializeField] private Color m_pendingColor = new(.18f, .9f, .77f);
	[SerializeField] private Color m_frozenColor = new(.5f, .84f, 1f);
	[SerializeField] private Color m_hintBaseColor = new(.05f, .04f, .11f, .96f);
	[SerializeField, Range(0f, 1f)] private float m_hintFillTint = .18f;
	[SerializeField, Range(0f, 1f)] private float m_dimmedAlpha = .4f;

	[Header("Motion")]
	[SerializeField] private float m_liftScale = 1.05f;
	[SerializeField] private float m_flightDuration = .26f;
	[SerializeField] private float m_flightArc = 28f;
	[SerializeField] private float m_revealDuration = .2f;
	[SerializeField] private float m_revealMargin = 8f;
	[SerializeField] private float m_pulseDuration = .5f;
	[SerializeField] private float m_shakeStrength = 6f;
	[SerializeField] private float m_callScale = 1.06f;
	[SerializeField] private float m_callPeriod = .6f;

	private GestureDisplay m_hintOwner;
	private GestureDisplay m_hovered;
	private GestureContainer m_hintTarget;
	private float m_refusalEnd;
	private float m_hintAlpha;
	private Tween m_hintPunch;
	private Tween m_moneyTween;

	public static float DimmedAlpha => s_instance != null ? s_instance.m_dimmedAlpha : 1f;
	public static float LiftScale => s_instance != null ? s_instance.m_liftScale : 1f;
	public static float FlightDuration => s_instance != null ? s_instance.m_flightDuration : 0f;
	public static float FlightArc => s_instance != null ? s_instance.m_flightArc : 0f;
	public static float RevealDuration => s_instance != null ? s_instance.m_revealDuration : 0f;
	public static float RevealMargin => s_instance != null ? s_instance.m_revealMargin : 0f;
	public static float PulseDuration => s_instance != null ? s_instance.m_pulseDuration : 0f;
	public static float ShakeStrength => s_instance != null ? s_instance.m_shakeStrength : 0f;
	public static Color RefusalColor => s_instance != null ? s_instance.m_refusalColor : Color.white;
	public static Color DropValidColor => s_instance != null ? s_instance.m_dropValidColor : Color.white;
	public static Color DropOverColor => s_instance != null ? s_instance.m_dropOverColor : Color.white;
	public static Color LockedColor => s_instance != null ? s_instance.m_lockedColor : Color.white;
	public static Color PendingColor => s_instance != null ? s_instance.m_pendingColor : Color.white;
	public static Color FrozenColor => s_instance != null ? s_instance.m_frozenColor : Color.white;

	private static HubTopCanvas Hub => UIManager.Instance != null ? UIManager.Instance.GetTopCanvas<HubTopCanvas>() : null;
	private bool IsHoldingRefusal => Time.unscaledTime < m_refusalEnd;

	private void Awake ()
	{
		s_instance = this;
		m_hintGroup.alpha = 0f;
		m_hint.gameObject.SetActive(false);
		m_moneyTMP.gameObject.SetActive(false);
	}

	private void OnDestroy ()
	{
		if (s_instance == this)
			s_instance = null;

		m_hintPunch?.Kill();
		m_moneyTween?.Kill();
	}

	public static Color ColorOf ( Gesture.Kind _kind )
	{
		if (s_instance != null)
		{
			foreach (KindColor entry in s_instance.m_kindColors)
			{
				if (entry.kind == _kind)
					return entry.color;
			}
		}

		return Color.white;
	}

	public static void ShowHint ( GestureDisplay _display )
	{
		if (s_instance != null)
			s_instance.DoShowHint(_display);
	}

	public static void HideHint ( GestureDisplay _display )
	{
		if (s_instance != null)
			s_instance.DoHideHint(_display);
	}

	public static void Refuse ( GestureDisplay _display, Gesture _gesture )
	{
		if (s_instance != null)
			s_instance.DoRefuse(_display, _gesture);
	}

	public static void PlayArrival ( GestureContainer _container, object _content, Rect _from, Color _color )
	{
		GestureDisplay landed = _container != null ? _container.FindDisplay(_content) : null;
		if (landed == null)
			return;

		_container.Reveal(landed);
		landed.FlyFrom(_from, () =>
		{
			landed.Punch();
			_container.Pulse(_color);
			SoundManager.PlayUISafe(SfxId.None, _config => _config.dragDrop);
		});
	}

	public static void PlayDeparture ( GestureDisplay _display, GestureContainer _container, Rect _from, Color _color, Rect? _to = null )
	{
		Rect to = _to ?? (_container != null ? ((RectTransform)_container.transform).GetWorldRect() : _from);
		_display.FlyAway(_from, to, () =>
		{
			if (_container != null)
				_container.Pulse(_color);
			SoundManager.PlayUISafe(SfxId.None, _config => _config.dragDrop);
		});
	}

	public static void SetCalling ( Transform _target, ref Tween _call, bool _isCalling )
	{
		if (_isCalling == (_call != null))
			return;

		_call?.Kill();
		_call = null;
		_target.localScale = Vector3.one;
		if (_isCalling && s_instance != null)
			_call = _target.DOScale(s_instance.m_callScale, s_instance.m_callPeriod).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
	}

	public static void HoldCurrency ( CurrencyType _type )
	{
		HubTopCanvas hub = s_instance != null ? Hub : null;
		if (hub != null && hub.Visible && hub.GetCurrencyAnchor(_type) != null)
			hub.DeferRoll(_type);
	}

	public static void PlayMoney ( Vector3 _fromWorld, CurrencyType _type, long _amount )
	{
		if (s_instance != null)
			s_instance.DoPlayMoney(_fromWorld, _type, _amount);
	}

	private void DoShowHint ( GestureDisplay _display )
	{
		m_hovered = _display;
		if (IsHoldingRefusal && m_hintOwner == _display)
			return;

		ClearHintTarget();
		GestureContainer origin = _display.CurrentContainer;
		Gesture gesture = origin != null ? origin.ResolveGesture(_display) : Gesture.Refused(Gesture.Refusal.Silent);
		if (gesture.IsSilent)
		{
			HideHintNow();
			return;
		}

		Color color = gesture.IsValid ? ColorOf(gesture.kind) : m_refusalColor;
		ShowHintFor(_display, gesture.GetLabel(_display), color);
		if (!gesture.IsValid)
			return;

		m_hintTarget = gesture.target;
		m_hintTarget.SetHighlight(GestureContainer.Highlight.Target, color);
	}

	private void DoHideHint ( GestureDisplay _display )
	{
		if (m_hovered == _display)
			m_hovered = null;

		if (m_hintOwner != _display)
			return;

		ClearHintTarget();
		if (!IsHoldingRefusal)
			HideHintNow();
	}

	private void DoRefuse ( GestureDisplay _display, Gesture _gesture )
	{
		if (_gesture.IsSilent || _gesture.refusal == Gesture.Refusal.None || _display == null)
			return;

		_display.Shake();
		SoundManager.PlayUISafe(SfxId.None, _config => _config.buttonBlocked);
		ClearHintTarget();
		ShowHintFor(_display, _gesture.GetLabel(_display), m_refusalColor);
		m_refusalEnd = Time.unscaledTime + m_refusalHold;
		m_hintPunch?.Kill(true);
		m_hintPunch = m_hint.DOPunchScale(Vector3.one * .12f, .3f, 6, 1f).SetUpdate(true);
	}

	private void ShowHintFor ( GestureDisplay _display, string _text, Color _color )
	{
		m_hintOwner = _display;
		m_hintTMP.text = _text;
		m_hintTMP.color = _color;
		m_hintFrame.color = new Color(_color.r, _color.g, _color.b, .7f);
		m_hintFill.color = Color.Lerp(m_hintBaseColor, new Color(_color.r, _color.g, _color.b, m_hintBaseColor.a), m_hintFillTint);
		m_hint.gameObject.SetActive(true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(m_hint);
		PlaceHint();
		m_hintAlpha = 1f;
	}

	private void HideHintNow ()
	{
		m_hintOwner = null;
		m_refusalEnd = 0f;
		m_hintAlpha = 0f;
	}

	private void ClearHintTarget ()
	{
		if (m_hintTarget != null)
			m_hintTarget.ClearTargetHighlight();
		m_hintTarget = null;
	}

	private void PlaceHint ()
	{
		Rect rect = m_hintOwner.VisualWorldRect;
		Vector3 scale = m_hint.lossyScale;
		float halfWidth = m_hint.rect.width * scale.x * .5f;
		float height = m_hint.rect.height * scale.y;
		float gap = m_hintGap * scale.y;
		bool isBelow = rect.yMin - gap - height >= 0f;
		m_hint.pivot = new Vector2(.5f, isBelow ? 1f : 0f);
		float x = Mathf.Clamp(rect.center.x, halfWidth, Mathf.Max(halfWidth, Screen.width - halfWidth));
		m_hint.position = new Vector3(x, isBelow ? rect.yMin - gap : rect.yMax + gap, m_hint.position.z);
	}

	private void Update ()
	{
		if (m_refusalEnd > 0f && !IsHoldingRefusal)
		{
			m_refusalEnd = 0f;
			if (m_hovered != null && m_hovered == m_hintOwner)
				DoShowHint(m_hovered);
			else
				HideHintNow();
		}

		if (m_hintOwner != null)
			PlaceHint();
		else
		{
			m_hintAlpha = 0f;
			ClearHintTarget();
		}

		if (!m_hint.gameObject.activeSelf)
			return;

		m_hintGroup.alpha = Mathf.MoveTowards(m_hintGroup.alpha, m_hintAlpha, Time.unscaledDeltaTime / Mathf.Max(.01f, m_hintFadeDuration));
		if (m_hintAlpha <= 0f && m_hintGroup.alpha <= 0f)
			m_hint.gameObject.SetActive(false);
	}

	private void DoPlayMoney ( Vector3 _fromWorld, CurrencyType _type, long _amount )
	{
		HubTopCanvas hub = Hub;
		RectTransform anchor = hub != null && hub.Visible ? hub.GetCurrencyAnchor(_type) : null;
		if (anchor == null || _amount == 0)
		{
			if (hub != null)
				hub.SettleCurrency(_type, false);
			return;
		}

		m_moneyTween?.Kill(true);
		RectTransform money = m_moneyTMP.rectTransform;
		m_moneyTMP.text = (_amount > 0 ? "+" : "-") + Math.Abs(_amount);
		m_moneyTMP.color = _amount > 0 ? m_gainColor : m_spendColor;
		money.gameObject.SetActive(true);
		money.position = new Vector3(_fromWorld.x, _fromWorld.y, money.position.z);
		Vector3 risen = money.position + Vector3.up * (m_moneyRise * money.lossyScale.y);
		m_moneyTween = DOTween.Sequence().SetUpdate(true)
			.Append(money.DOMove(risen, m_moneyRiseDuration).SetEase(Ease.OutQuad))
			.Append(money.DOMove(anchor.position, m_moneyTravelDuration).SetEase(Ease.InCubic))
			.Join(m_moneyTMP.DOFade(.2f, m_moneyTravelDuration).SetEase(Ease.InQuad))
			.OnKill(() =>
			{
				money.gameObject.SetActive(false);
				hub.SettleCurrency(_type, true);
			});
	}
}
