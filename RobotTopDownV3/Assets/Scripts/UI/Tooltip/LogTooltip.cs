using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogTooltip : MonoBehaviour
{
	[Serializable]
	private struct RowView
	{
		public GameObject root;
		public TextMeshProUGUI label;
		public TextMeshProUGUI value;
	}

	[Serializable]
	private struct ColumnView
	{
		public GameObject root;
		public Image rule;
		public TextMeshProUGUI title;
		public TextMeshProUGUI total;
		public RowView[] rows;
	}

	[Serializable]
	private struct StepView
	{
		public GameObject root;
		public TextMeshProUGUI amount;
		public TextMeshProUGUI label;
		public TextMeshProUGUI value;
	}

	[Serializable]
	private struct DamageView
	{
		public GameObject root;
		public Image icon;
		public TextMeshProUGUI type;
		public TextMeshProUGUI baseValue;
		public StepView[] steps;
	}

	[Serializable]
	private struct KindStyle
	{
		public LogCard.Kind kind;
		public Sprite icon;
		public Color color;
	}

	public event Action<LogTooltip> onCloseRequested;

	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private RectTransform m_card;
	[SerializeField] private Vector2 m_cursorOffset = new(15f, -15f);
	[SerializeField] private float m_screenMargin = 12f;
	[SerializeField] private float m_fadeDuration = .12f;

	[Header("Head")]
	[SerializeField] private Image m_iconFill;
	[SerializeField] private Image m_iconFrame;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private GameObject m_subtitle;
	[SerializeField] private TextMeshProUGUI m_actorTMP;
	[SerializeField] private GameObject m_targetArrow;
	[SerializeField] private TextMeshProUGUI m_targetTMP;
	[SerializeField] private GameObject m_verdict;
	[SerializeField] private Image m_verdictFill;
	[SerializeField] private Image m_verdictFrame;
	[SerializeField] private TextMeshProUGUI m_verdictTMP;
	[SerializeField] private TextMeshProUGUI m_verdictValueTMP;
	[SerializeField] private GameObject m_pin;
	[SerializeField] private Button m_closeButton;

	[Header("Roll")]
	[SerializeField] private GameObject m_roll;
	[SerializeField] private RectTransform m_rollZone;
	[SerializeField] private RectTransform m_rollNeedle;
	[SerializeField] private TextMeshProUGUI m_chanceTMP;
	[SerializeField] private TextMeshProUGUI m_rollTMP;
	[SerializeField] private GameObject m_columnsRoot;
	[SerializeField] private ColumnView[] m_columns;
	[SerializeField] private TextMeshProUGUI m_equationTMP;

	[Header("Damage")]
	[SerializeField] private GameObject m_damagesRoot;
	[SerializeField] private DamageView[] m_damages;

	[Header("Status")]
	[SerializeField] private GameObject m_statusChip;
	[SerializeField] private Image m_statusChipFill;
	[SerializeField] private Image m_statusChipFrame;
	[SerializeField] private Image m_statusChipIcon;
	[SerializeField] private TextMeshProUGUI m_statusChipTMP;
	[SerializeField] private GameObject m_duration;
	[SerializeField] private Image[] m_durationCells;
	[SerializeField] private TextMeshProUGUI m_durationTMP;
	[SerializeField] private GameObject m_needsRoll;

	[Header("Facts")]
	[SerializeField] private GameObject m_factsRoot;
	[SerializeField] private RowView[] m_facts;
	[SerializeField] private GameObject m_health;
	[SerializeField] private RectTransform m_healthFill;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;

	[Header("Style")]
	[SerializeField] private KindStyle[] m_kinds;
	[SerializeField] private Color m_attackColor = new(1f, .62f, .26f);
	[SerializeField] private Color m_defenseColor = new(.31f, .82f, 1f);
	[SerializeField] private Color m_neutralColor = new(.6f, .64f, .72f);
	[SerializeField] private Color m_upColor = new(.32f, .89f, .49f);
	[SerializeField] private Color m_downColor = new(1f, .42f, .42f);
	[SerializeField] private Color m_textColor = new(.93f, .95f, .97f);
	[SerializeField] private Color m_cellOffColor = new(1f, 1f, 1f, .12f);
	[SerializeField] private Color m_chipBaseColor = new(.05f, .04f, .11f, .96f);
	[SerializeField, Range(0f, 1f)] private float m_chipFillTint = .16f;
	[SerializeField, Range(0f, 1f)] private float m_chipFrameAlpha = .6f;
	[SerializeField, Range(0f, 1f)] private float m_ruleAlpha = .45f;

	private float m_targetAlpha;
	private bool m_isFollowingCursor = true;

	public bool IsShown => gameObject.activeSelf && m_targetAlpha > 0f;

	private void Awake ()
	{
		m_closeButton.onClick.AddListener(() => onCloseRequested?.Invoke(this));
	}

	public void Show ( LogCard _card )
	{
		gameObject.SetActive(true);
		BindHead(_card);
		BindRoll(_card);
		BindDamages(_card);
		BindStatus(_card);
		BindFacts(_card);
		m_descriptionTMP.gameObject.SetActive(!string.IsNullOrEmpty(_card.description));
		m_descriptionTMP.text = _card.description;

		LayoutRebuilder.ForceRebuildLayoutImmediate(m_card);
		if (m_isFollowingCursor)
			PlaceAtCursor();
		m_targetAlpha = 1f;
	}

	public void Hide ()
	{
		m_targetAlpha = 0f;
	}

	public void SetPinned ( bool _isPinned )
	{
		m_isFollowingCursor = !_isPinned;
		m_pin.SetActive(_isPinned);
		m_canvasGroup.blocksRaycasts = _isPinned;
	}

	public void CopyPlacementOf ( LogTooltip _other )
	{
		m_card.pivot = _other.m_card.pivot;
		m_card.localPosition = _other.m_card.localPosition;
	}

	private void Update ()
	{
		if (!Mathf.Approximately(m_canvasGroup.alpha, m_targetAlpha))
			m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, m_targetAlpha, m_fadeDuration > 0f ? Time.unscaledDeltaTime / m_fadeDuration : 1f);
		else if (m_targetAlpha <= 0f)
		{
			gameObject.SetActive(false);
			return;
		}

		if (m_isFollowingCursor && m_targetAlpha > 0f)
			PlaceAtCursor();
	}

	private void PlaceAtCursor ()
	{
		RectTransform root = (RectTransform)transform;
		Vector2 cursor = Input.mousePosition;
		bool isRightSide = cursor.x > Screen.width * .5f;
		m_card.pivot = new Vector2(isRightSide ? 1f : 0f, 0f);

		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, cursor, null, out Vector2 position);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Vector2.zero, null, out Vector2 screenMin);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(Screen.width, Screen.height), null, out Vector2 screenMax);
		position += new Vector2(isRightSide ? -m_cursorOffset.x : m_cursorOffset.x, m_cursorOffset.y);

		Vector2 size = m_card.rect.size;
		float left = isRightSide ? position.x - size.x : position.x;
		left = Mathf.Clamp(left, screenMin.x + m_screenMargin, Mathf.Max(screenMin.x + m_screenMargin, screenMax.x - m_screenMargin - size.x));
		position.x = isRightSide ? left + size.x : left;
		position.y = Mathf.Clamp(position.y, screenMin.y + m_screenMargin, Mathf.Max(screenMin.y + m_screenMargin, screenMax.y - m_screenMargin - size.y));
		m_card.localPosition = position;
	}

	private void BindHead ( LogCard _card )
	{
		KindStyle style = StyleOf(_card.kind);
		Color accent = _card.accent ?? (_card.verdictDamageType.HasValue ? DamageColor(_card.verdictDamageType.Value) : style.color);
		Paint(m_iconFill, m_iconFrame, accent);
		m_icon.sprite = _card.icon != null ? _card.icon : style.icon;
		m_icon.color = accent;
		m_titleTMP.text = _card.title;

		m_subtitle.SetActive(!string.IsNullOrEmpty(_card.actor));
		m_actorTMP.text = _card.actor;
		bool hasTarget = !string.IsNullOrEmpty(_card.target);
		m_targetArrow.SetActive(hasTarget);
		m_targetTMP.gameObject.SetActive(hasTarget);
		m_targetTMP.text = _card.target;

		bool hasVerdict = !string.IsNullOrEmpty(_card.verdict);
		m_verdict.SetActive(hasVerdict);
		if (!hasVerdict)
			return;

		Color color = _card.verdictDamageType.HasValue ? DamageColor(_card.verdictDamageType.Value) : ToneColor(_card.verdictTone);
		Paint(m_verdictFill, m_verdictFrame, color);
		m_verdictTMP.gameObject.SetActive(!_card.isVerdictValue);
		m_verdictValueTMP.gameObject.SetActive(_card.isVerdictValue);
		TextMeshProUGUI verdict = _card.isVerdictValue ? m_verdictValueTMP : m_verdictTMP;
		verdict.text = _card.verdict;
		verdict.color = color;
	}

	private void BindRoll ( LogCard _card )
	{
		m_roll.SetActive(_card.hasRoll);
		m_columnsRoot.SetActive(_card.columns.Count > 0);
		for (int i = 0; i < m_columns.Length; i++)
			BindColumn(m_columns[i], i < _card.columns.Count ? _card.columns[i] : null);
		m_equationTMP.gameObject.SetActive(!string.IsNullOrEmpty(_card.equation));
		m_equationTMP.text = _card.equation;
		if (!_card.hasRoll)
			return;

		LocalizationManager localization = LocalizationManager.Instance;
		m_rollZone.anchorMax = new Vector2(_card.chance, 1f);
		bool hasRoll = _card.roll.HasValue;
		m_rollNeedle.gameObject.SetActive(hasRoll);
		m_rollTMP.gameObject.SetActive(hasRoll);
		m_chanceTMP.text = hasRoll ? string.Format(localization.Get(LocalizationKey.log_card_chance), _card.chance.ToPercentText()) : localization.Get(LocalizationKey.log_card_guaranteed);
		if (!hasRoll)
		{
			SetLabelAt(m_chanceTMP.rectTransform, 0f, 0f);
			return;
		}

		float roll = Mathf.Clamp01(_card.roll.Value);
		m_rollNeedle.anchorMin = new Vector2(roll, 0f);
		m_rollNeedle.anchorMax = new Vector2(roll, 1f);
		m_rollTMP.text = string.Format(localization.Get(LocalizationKey.log_card_roll), roll.ToPercent());
		bool isRollOnLeft = roll < .35f;
		SetLabelAt(m_chanceTMP.rectTransform, isRollOnLeft ? 1f : 0f, isRollOnLeft ? 1f : 0f);
		SetLabelAt(m_rollTMP.rectTransform, roll, roll > .65f ? 1f : 0f);
	}

	private static void SetLabelAt ( RectTransform _label, float _anchorX, float _pivotX )
	{
		_label.anchorMin = new Vector2(_anchorX, _label.anchorMin.y);
		_label.anchorMax = new Vector2(_anchorX, _label.anchorMax.y);
		_label.pivot = new Vector2(_pivotX, _label.pivot.y);
		_label.anchoredPosition = new Vector2(0f, _label.anchoredPosition.y);
	}

	private void BindColumn ( ColumnView _view, LogCard.Column _column )
	{
		_view.root.SetActive(_column != null);
		if (_column == null)
			return;

		Color color = _column.kind switch
		{
			LogCard.ColumnKind.Attack => m_attackColor,
			LogCard.ColumnKind.Defense => m_defenseColor,
			_ => m_neutralColor
		};
		_view.rule.color = new Color(color.r, color.g, color.b, m_ruleAlpha);
		_view.title.text = _column.title;
		_view.title.color = color;
		_view.total.text = _column.total;
		BindRows(_view.rows, _column.rows);
	}

	private static void BindRows ( RowView[] _views, List<LogCard.Row> _rows )
	{
		for (int i = 0; i < _views.Length; i++)
		{
			bool isUsed = i < _rows.Count;
			_views[i].root.SetActive(isUsed);
			if (!isUsed)
				continue;

			_views[i].label.text = _rows[i].label;
			_views[i].value.text = _rows[i].value;
		}
	}

	private void BindDamages ( LogCard _card )
	{
		m_damagesRoot.SetActive(_card.damages.Count > 0);
		for (int i = 0; i < m_damages.Length; i++)
		{
			DamageView view = m_damages[i];
			bool isUsed = i < _card.damages.Count;
			view.root.SetActive(isUsed);
			if (!isUsed)
				continue;

			LogCard.DamageLine line = _card.damages[i];
			Color color = DamageColor(line.type);
			bool hasIcon = GameAssets.current.ui.damageIconPerType.TryGetValue(line.type, out Sprite icon) && icon != null;
			view.icon.gameObject.SetActive(hasIcon);
			view.icon.sprite = icon;
			view.type.text = line.type.GetLocalizedTitle();
			view.type.color = color;
			view.baseValue.text = line.baseValue.ToString();
			view.baseValue.color = line.steps.Count == 0 ? color : m_textColor;

			for (int s = 0; s < view.steps.Length; s++)
			{
				StepView step = view.steps[s];
				bool isStepUsed = s < line.steps.Count;
				step.root.SetActive(isStepUsed);
				if (!isStepUsed)
					continue;

				LogCard.DamageStep data = line.steps[s];
				bool isLast = s == line.steps.Count - 1;
				step.amount.text = data.amount;
				step.amount.color = ToneColor(data.tone);
				step.label.text = data.label;
				step.value.text = isLast ? line.final.ToString() : data.value;
				step.value.color = isLast ? color : m_textColor;
			}
		}
	}

	private void BindStatus ( LogCard _card )
	{
		AEntityStatus status = _card.status;
		bool isChip = status != null && !_card.showDuration;
		m_statusChip.SetActive(isChip);
		if (isChip)
		{
			Paint(m_statusChipFill, m_statusChipFrame, status.color);
			m_statusChipIcon.gameObject.SetActive(status.icon != null);
			m_statusChipIcon.sprite = status.icon;
			m_statusChipIcon.color = status.color;
			m_statusChipTMP.text = status.GetLocalizedName();
		}

		bool hasDuration = status != null && _card.showDuration;
		m_duration.SetActive(hasDuration);
		m_needsRoll.SetActive(hasDuration && status.doesNeedRoll);
		if (!hasDuration)
			return;

		for (int i = 0; i < m_durationCells.Length; i++)
		{
			m_durationCells[i].gameObject.SetActive(i < _card.duration);
			m_durationCells[i].color = i < _card.remaining ? status.color : m_cellOffColor;
		}

		LocalizationKey key = status.durationType == AEntityStatus.DurationType.Tick ? LocalizationKey.log_card_remaining_ticks : LocalizationKey.log_card_remaining_rounds;
		m_durationTMP.text = string.Format(LocalizationManager.Instance.Get(key), _card.remaining, _card.duration);
	}

	private void BindFacts ( LogCard _card )
	{
		m_factsRoot.SetActive(_card.facts.Count > 0);
		BindRows(m_facts, _card.facts);
		m_health.SetActive(_card.hasHealth);
		if (_card.hasHealth)
			m_healthFill.anchorMax = new Vector2(_card.maxHealth > 0 ? Mathf.Clamp01((float)_card.health / _card.maxHealth) : 0f, 1f);
	}

	private void Paint ( Image _fill, Image _frame, Color _color )
	{
		_fill.color = Color.Lerp(m_chipBaseColor, new Color(_color.r, _color.g, _color.b, m_chipBaseColor.a), m_chipFillTint);
		_frame.color = new Color(_color.r, _color.g, _color.b, m_chipFrameAlpha);
	}

	private KindStyle StyleOf ( LogCard.Kind _kind )
	{
		foreach (KindStyle style in m_kinds)
			if (style.kind == _kind)
				return style;
		return new KindStyle { kind = _kind, color = m_neutralColor };
	}

	private Color ToneColor ( LogCard.Tone _tone )
	{
		return _tone switch
		{
			LogCard.Tone.Up => m_upColor,
			LogCard.Tone.Down => m_downColor,
			_ => m_neutralColor
		};
	}

	private Color DamageColor ( WeaponEquipmentData.DamageType _type )
	{
		return GameAssets.current.ui.damageTypeColors.TryGetValue(_type, out Color color) ? color : m_textColor;
	}
}
