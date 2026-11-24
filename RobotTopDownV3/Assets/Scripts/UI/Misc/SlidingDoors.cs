using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using DG.Tweening;

public class SlidingDoors : MonoBehaviour
{
	[SerializeField] private RectTransform m_firstDoor;
	[SerializeField] private RectTransform m_secondDoor;
	[SerializeField] private CanvasGroup m_closedBadge;
	[SerializeField] private Vector2 m_openedOffset = new Vector2(64f, 0f);
	[SerializeField] private bool m_doBlockRaycastsWhenClosed = true;

	[Header("Close")]
	[SerializeField, FormerlySerializedAs("m_duration")] private float m_closeDuration = .3f;
	[SerializeField] private Ease m_closeEase = Ease.InCubic;
	[SerializeField] private float m_impactDuration = .12f;
	[SerializeField] private float m_impactShake = 2.5f;
	[SerializeField] private float m_badgeDuration = .18f;
	[SerializeField] private float m_badgeStartScale = 1.4f;

	[Header("Open")]
	[SerializeField] private float m_openDuration = .4f;
	[SerializeField] private Ease m_openEase = Ease.OutCubic;

	private Graphic[] m_graphics;
	private Vector2 m_firstClosedPosition;
	private Vector2 m_secondClosedPosition;
	private Sequence m_sequence;
	private bool m_isClosed;

	private Vector2 FirstTarget => m_isClosed ? m_firstClosedPosition : m_firstClosedPosition - m_openedOffset;
	private Vector2 SecondTarget => m_isClosed ? m_secondClosedPosition : m_secondClosedPosition + m_openedOffset;

	private void Awake ()
	{
		Initialize();
	}

	private void OnDisable ()
	{
		KillSequence();
		ApplyInstant();
	}

	private void Initialize ()
	{
		if (m_graphics != null)
			return;

		m_graphics = GetComponentsInChildren<Graphic>(true);
		m_firstClosedPosition = m_firstDoor.anchoredPosition;
		m_secondClosedPosition = m_secondDoor.anchoredPosition;
		RefreshRaycasts();
		ApplyInstant();
	}

	public void SetClosed ( bool _isClosed, bool _isInstant )
	{
		Initialize();

		if (m_isClosed == _isClosed)
			return;

		m_isClosed = _isClosed;

		KillSequence();
		RefreshRaycasts();

		if (_isInstant || !gameObject.activeInHierarchy)
			ApplyInstant();
		else if (m_isClosed)
			PlayClose();
		else
			PlayOpen();
	}

	private void RefreshRaycasts ()
	{
		foreach (Graphic graphic in m_graphics)
			graphic.raycastTarget = m_doBlockRaycastsWhenClosed && m_isClosed;
	}

	private void ApplyInstant ()
	{
		m_firstDoor.anchoredPosition = FirstTarget;
		m_secondDoor.anchoredPosition = SecondTarget;
		m_closedBadge.alpha = m_isClosed ? 1f : 0f;
		m_closedBadge.transform.localScale = Vector3.one;
	}

	private void PlayClose ()
	{
		SoundManager.PlayUISafe(SfxId.None, _config => _config.slotDoorsClose);
		m_closedBadge.alpha = 0f;
		m_closedBadge.transform.localScale = Vector3.one * m_badgeStartScale;

		m_sequence = DOTween.Sequence()
			.Join(m_firstDoor.DOAnchorPos(FirstTarget, m_closeDuration).SetEase(m_closeEase))
			.Join(m_secondDoor.DOAnchorPos(SecondTarget, m_closeDuration).SetEase(m_closeEase))
			.Append(DOVirtual.Float(0f, 1f, m_impactDuration, Shake))
			.Append(m_closedBadge.DOFade(1f, m_badgeDuration))
			.Join(m_closedBadge.transform.DOScale(1f, m_badgeDuration).SetEase(Ease.OutBack));
	}

	private void PlayOpen ()
	{
		SoundManager.PlayUISafe(SfxId.None, _config => _config.slotDoorsOpen);

		m_sequence = DOTween.Sequence()
			.Append(m_closedBadge.DOFade(0f, m_badgeDuration))
			.Append(m_firstDoor.DOAnchorPos(FirstTarget, m_openDuration).SetEase(m_openEase))
			.Join(m_secondDoor.DOAnchorPos(SecondTarget, m_openDuration).SetEase(m_openEase));
	}

	private void Shake ( float _progress )
	{
		Vector2 offset = new Vector2(Mathf.Sin(_progress * Mathf.PI * 4f) * m_impactShake * (1f - _progress), 0f);
		m_firstDoor.anchoredPosition = m_firstClosedPosition + offset;
		m_secondDoor.anchoredPosition = m_secondClosedPosition + offset;
	}

	private void KillSequence ()
	{
		if (m_sequence.IsActive())
			m_sequence.Kill();
	}
}
