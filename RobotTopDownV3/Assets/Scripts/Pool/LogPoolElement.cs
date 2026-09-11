using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogPoolElement : PoolElement
{
	[SerializeField] private TextMeshProUGUI m_text;
	[SerializeField] private LayoutElement m_layoutElement;
	[SerializeField] private Vector2 m_preferedTextSizePrefreredValues;
	[SerializeField] private FontStyles m_highlightFontStyle = FontStyles.Bold | FontStyles.Italic;

	private LogConsole.LogEventType m_eventType;
	public LogConsole.LogEventType EventType => m_eventType;

	private bool m_isHighlighted = false;
	public bool IsHighlighted => m_isHighlighted;

	private FontStyles m_defaultFontStyle;
	private bool m_didCacheDefaultFontStyle = false;

	public void Init ( LogConsole.Log _log, bool _isHighlighted )
	{
		m_eventType = _log.eventType;
		m_text.SetText(_log.ToString());

		ApplyHighlight(_isHighlighted);
		RefreshPreferredHeight();
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
	}

	private void RefreshPreferredHeight ()
	{
		m_text.ForceMeshUpdate();
		m_layoutElement.preferredHeight = m_text.GetPreferredValues().y + 8f;
	}
}
