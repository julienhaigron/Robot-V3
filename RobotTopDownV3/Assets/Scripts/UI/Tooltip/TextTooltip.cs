using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextTooltip : TooltipCard
{
	public const string ROWS_GAP = "\n<size=45%> </size>";
	private const string ROW_VALUE_POSITION = "<pos=7.5em>";
	private const string ROW_LABEL_COLOR = "<color=#9AA3B8>";
	private const string ROW_VALUE_COLOR = "<color=#EEF1F7>";

	[Header("Content")]
	[SerializeField] private VerticalLayoutGroup m_cardLayout;
	[SerializeField] private RectTransform m_head;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private TextMeshProUGUI m_textTMP;
	[SerializeField] private Color m_titleColor = new(.93f, .95f, .97f);
	[SerializeField] private float m_minWidth = 150f;
	[SerializeField] private float m_maxWidth = 300f;

	[Header("Placement")]
	[SerializeField] private float m_anchorGap = 10f;
	[SerializeField] private Vector2 m_cursorOffset = new(15f, -15f);

	private RectTransform m_anchor;

	public static string Row ( string _label, string _value )
	{
		return "\n" + ROW_LABEL_COLOR + _label + "</color>" + ROW_VALUE_POSITION + ROW_VALUE_COLOR + _value + "</color>";
	}

	public void Show ( string _title, string _text, RectTransform _anchor, Sprite _icon, Color? _accent )
	{
		Open();
		m_anchor = IsOnOverlay(_anchor) ? _anchor : null;

		Color accent = _accent ?? m_titleColor;
		bool hasTitle = !string.IsNullOrEmpty(_title);
		m_head.gameObject.SetActive(hasTitle);
		m_icon.gameObject.SetActive(_icon != null);
		m_icon.sprite = _icon;
		m_icon.color = accent;
		m_titleTMP.text = _title;
		m_titleTMP.color = accent;

		bool hasText = !string.IsNullOrEmpty(_text);
		m_textTMP.gameObject.SetActive(hasText);
		m_textTMP.text = _text;

		m_card.sizeDelta = new Vector2(m_maxWidth, m_card.sizeDelta.y);
		LayoutRebuilder.ForceRebuildLayoutImmediate(m_card);
		float content = Mathf.Max(hasTitle ? LayoutUtility.GetPreferredWidth(m_head) : 0f, hasText ? m_textTMP.GetPreferredValues(_text).x : 0f);
		m_card.sizeDelta = new Vector2(Mathf.Clamp(content + m_cardLayout.padding.horizontal, m_minWidth, m_maxWidth), m_card.sizeDelta.y);
		LayoutRebuilder.ForceRebuildLayoutImmediate(m_card);
		Place();
	}

	public override void Hide ()
	{
		base.Hide();
		m_anchor = null;
	}

	protected override void OnShownUpdate ()
	{
		Place();
	}

	private void Place ()
	{
		if (m_anchor != null)
			PlaceAbove(LocalRectOf(m_anchor, null), m_anchorGap);
		else
			PlaceAtCursor(m_cursorOffset);
	}
}
