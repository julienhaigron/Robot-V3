using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionTooltip : TooltipCard
{
	[Serializable]
	private struct PhaseView
	{
		public GameObject root;
		public Image[] segments;
		public TextMeshProUGUI label;
	}

	[Serializable]
	private struct StatView
	{
		public GameObject root;
		public TextMeshProUGUI label;
		public TextMeshProUGUI value;
	}

	[Serializable]
	private struct RowView
	{
		public GameObject root;
		public Image tone;
		public TextMeshProUGUI text;
	}

	[Serializable]
	private struct ChipView
	{
		public GameObject root;
		public Image fill;
		public Image frame;
		public Image icon;
		public TextMeshProUGUI label;
	}

	[Serializable]
	private struct IconEntry
	{
		public string id;
		public Sprite sprite;
	}


	[Header("Layout")]
	[SerializeField] private RectTransform m_column;
	[SerializeField] private float m_anchorGap = 12f;
	[SerializeField] private float m_columnGap = 10f;
	[SerializeField] private float m_columnDelay = .35f;

	[Header("Head")]
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private TextMeshProUGUI m_typeTMP;
	[SerializeField] private LayoutElement m_typeLayout;
	[SerializeField] private TextMeshProUGUI m_targetTMP;
	[SerializeField] private TextMeshProUGUI m_costTMP;
	[SerializeField] private TextMeshProUGUI m_costUnitTMP;
	[SerializeField] private LayoutElement m_costLayout;

	[Header("Tempo and stats")]
	[SerializeField] private GameObject m_strip;
	[SerializeField] private PhaseView m_preparationPhase;
	[SerializeField] private PhaseView m_performPhase;
	[SerializeField] private PhaseView m_cooldownPhase;
	[SerializeField] private GameObject m_statsRoot;
	[SerializeField] private StatView[] m_stats;

	[Header("Exchange")]
	[SerializeField] private GameObject m_exchange;
	[SerializeField] private Image m_exchangeIcon;
	[SerializeField] private TextMeshProUGUI m_exchangeClassTMP;
	[SerializeField] private TextMeshProUGUI m_exchangeTypeTMP;
	[SerializeField] private RowView[] m_exchangeRows;

	[Header("Keywords")]
	[SerializeField] private GameObject m_chipsRoot;
	[SerializeField] private ChipView[] m_chips;
	[SerializeField] private GameObject m_footer;
	[SerializeField] private TextMeshProUGUI m_footerTMP;
	[SerializeField] private KeywordBox[] m_boxes;
	[SerializeField] private GameObject m_more;
	[SerializeField] private TextMeshProUGUI m_moreTMP;

	[Header("Style")]
	[SerializeField] private Color m_targetColor = new(.36f, .78f, 1f);
	[SerializeField] private Color m_zoneColor = new(1f, .55f, .26f);
	[SerializeField] private Color m_exchangeColor = new(.18f, .9f, .77f);
	[SerializeField] private Color m_effectColor = new(.48f, .65f, 1f);
	[SerializeField] private Color m_conditionColor = new(.96f, .84f, .43f);
	[SerializeField] private Color m_limitColor = new(1f, .54f, .5f);
	[SerializeField] private Color m_mechanicColor = new(.76f, .81f, .88f);
	[SerializeField] private Color m_unitColor = Color.white;
	[SerializeField] private Color m_upColor = new(.32f, .89f, .49f);
	[SerializeField] private Color m_downColor = new(1f, .42f, .42f);
	[SerializeField] private Color m_neutralColor = new(.6f, .64f, .72f);
	[SerializeField] private Color m_rangeBonusColor = new(.96f, .84f, .43f);
	[SerializeField] private Color m_chipBaseColor = new(.05f, .04f, .11f, .96f);
	[SerializeField, Range(0f, 1f)] private float m_chipFillTint = .14f;
	[SerializeField, Range(0f, 1f)] private float m_chipFrameAlpha = .55f;
	[SerializeField] private Sprite m_chipFrameSprite;
	[SerializeField] private Sprite m_unitChipFrameSprite;
	[SerializeField] private IconEntry[] m_icons;

	private readonly List<ActionExchange.Row> m_rows = new();
	private readonly List<string> m_overflowLabels = new();
	private Dictionary<string, Sprite> m_iconById;
	private RectTransform m_anchor;
	private Camera m_anchorCamera;
	private float m_columnTime;
	private bool m_isColumnPending;

	public void Show ( ActionTooltipContent _content, RectTransform _anchor, string _footer )
	{
		m_anchor = _anchor;
		m_anchorCamera = CameraOf(_anchor);

		Open();
		BindHead(_content);
		BindTempo(_content);
		BindStats(_content);
		BindExchange(_content);
		BindChips(_content);
		m_footer.SetActive(!string.IsNullOrEmpty(_footer));
		m_footerTMP.text = _footer;

		m_column.gameObject.SetActive(false);
		m_isColumnPending = BindColumn(_content);
		m_columnTime = Time.unscaledTime + m_columnDelay;

		LayoutRebuilder.ForceRebuildLayoutImmediate(m_card);
		PlaceAbove(m_anchor == null ? new Rect(ScreenRect().center, Vector2.zero) : LocalRectOf(m_anchor, m_anchorCamera), m_anchorGap);
	}

	public override void Hide ()
	{
		base.Hide();
		m_isColumnPending = false;
		m_anchor = null;
	}

	protected override void OnShownUpdate ()
	{
		if (!m_isColumnPending || Time.unscaledTime < m_columnTime)
			return;

		m_isColumnPending = false;
		m_column.gameObject.SetActive(true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(m_column);
		PlaceColumn();
	}

	private void BindHead ( ActionTooltipContent _content )
	{
		m_icon.sprite = _content.data.icon;
		m_nameTMP.text = _content.Name;
		m_typeTMP.text = _content.TypeLabel;
		m_typeLayout.minWidth = m_typeTMP.GetPreferredValues(_content.TypeLabel).x;
		m_targetTMP.text = _content.TargetLabel;
		m_costTMP.text = _content.CostValue;
		m_costUnitTMP.text = _content.CostUnit;
		m_costLayout.minWidth = Mathf.Max(m_costTMP.GetPreferredValues(_content.CostValue).x, m_costUnitTMP.GetPreferredValues(_content.CostUnit).x);
	}

	private void BindTempo ( ActionTooltipContent _content )
	{
		m_strip.SetActive(_content.HasTempo);
		if (!_content.HasTempo)
			return;

		BindPhase(m_preparationPhase, _content.preparation, LocalizationKey.entity_ui_phase_preparation);
		BindPhase(m_performPhase, _content.Duration, LocalizationKey.entity_ui_phase_perform);
		BindPhase(m_cooldownPhase, _content.cooldown, LocalizationKey.entity_ui_phase_cooldown);
	}

	private static void BindPhase ( PhaseView _view, int _ticks, LocalizationKey _label )
	{
		_view.root.SetActive(_ticks > 0);
		if (_ticks <= 0)
			return;

		for (int i = 0; i < _view.segments.Length; i++)
			_view.segments[i].gameObject.SetActive(i < _ticks);
		_view.label.text = LocalizationManager.Instance.Get(_label) + " " + _ticks;
	}

	private void BindStats ( ActionTooltipContent _content )
	{
		int index = 0;
		if (_content.HasSpeed)
			SetStat(index++, LocalizationKey.tooltip_speed, _content.SpeedValue);
		else if (_content.HasRange)
			SetStat(index++, LocalizationKey.tooltip_range, _content.RangeValue
				+ (_content.steadyRangeBonus > 0 ? " <color=#" + ColorUtility.ToHtmlStringRGB(m_rangeBonusColor) + ">+" + _content.steadyRangeBonus + "</color>" : ""));
		if (_content.HasDamage)
			SetStat(index++, LocalizationKey.tooltip_damage, _content.GetDamageText(DamageColorOf));

		m_statsRoot.SetActive(index > 0);
		for (int i = index; i < m_stats.Length; i++)
			m_stats[i].root.SetActive(false);
	}

	private void SetStat ( int _index, LocalizationKey _label, string _value )
	{
		if (_index >= m_stats.Length)
			return;

		m_stats[_index].root.SetActive(true);
		m_stats[_index].label.text = LocalizationManager.Instance.Get(_label);
		m_stats[_index].value.text = _value;
	}

	private void BindExchange ( ActionTooltipContent _content )
	{
		m_exchange.SetActive(_content.hasExchange);
		if (!_content.hasExchange)
			return;

		ActionExchange exchange = _content.exchange;
		m_exchangeIcon.gameObject.SetActive(exchange.HasClass);
		m_exchangeClassTMP.gameObject.SetActive(exchange.HasClass);
		m_exchangeTypeTMP.gameObject.SetActive(exchange.HasClass && !string.IsNullOrEmpty(exchange.ClassTypeLabel));
		m_exchangeIcon.sprite = IconOf(exchange);
		m_exchangeIcon.color = m_exchangeColor;
		m_exchangeClassTMP.text = exchange.ClassName;
		m_exchangeTypeTMP.text = exchange.ClassTypeLabel;

		exchange.GetRows(m_rows);
		for (int i = 0; i < m_exchangeRows.Length; i++)
		{
			RowView row = m_exchangeRows[i];
			bool isUsed = i < m_rows.Count;
			row.root.SetActive(isUsed);
			if (!isUsed)
				continue;

			row.tone.sprite = Icon("Tone" + m_rows[i].tone);
			row.tone.color = ToneColor(m_rows[i].tone);
			row.text.text = m_rows[i].text;
		}
	}

	private void BindChips ( ActionTooltipContent _content )
	{
		int count = 0;
		foreach (ActionKeyword keyword in _content.keywords)
		{
			if (count >= m_chips.Length)
				break;

			ChipView chip = m_chips[count++];
			Color color = ColorOf(keyword);
			chip.root.SetActive(true);
			chip.fill.color = Color.Lerp(m_chipBaseColor, new Color(color.r, color.g, color.b, m_chipBaseColor.a), m_chipFillTint);
			chip.frame.sprite = keyword.isUnit ? m_unitChipFrameSprite : m_chipFrameSprite;
			chip.frame.color = new Color(color.r, color.g, color.b, m_chipFrameAlpha);
			chip.icon.sprite = IconOf(keyword);
			chip.icon.color = color;
			chip.label.text = keyword.Label;
		}

		for (int i = count; i < m_chips.Length; i++)
			m_chips[i].root.SetActive(false);
		m_chipsRoot.SetActive(count > 0);
	}

	private bool BindColumn ( ActionTooltipContent _content )
	{
		int count = 0;
		m_overflowLabels.Clear();

		if (_content.hasExchange)
		{
			ActionExchange exchange = _content.exchange;
			string title = exchange.HasClass ? exchange.ClassName : LocalizationManager.Instance.Get(LocalizationKey.exchange_title);
			m_boxes[count++].Show(IconOf(exchange), m_exchangeColor, title, exchange.ClassTypeLabel, exchange.GetDescription(), "", null);
		}

		foreach (ActionKeyword keyword in _content.keywords)
		{
			if (count >= m_boxes.Length)
			{
				m_overflowLabels.Add(keyword.Label);
				continue;
			}

			string tag = keyword.isUnit ? LocalizationManager.Instance.Get(LocalizationKey.tooltip_unit_tag) : "";
			m_boxes[count++].Show(IconOf(keyword), ColorOf(keyword), keyword.Title, tag, keyword.Text, keyword.Note, keyword.HasZone ? keyword : (ActionKeyword?)null);
		}

		for (int i = count; i < m_boxes.Length; i++)
			m_boxes[i].gameObject.SetActive(false);

		m_more.SetActive(m_overflowLabels.Count > 0);
		if (m_overflowLabels.Count > 0)
			m_moreTMP.text = string.Format(LocalizationManager.Instance.Get(m_overflowLabels.Count > 1 ? LocalizationKey.tooltip_more : LocalizationKey.tooltip_more_one)
				, m_overflowLabels.Count, string.Join(", ", m_overflowLabels));

		return count > 0;
	}

	private void PlaceColumn ()
	{
		Rect screen = ScreenRect();
		Vector2 cardPosition = m_card.localPosition;
		Vector2 cardSize = m_card.rect.size;
		Vector2 size = m_column.rect.size;

		float x = cardPosition.x + cardSize.x + m_columnGap;
		if (x + size.x > screen.xMax - m_screenMargin)
			x = cardPosition.x - m_columnGap - size.x;
		x = Mathf.Max(screen.xMin + m_screenMargin, x);

		float y = Mathf.Min(cardPosition.y, screen.yMax - m_screenMargin - size.y);
		m_column.localPosition = new Vector3(x, Mathf.Max(screen.yMin + m_screenMargin, y), 0f);
	}

	private Color ColorOf ( ActionKeyword _keyword )
	{
		if (_keyword.type == ActionKeywordType.Status && _keyword.status != null)
			return _keyword.status.color;

		return _keyword.Family switch
		{
			ActionKeywordFamily.Target => m_targetColor,
			ActionKeywordFamily.Zone => m_zoneColor,
			ActionKeywordFamily.Effect => m_effectColor,
			ActionKeywordFamily.Condition => m_conditionColor,
			ActionKeywordFamily.Limit => m_limitColor,
			ActionKeywordFamily.Unit => m_unitColor,
			_ => m_mechanicColor
		};
	}

	private Color ToneColor ( ActionExchange.Tone _tone )
	{
		return _tone switch
		{
			ActionExchange.Tone.Up => m_upColor,
			ActionExchange.Tone.Down => m_downColor,
			_ => m_neutralColor
		};
	}

	private static Color? DamageColorOf ( WeaponEquipmentData.DamageType _type )
	{
		return GameAssets.current.ui.damageTypeColors.TryGetValue(_type, out Color color) ? color : null;
	}

	private Sprite IconOf ( ActionKeyword _keyword )
	{
		if (_keyword.type == ActionKeywordType.Status && _keyword.status != null && _keyword.status.icon != null)
			return _keyword.status.icon;
		return Icon(_keyword.type.ToString());
	}

	private Sprite IconOf ( ActionExchange _exchange )
	{
		return Icon(_exchange.isAttack ? _exchange.data.subType.ToString() : _exchange.movesItsUnit ? "Move" : "Place");
	}

	private Sprite Icon ( string _id )
	{
		if (m_iconById == null)
		{
			m_iconById = new Dictionary<string, Sprite>();
			foreach (IconEntry entry in m_icons)
				m_iconById[entry.id] = entry.sprite;
		}

		return m_iconById.TryGetValue(_id, out Sprite sprite) ? sprite : null;
	}
}
