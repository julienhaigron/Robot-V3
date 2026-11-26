using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ContentSwap : MonoBehaviour
{
	[SerializeField] private CanvasGroup m_group;

	private RectTransform m_rect;
	private Vector2 m_restPosition;
	private bool m_isInitialized;
	private Tween m_tween;

	private void Awake ()
	{
		Initialize();
	}

	private void OnDisable ()
	{
		m_tween?.Kill();
	}

	private void Initialize ()
	{
		if (m_isInitialized)
			return;

		m_isInitialized = true;
		m_rect = (RectTransform)transform;
		m_restPosition = m_rect.anchoredPosition;
	}

	public void Play ()
	{
		Initialize();
		if (!isActiveAndEnabled)
			return;

		m_tween?.Kill();
		m_tween = UIMotion.Swap(m_rect, m_group, m_restPosition);
	}
}
