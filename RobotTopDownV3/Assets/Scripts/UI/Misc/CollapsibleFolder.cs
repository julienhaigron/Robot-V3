using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class CollapsibleFolder : MonoBehaviour
{
	[SerializeField] private RectTransform m_viewport;
	[SerializeField] private CanvasGroup m_content;
	[SerializeField] private Image m_maximizedBg;
	[SerializeField] private Image m_minimizedBg;
	[SerializeField] private Image m_toggleIcon;
	[SerializeField] private Sprite m_minimizeSprite;
	[SerializeField] private Sprite m_maximizeSprite;
	[SerializeField] private float m_collapsedHeight = 110f;
	[SerializeField] private float m_duration = .25f;
	[SerializeField] private Ease m_ease = Ease.OutCubic;

	private float m_expandedHeight;
	private bool m_isCollapsed;
	public bool IsCollapsed => m_isCollapsed;
	private Sequence m_sequence;

	private void Awake ()
	{
		m_expandedHeight = m_viewport.sizeDelta.y;
		SetCollapsed(false, true);
	}

	private void OnDestroy ()
	{
		m_sequence?.Kill();
	}

	public void Toggle ()
	{
		SetCollapsed(!m_isCollapsed, false);
	}

	public void SetCollapsed ( bool _isCollapsed, bool _isInstant )
	{
		if (_isCollapsed && HasActiveHighlightZone())
			return;

		m_isCollapsed = _isCollapsed;
		m_sequence?.Kill();

		m_content.interactable = !_isCollapsed;
		m_content.blocksRaycasts = !_isCollapsed;

		if (m_toggleIcon != null)
			m_toggleIcon.sprite = _isCollapsed ? m_maximizeSprite : m_minimizeSprite;

		float height = _isCollapsed ? m_collapsedHeight : m_expandedHeight;

		m_sequence = DOTween.Sequence().SetUpdate(true)
			.Join(m_viewport.DOSizeDelta(new Vector2(m_viewport.sizeDelta.x, height), m_duration).SetEase(m_ease))
			.Join(m_content.DOFade(_isCollapsed ? 0f : 1f, m_duration))
			.Join(m_maximizedBg.DOFade(_isCollapsed ? 0f : 1f, m_duration))
			.Join(m_minimizedBg.DOFade(_isCollapsed ? 1f : 0f, m_duration));

		if (_isInstant)
			m_sequence.Complete();
	}

	private bool HasActiveHighlightZone ()
	{
		return TutorialHighlightZone.ActiveHighlightZones.Any(_zone => _zone != null && _zone.transform.IsChildOf(transform));
	}
}
