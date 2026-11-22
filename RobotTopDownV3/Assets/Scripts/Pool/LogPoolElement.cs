using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LogPoolElement : PoolElement, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	[SerializeField] private TextMeshProUGUI m_text;
	[SerializeField] private LayoutElement m_layoutElement;
	[SerializeField] private GameObject m_highlight;
	[SerializeField] private FontStyles m_highlightFontStyle = FontStyles.Bold | FontStyles.Italic;
	[SerializeField] private GameObject m_detailMarker;
	[SerializeField] private GameObject m_hover;
	[SerializeField] private Image m_hoverBar;

	private LogConsole.LogEventType m_eventType;
	public LogConsole.LogEventType EventType => m_eventType;

	private LogConsole.LogDetails m_details;
	private bool m_isHighlighted = false;

	private FontStyles m_defaultFontStyle;
	private bool m_didCacheDefaultFontStyle = false;

	public void Init ( LogConsole.Log _log, bool _isHighlighted )
	{
		m_eventType = _log.eventType;
		m_details = _log.details;
		m_text.SetText(_log.ToString());
		m_detailMarker.SetActive(m_details != null);
		m_hoverBar.color = GameConfig.current.meta.colorsPerType[_log.eventType];
		m_hover.SetActive(false);

		ApplyHighlight(_isHighlighted);
		RefreshPreferredHeight();
	}

	public void OnPointerEnter ( PointerEventData _eventData )
	{
		m_hover.SetActive(m_details != null);
	}

	public void OnPointerExit ( PointerEventData _eventData )
	{
		m_hover.SetActive(false);
	}

	public void OnPointerClick ( PointerEventData _eventData )
	{
		if (m_details != null && _eventData.button == PointerEventData.InputButton.Left)
			ToolTipManager.Instance.TogglePin(m_details);
	}

	private void OnDisable ()
	{
		m_hover.SetActive(false);
	}

	public void SetHighlighted ( bool _isHighlighted )
	{
		if (m_isHighlighted == _isHighlighted)
			return;

		ApplyHighlight(_isHighlighted);
		RefreshPreferredHeight();
	}

	private void ApplyHighlight ( bool _isHighlighted )
	{
		if (!m_didCacheDefaultFontStyle)
		{
			m_defaultFontStyle = m_text.fontStyle;
			m_didCacheDefaultFontStyle = true;
		}

		m_isHighlighted = _isHighlighted;
		m_text.fontStyle = _isHighlighted ? m_defaultFontStyle | m_highlightFontStyle : m_defaultFontStyle;

		if (m_highlight != null)
			m_highlight.SetActive(_isHighlighted);
	}

	private void RefreshPreferredHeight ()
	{
		m_text.ForceMeshUpdate();
		m_layoutElement.preferredHeight = m_text.GetPreferredValues().y + 8f;
	}
}
