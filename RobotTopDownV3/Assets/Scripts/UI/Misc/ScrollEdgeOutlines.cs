using UnityEngine;
using UnityEngine.UI;

public class ScrollEdgeOutlines : MonoBehaviour
{
	[SerializeField] private ScrollRect m_scrollRect;
	[SerializeField] private GameObject m_startOutline;
	[SerializeField] private GameObject m_endOutline;
	[SerializeField] private float m_edgeThreshold = .002f;

	private bool m_isStartOutlineVisible = true;
	private bool m_isEndOutlineVisible = true;
	private bool m_hasState;

	private void OnEnable ()
	{
		if (m_scrollRect != null)
			m_scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

		m_hasState = false;
		Refresh();
	}

	private void OnDisable ()
	{
		if (m_scrollRect != null)
			m_scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
	}

	private void LateUpdate ()
	{
		Refresh();
	}

	private void OnScrollValueChanged ( Vector2 _normalizedPosition )
	{
		Refresh();
	}

	public void Refresh ()
	{
		if (m_scrollRect == null || m_scrollRect.content == null)
			return;

		RectTransform viewport = m_scrollRect.viewport != null ? m_scrollRect.viewport : m_scrollRect.transform as RectTransform;
		if (viewport == null)
			return;

		bool isVertical = m_scrollRect.vertical;
		float contentSize = isVertical ? m_scrollRect.content.rect.height : m_scrollRect.content.rect.width;
		float viewportSize = isVertical ? viewport.rect.height : viewport.rect.width;

		if (contentSize <= viewportSize)
		{
			ApplyVisibility(false, false);
			return;
		}

		//vertical normalized position is 1 at the top and 0 at the bottom, horizontal is the other way around
		float normalizedPosition = isVertical ? m_scrollRect.verticalNormalizedPosition : m_scrollRect.horizontalNormalizedPosition;
		float distanceToStart = isVertical ? 1f - normalizedPosition : normalizedPosition;

		ApplyVisibility(distanceToStart > m_edgeThreshold, distanceToStart < 1f - m_edgeThreshold);
	}

	private void ApplyVisibility ( bool _isStartVisible, bool _isEndVisible )
	{
		if (m_hasState && m_isStartOutlineVisible == _isStartVisible && m_isEndOutlineVisible == _isEndVisible)
			return;

		m_isStartOutlineVisible = _isStartVisible;
		m_isEndOutlineVisible = _isEndVisible;
		m_hasState = true;

		if (m_startOutline != null)
			m_startOutline.SetActive(_isStartVisible);

		if (m_endOutline != null)
			m_endOutline.SetActive(_isEndVisible);
	}
}
