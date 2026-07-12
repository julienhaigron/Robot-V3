using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StateLineDisplay :
	MonoBehaviour,
	IBeginDragHandler,
	IDragHandler,
	IEndDragHandler
{
	[SerializeField] private Image m_backgroundImg;
	[SerializeField] private CanvasGroup m_canvasGroup;

	[SerializeField] private float m_stateLineUnitLenght = 135f;
	[SerializeField] private float m_stateLineUnitBorderLenght = 2.4f;

	private float m_timeAtStart;

	private Canvas m_canvas;
	private Transform m_originalParent;
	private StateLineSlot m_originalSlot;

	private bool m_dropSucceeded;

	/*private void Awake ()
	{
		m_canvas = GetComponentInParent<Canvas>();
	}*/

	public void Init(Entity.EntityState _state, StateLineSlot _slot, float _duration, float _timeAtStart)
	{
		m_backgroundImg.color = GameAssets.current.ui.entityStateColors[_state];
		m_timeAtStart = _timeAtStart;
		m_canvas = _slot.Canvas;

		_slot.SetDisplay(this);

		Vector2 newSize = (m_backgroundImg.transform as RectTransform).sizeDelta;
		newSize.x = (m_stateLineUnitLenght * _duration) + (_duration > 1 ? (m_stateLineUnitBorderLenght * (_duration - 1)) : 0);
		(m_backgroundImg.transform as RectTransform).sizeDelta = newSize;
		/*Vector2 newPos = (m_backgroundImg.transform as RectTransform).anchoredPosition;
		newPos.x = 0f;
		(m_backgroundImg.transform as RectTransform).anchoredPosition = newPos;*/
	}

	private bool DnDPredicate(StateLineSlot _slot )
	{
		return _slot.TimeAtStart == m_timeAtStart;
	}

	private void OnDnDropEnded ( StateLineSlot _droppedOnSlot)
	{
		//for later
	}

	public void OnBeginDrag ( PointerEventData eventData )
	{
		m_dropSucceeded = false;

		m_originalParent = transform.parent;
		m_originalSlot = GetComponentInParent<StateLineSlot>();

		if (m_originalSlot != null)
			m_originalSlot.RemoveDisplay();

		transform.SetParent(m_canvas.transform, true);

		m_canvasGroup.blocksRaycasts = false;
	}

	public void OnDrag ( PointerEventData eventData )
	{
		transform.position = eventData.position;
	}

	public void OnEndDrag ( PointerEventData eventData )
	{
		m_canvasGroup.blocksRaycasts = true;

		if (!m_dropSucceeded)
		{
			transform.SetParent(m_originalParent, false);
			(transform as RectTransform).anchoredPosition = Vector2.zero;

			if (m_originalSlot != null)
				m_originalSlot.SetDisplay(this);
		}
	}

	public void TryDropOn ( StateLineSlot slot )
	{
		if (!DnDPredicate(slot))
			return;

		slot.SetDisplay(this);

		m_dropSucceeded = true;

		OnDnDropEnded(slot);
	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}
}
