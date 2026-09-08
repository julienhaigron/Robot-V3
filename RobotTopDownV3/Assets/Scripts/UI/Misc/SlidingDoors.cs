using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlidingDoors : MonoBehaviour
{
	[SerializeField] private RectTransform m_firstDoor;
	[SerializeField] private RectTransform m_secondDoor;
	[SerializeField] private Vector2 m_openedOffset = new Vector2(63f, 0f);
	[SerializeField] private float m_duration = .35f;
	[SerializeField] private Ease m_ease = Ease.InOutQuad;
	[SerializeField] private bool m_doBlockRaycastsWhenClosed = true;

	private Graphic[] m_graphics;

	private Vector2 m_firstClosedPosition;
	private Vector2 m_secondClosedPosition;
	private Tween m_firstTween;
	private Tween m_secondTween;
	private Tween m_refreshTween;

	private bool m_isClosed;
	private bool m_hasState;

	private void Awake ()
	{
		m_graphics = GetComponentsInChildren<Graphic>(true);

		if (m_firstDoor != null)
			m_firstClosedPosition = m_firstDoor.anchoredPosition;

		if (m_secondDoor != null)
			m_secondClosedPosition = m_secondDoor.anchoredPosition;

		SetClosed(false, _isInstant: true);
	}

	private void OnDisable ()
	{
		KillRefreshTween();
		KillTweens();
		ApplyPositions(_isInstant: true);
	}

	private void OnDestroy ()
	{
		KillRefreshTween();
		KillTweens();
	}

	private void KillRefreshTween ()
	{
		if (m_refreshTween.IsActive())
			m_refreshTween.Kill();
	}

	public void CloseThenOpen ( System.Action _onClosed )
	{
		KillRefreshTween();

		if (!gameObject.activeInHierarchy)
		{
			_onClosed?.Invoke();
			return;
		}

		SetClosed(true, _isInstant: false);

		m_refreshTween = DOVirtual.DelayedCall(m_duration, () =>
		{
			_onClosed?.Invoke();
			SetClosed(false, _isInstant: false);
		});
	}

	public void SetClosed ( bool _isClosed, bool _isInstant )
	{
		if (m_hasState && m_isClosed == _isClosed)
			return;

		m_isClosed = _isClosed;
		m_hasState = true;

		KillTweens();
		RefreshRaycasts();
		ApplyPositions(_isInstant || !gameObject.activeInHierarchy);
	}

	private void RefreshRaycasts ()
	{
		if (m_graphics == null)
			return;

		foreach (Graphic graphic in m_graphics)
		{
			if (graphic != null)
				graphic.raycastTarget = m_doBlockRaycastsWhenClosed && m_isClosed;
		}
	}

	private void ApplyPositions ( bool _isInstant )
	{
		if (!m_hasState)
			return;

		MoveDoor(m_firstDoor, m_isClosed ? m_firstClosedPosition : m_firstClosedPosition - m_openedOffset, _isInstant, ref m_firstTween);
		MoveDoor(m_secondDoor, m_isClosed ? m_secondClosedPosition : m_secondClosedPosition + m_openedOffset, _isInstant, ref m_secondTween);
	}

	private void MoveDoor ( RectTransform _door, Vector2 _target, bool _isInstant, ref Tween _tween )
	{
		if (_door == null)
			return;

		if (_isInstant)
		{
			_door.anchoredPosition = _target;
			return;
		}

		_tween = _door.DOAnchorPos(_target, m_duration).SetEase(m_ease);
	}

	private void KillTweens ()
	{
		if (m_firstTween.IsActive())
			m_firstTween.Kill();

		if (m_secondTween.IsActive())
			m_secondTween.Kill();
	}
}
