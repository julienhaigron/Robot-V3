using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

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
	private TurnManager.RecordedAction m_recordedAction;

	/*private void Awake ()
	{
		m_canvas = GetComponentInParent<Canvas>();
	}*/

	public void Init(Entity.EntityState _state, StateLineSlot _slot, TurnManager.RecordedAction _recordedAction)
	{
		m_backgroundImg.color = GameAssets.current.ui.entityStateColors[_state];
		m_recordedAction = _recordedAction;
		m_canvas = _slot.Canvas;
		m_originalSlot = _slot;

		_slot.SetDisplay(this);

		Vector2 newSize = (transform as RectTransform).sizeDelta;
		newSize.x = (m_stateLineUnitLenght * _recordedAction.action.TotalDuration) 
			+ (_recordedAction.action.TotalDuration > 1 ? (m_stateLineUnitBorderLenght * (_recordedAction.action.TotalDuration - 1)) : 0);
		(transform as RectTransform).sizeDelta = newSize;
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
		m_backgroundImg.color = GameAssets.current.ui.entityStateColors[_droppedOnSlot.State];
		m_recordedAction.entityState = _droppedOnSlot.State;
		TurnManager.Instance.ReplaceAction(m_recordedAction);
	}

	public void OnBeginDrag ( PointerEventData eventData )
	{
		m_dropSucceeded = false;

		m_originalParent = transform.parent;

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
