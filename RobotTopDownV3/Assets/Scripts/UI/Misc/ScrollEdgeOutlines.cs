using UnityEngine;
using UnityEngine.UI;

public class ScrollEdgeOutlines : MonoBehaviour
{
	[SerializeField] private ScrollRect m_scrollRect;
	[SerializeField] private GameObject m_startOutline;
	[SerializeField] private GameObject m_endOutline;
	[SerializeField] private float m_edgeThreshold = .002f;
	[SerializeField] private float m_sizeTolerance = 1f;

	private static readonly Vector3[] s_corners = new Vector3[4];

	private bool m_isStartOutlineVisible = true;
	private bool m_isEndOutlineVisible = true;
	private bool m_hasState;

	private bool m_canScrollVertically;
	private bool m_canScrollHorizontally;
	private bool m_isScrollable = true;

	private void Awake ()
	{
		if (m_scrollRect == null)
			return;

		m_canScrollVertically = m_scrollRect.vertical;
		m_canScrollHorizontally = m_scrollRect.horizontal;
	}

	private void OnEnable ()
	{
		if (m_scrollRect != null)
			m_scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

		m_hasState = false;
		Refresh();
	}

	private void OnDisable ()
	{
		if (m_scrollRect == null)
			return;

		m_scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
		SetScrollable(true);
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

		bool isVertical = m_canScrollVertically;
		m_scrollRect.content.GetWorldCorners(s_corners);
		Vector3 contentSizeInViewport = viewport.InverseTransformPoint(s_corners[2]) - viewport.InverseTransformPoint(s_corners[0]);
		float contentSize = Mathf.Abs(isVertical ? contentSizeInViewport.y : contentSizeInViewport.x);
		float viewportSize = isVertical ? viewport.rect.height : viewport.rect.width;

		if (contentSize <= viewportSize + m_sizeTolerance)
		{
			SetScrollable(false);
			ApplyVisibility(false, false);
			return;
		}

		SetScrollable(true);

		//vertical normalized position is 1 at the top and 0 at the bottom, horizontal is the other way around
		float normalizedPosition = isVertical ? m_scrollRect.verticalNormalizedPosition : m_scrollRect.horizontalNormalizedPosition;
		float distanceToStart = isVertical ? 1f - normalizedPosition : normalizedPosition;

		ApplyVisibility(distanceToStart > m_edgeThreshold, distanceToStart < 1f - m_edgeThreshold);
	}

	private void SetScrollable ( bool _isScrollable )
	{
		if (m_isScrollable == _isScrollable)
			return;

		m_isScrollable = _isScrollable;
		m_scrollRect.vertical = _isScrollable && m_canScrollVertically;
		m_scrollRect.horizontal = _isScrollable && m_canScrollHorizontally;

		if (_isScrollable)
			return;

		m_scrollRect.StopMovement();
		if (m_canScrollVertically)
			m_scrollRect.verticalNormalizedPosition = 1f;
		else
			m_scrollRect.horizontalNormalizedPosition = 0f;
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
