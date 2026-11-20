using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPhaseIndicator : MonoBehaviour
{
	private enum Phase { Preparation, Perform, Cooldown }

	[SerializeField] private Canvas m_canvas;
	[SerializeField] private CanvasGroup m_group;
	[SerializeField] private Image m_guide;
	[SerializeField] private Image[] m_segments;
	[SerializeField] private Image m_progress;
	[SerializeField] private Image m_ripple;
	[SerializeField] private RectTransform m_badge;
	[SerializeField] private Image m_icon;
	[SerializeField] private RectTransform m_pill;
	[SerializeField] private TextMeshProUGUI m_label;
	[SerializeField] private Sprite m_segmentSprite;
	[SerializeField] private Sprite m_cooldownSegmentSprite;
	[SerializeField] private Color m_preparationColor = new(1f, .7f, .24f);
	[SerializeField] private Color m_performColor = new(.32f, .89f, .49f);
	[SerializeField] private Color m_cooldownColor = new(.6f, .66f, .79f);
	[SerializeField, Range(0f, 1f)] private float m_doneAlpha = .85f;
	[SerializeField, Range(0f, 1f)] private float m_pendingAlpha = .22f;
	[SerializeField, Range(0f, 1f)] private float m_guideAlpha = .16f;
	[SerializeField] private float m_arcStartAngle = 200f;
	[SerializeField] private float m_arcEndAngle = 340f;
	[SerializeField] private float m_segmentGap = 3f;
	[SerializeField] private float m_groundHeight = .03f;
	[SerializeField] private float m_badgeGap = .04f;
	[SerializeField] private float m_pillPadding = .1f;
	[SerializeField] private float m_fadeInDuration = .12f;
	[SerializeField] private float m_fadeOutDuration = .25f;
	[SerializeField] private float m_flashDuration = .45f;
	[SerializeField] private float m_rippleDuration = .6f;
	[SerializeField] private float m_rippleScale = 1.4f;

	private Entity m_entity;
	private AEntityAction m_action;
	private Sequence m_tickSequence;
	private Tween m_fadeTween;

	public void Init ( Entity _entity )
	{
		Unsubscribe();
		m_entity = _entity;
		m_entity.onStartActionTick += OnStartActionTick;
		m_entity.onEndPerformAction += OnEndPerformAction;
		TurnManager.onStartInputPhase += Hide;
		HideImmediate();
	}

	public void HideImmediate ()
	{
		m_action = null;
		KillTweens();
		m_group.alpha = 0f;
		m_canvas.enabled = false;
	}

	private void OnDestroy ()
	{
		Unsubscribe();
		KillTweens();
	}

	private void Unsubscribe ()
	{
		if (m_entity == null)
			return;

		m_entity.onStartActionTick -= OnStartActionTick;
		m_entity.onEndPerformAction -= OnEndPerformAction;
		TurnManager.onStartInputPhase -= Hide;
		m_entity = null;
	}

	private void OnEnable ()
	{
		if (m_action == null || !m_action.IsPerforming)
		{
			HideImmediate();
			return;
		}

		m_group.alpha = 1f;
		m_canvas.enabled = true;
		PlayTick(false, false);
	}

	private void OnDisable ()
	{
		KillTweens();
		if (m_action != null)
			return;

		m_group.alpha = 0f;
		m_canvas.enabled = false;
	}

	private void LateUpdate ()
	{
		if (m_canvas.enabled)
			AlignToGround();
	}

	private static bool IsShownFor ( AEntityAction _action )
	{
		EntityActionData data = _action.Data;
		if (data.isModAction || _action.enumID == EntityActionEnumID.Wait || _action.enumID == EntityActionEnumID.Unknowned)
			return false;

		return data.GetMainActionType() != EntityActionData.MainActionType.Movement || _action.TotalDuration > 1;
	}

	private void OnStartActionTick ( AEntityAction _action )
	{
		if (!IsShownFor(_action))
			return;

		m_action = _action;
		Show();
		PlayTick(_action.IsPerformingAtTick(TurnManager.currentTick), isActiveAndEnabled);
	}

	private void OnEndPerformAction ()
	{
		if (m_action != null && !m_action.IsPerforming)
			Hide();
	}

	private void Show ()
	{
		m_fadeTween?.Kill();
		m_fadeTween = null;
		if (!m_canvas.enabled)
		{
			m_group.alpha = 0f;
			m_canvas.enabled = true;
			AlignToGround();
		}

		if (isActiveAndEnabled)
			m_fadeTween = m_group.DOFade(1f, m_fadeInDuration);
		else
			m_group.alpha = 1f;
	}

	private void Hide ()
	{
		m_action = null;
		if (!m_canvas.enabled)
			return;

		if (!isActiveAndEnabled)
		{
			HideImmediate();
			return;
		}

		m_fadeTween?.Kill();
		m_fadeTween = m_group.DOFade(0f, m_fadeOutDuration).OnComplete(() => m_canvas.enabled = false);
	}

	private void PlayTick ( bool _performs, bool _animate )
	{
		int count = Mathf.Clamp(m_action.TotalDuration, 1, m_segments.Length);
		int current = Mathf.Clamp(m_action.lifetime, 0, count - 1);
		float span = (m_arcEndAngle - m_arcStartAngle) / count;
		float arc = Mathf.Max(0f, span - m_segmentGap) / 360f;

		for (int i = 0; i < m_segments.Length; i++)
		{
			Image segment = m_segments[i];
			segment.enabled = i < count;
			if (i >= count)
				continue;

			Phase phase = GetPhase(i);
			PlaceOnArc(segment, i, span, arc);
			segment.sprite = GetSprite(phase);
			segment.color = WithAlpha(GetColor(phase), i < current ? m_doneAlpha : m_pendingAlpha);
		}

		Phase currentPhase = GetPhase(current);
		Color color = GetColor(currentPhase);
		PlaceOnArc(m_progress, current, span, arc);
		m_progress.sprite = GetSprite(currentPhase);
		m_guide.color = WithAlpha(color, m_guideAlpha);
		m_icon.sprite = m_action.Data.icon;
		m_label.text = GetStepText(currentPhase, current);
		m_label.color = color;
		LayoutTag();

		KillTickSequence();
		bool doesFlash = _performs && _animate;
		m_progress.color = doesFlash ? Color.white : color;
		m_icon.color = doesFlash ? Color.white : color;
		m_ripple.color = WithAlpha(color, doesFlash ? 1f : 0f);
		m_ripple.rectTransform.localScale = Vector3.one;
		m_progress.fillAmount = _performs || !_animate ? arc : 0f;
		if (!_animate)
			return;

		m_tickSequence = DOTween.Sequence();
		if (doesFlash)
		{
			m_tickSequence.Join(m_progress.DOColor(color, m_flashDuration))
				.Join(m_icon.DOColor(color, m_flashDuration))
				.Join(m_ripple.rectTransform.DOScale(m_rippleScale, m_rippleDuration).SetEase(Ease.OutQuad))
				.Join(m_ripple.DOFade(0f, m_rippleDuration));
		}
		else
			m_tickSequence.Join(m_progress.DOFillAmount(arc, GameConfig.current.game.actionDuration).SetEase(Ease.Linear));
	}

	private void PlaceOnArc ( Image _image, int _index, float _span, float _arc )
	{
		_image.fillAmount = _arc;
		_image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, m_arcStartAngle + _index * _span + m_segmentGap * .5f);
	}

	private Phase GetPhase ( int _index )
	{
		if (_index < m_action.preparationDuration)
			return Phase.Preparation;

		return _index < m_action.preparationDuration + m_action.actualDuration ? Phase.Perform : Phase.Cooldown;
	}

	private Color GetColor ( Phase _phase )
	{
		return _phase == Phase.Preparation ? m_preparationColor : _phase == Phase.Perform ? m_performColor : m_cooldownColor;
	}

	private Sprite GetSprite ( Phase _phase )
	{
		return _phase == Phase.Cooldown ? m_cooldownSegmentSprite : m_segmentSprite;
	}

	private string GetStepText ( Phase _phase, int _index )
	{
		int first = _phase == Phase.Preparation ? 0 : _phase == Phase.Perform ? m_action.preparationDuration : m_action.preparationDuration + m_action.actualDuration;
		int length = _phase == Phase.Preparation ? m_action.preparationDuration : _phase == Phase.Perform ? m_action.actualDuration : m_action.cooldownDuration;
		LocalizationKey key = _phase == Phase.Preparation ? LocalizationKey.entity_ui_phase_preparation
			: _phase == Phase.Perform ? LocalizationKey.entity_ui_phase_perform : LocalizationKey.entity_ui_phase_cooldown;
		string label = LocalizationManager.Instance.Get(key);

		return length > 1 ? label + " " + (_index - first + 1) + "/" + length : label;
	}

	private void LayoutTag ()
	{
		float pillWidth = m_label.GetPreferredValues(m_label.text).x + m_pillPadding;
		float badgeWidth = m_badge.rect.width;
		float left = -(badgeWidth + m_badgeGap + pillWidth) * .5f;

		m_pill.sizeDelta = new Vector2(pillWidth, m_pill.sizeDelta.y);
		m_badge.anchoredPosition = new Vector2(left + badgeWidth * .5f, m_badge.anchoredPosition.y);
		m_pill.anchoredPosition = new Vector2(left + badgeWidth + m_badgeGap + pillWidth * .5f, m_pill.anchoredPosition.y);
	}

	private void AlignToGround ()
	{
		if (m_entity == null)
			return;

		Vector3 position = m_entity.Displacement.FeetPosition + Vector3.up * m_groundHeight;
		Quaternion rotation = Quaternion.Euler(90f, BillboardManager.TargetRot.eulerAngles.y, 0f);
		if (transform.position != position || transform.rotation != rotation)
			transform.SetPositionAndRotation(position, rotation);
	}

	private static Color WithAlpha ( Color _color, float _alpha )
	{
		_color.a = _alpha;
		return _color;
	}

	private void KillTickSequence ()
	{
		m_tickSequence?.Kill();
		m_tickSequence = null;
	}

	private void KillTweens ()
	{
		KillTickSequence();
		m_fadeTween?.Kill();
		m_fadeTween = null;
		m_ripple.color = WithAlpha(m_ripple.color, 0f);
	}
}
