using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LogCardStyle
{
	public Color attackColor = new(1f, .62f, .26f);
	public Color defenseColor = new(.31f, .82f, 1f);
	public Color neutralColor = new(.6f, .64f, .72f);
	public Color upColor = new(.32f, .89f, .49f);
	public Color downColor = new(1f, .42f, .42f);
	public Color textColor = new(.93f, .95f, .97f);
	public Color cellOffColor = new(1f, 1f, 1f, .12f);
	public Color chipBaseColor = new(.05f, .04f, .11f, .96f);
	[Range(0f, 1f)] public float chipFillTint = .16f;
	[Range(0f, 1f)] public float chipFrameAlpha = .6f;
	[Range(0f, 1f)] public float ruleAlpha = .45f;

	public void Paint ( Image _fill, Image _frame, Color _color )
	{
		_fill.color = Color.Lerp(chipBaseColor, new Color(_color.r, _color.g, _color.b, chipBaseColor.a), chipFillTint);
		_frame.color = new Color(_color.r, _color.g, _color.b, chipFrameAlpha);
	}

	public Color Tone ( LogCard.Tone _tone )
	{
		return _tone switch
		{
			LogCard.Tone.Up => upColor,
			LogCard.Tone.Down => downColor,
			_ => neutralColor
		};
	}

	public Color Damage ( WeaponEquipmentData.DamageType _type )
	{
		return GameAssets.current.ui.damageTypeColors.TryGetValue(_type, out Color color) ? color : textColor;
	}

	public Color Verdict ( LogCard _card )
	{
		return _card.verdictDamageType.HasValue ? Damage(_card.verdictDamageType.Value) : Tone(_card.verdictTone);
	}
}

public class LogStepView : MonoBehaviour
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
	private struct ChainStepView
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
		public ChainStepView[] steps;
	}

	[Header("Step header")]
	[SerializeField] private GameObject m_header;
	[SerializeField] private TextMeshProUGUI m_headerTitleTMP;
	[SerializeField] private TextMeshProUGUI m_headerTargetTMP;
	[SerializeField] private TextMeshProUGUI m_headerVerdictTMP;

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

	public void Bind ( LogCard _card, LogCard _owner, LogCardStyle _style )
	{
		BindHeader(_card, _owner, _style);
		BindRoll(_card, _style);
		BindDamages(_card, _style);
		BindStatus(_card, _style);
		BindFacts(_card);
		m_descriptionTMP.gameObject.SetActive(!string.IsNullOrEmpty(_card.description));
		m_descriptionTMP.text = _card.description;
	}

	private void BindHeader ( LogCard _card, LogCard _owner, LogCardStyle _style )
	{
		m_header.SetActive(_owner != null);
		if (_owner == null)
			return;

		m_headerTitleTMP.text = _card.title;
		bool hasTarget = !string.IsNullOrEmpty(_card.target) && _card.target != _owner.target;
		m_headerTargetTMP.text = hasTarget ? _card.target : string.Empty;
		bool hasVerdict = !string.IsNullOrEmpty(_card.verdict);
		m_headerVerdictTMP.gameObject.SetActive(hasVerdict);
		m_headerVerdictTMP.text = _card.verdict;
		m_headerVerdictTMP.color = _style.Verdict(_card);
	}

	private void BindRoll ( LogCard _card, LogCardStyle _style )
	{
		m_roll.SetActive(_card.hasRoll);
		m_columnsRoot.SetActive(_card.columns.Count > 0);
		for (int i = 0; i < m_columns.Length; i++)
			BindColumn(m_columns[i], i < _card.columns.Count ? _card.columns[i] : null, _style);
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

	private static void BindColumn ( ColumnView _view, LogCard.Column _column, LogCardStyle _style )
	{
		_view.root.SetActive(_column != null);
		if (_column == null)
			return;

		Color color = _column.kind switch
		{
			LogCard.ColumnKind.Attack => _style.attackColor,
			LogCard.ColumnKind.Defense => _style.defenseColor,
			_ => _style.neutralColor
		};
		_view.rule.color = new Color(color.r, color.g, color.b, _style.ruleAlpha);
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

	private void BindDamages ( LogCard _card, LogCardStyle _style )
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
			Color color = _style.Damage(line.type);
			bool hasIcon = GameAssets.current.ui.damageIconPerType.TryGetValue(line.type, out Sprite icon) && icon != null;
			view.icon.gameObject.SetActive(hasIcon);
			view.icon.sprite = icon;
			view.type.text = line.type.GetLocalizedTitle();
			view.type.color = color;
			view.baseValue.text = line.baseValue.ToString();
			view.baseValue.color = line.steps.Count == 0 ? color : _style.textColor;

			for (int s = 0; s < view.steps.Length; s++)
			{
				ChainStepView step = view.steps[s];
				bool isStepUsed = s < line.steps.Count;
				step.root.SetActive(isStepUsed);
				if (!isStepUsed)
					continue;

				LogCard.DamageStep data = line.steps[s];
				bool isLast = s == line.steps.Count - 1;
				step.amount.text = data.amount;
				step.amount.color = _style.Tone(data.tone);
				step.label.text = data.label;
				step.value.text = isLast ? line.final.ToString() : data.value;
				step.value.color = isLast ? color : _style.textColor;
			}
		}
	}

	private void BindStatus ( LogCard _card, LogCardStyle _style )
	{
		AEntityStatus status = _card.status;
		bool isChip = status != null && !_card.showDuration;
		m_statusChip.SetActive(isChip);
		if (isChip)
		{
			_style.Paint(m_statusChipFill, m_statusChipFrame, status.color);
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
			m_durationCells[i].color = i < _card.remaining ? status.color : _style.cellOffColor;
		}

		LocalizationKey key = status.durationType switch
		{
			AEntityStatus.DurationType.Tick => LocalizationKey.log_card_remaining_ticks,
			AEntityStatus.DurationType.Round => LocalizationKey.log_card_remaining_rounds,
			_ => LocalizationKey.log_card_remaining_charges
		};
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
}
