using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;


public class PriorityQueueActionDisplay :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Title("Dependencies")]
    [SerializeField] private Image m_icon;
	[SerializeField] private CanvasGroup m_canvasGroup;

	private Canvas m_canvas;
    private Transform m_originalParent;
    private PriorityQueueActionSlot m_originalSlot;
	public PriorityQueueActionSlot OriginalSlot => m_originalSlot;

	//private bool m_dropSucceeded;
	private EntityActionQueue m_queue;

	private EntityActionEnumID m_actionEnumID;
    public EntityActionEnumID ActionEnumId => m_actionEnumID;

    public void Init (EntityActionEnumID _actionID, PriorityQueueActionSlot _slot, EntityActionQueue _queue )
	{
        m_actionEnumID = _actionID;
		m_icon.sprite = GameAssets.current.game.entityActionsData[_actionID].icon;
		m_canvas = _slot.Canvas;
        m_originalSlot = _slot;
		m_queue = _queue;

		_slot.SetDisplay(this);
    }

	private bool DnDPredicate ( PriorityQueueActionSlot _slot )
	{
		return m_originalSlot != _slot;
	}

	private void OnDnDropEnded ( PriorityQueueActionSlot _droppedOnSlot )
	{
		m_originalSlot = _droppedOnSlot;
		UIManager.Instance.GetPanel<InGamePanel>().ActionQueue.RegisterActionPriorityOrder();
	}

	public void OnBeginDrag ( PointerEventData eventData )
	{
		//m_dropSucceeded = false;

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

		transform.SetParent(m_originalSlot.DisplayParent, false);
		(transform as RectTransform).anchoredPosition = Vector2.zero;

		OnDnDropEnded(m_originalSlot);
	}

	public void HoverSlot ( PriorityQueueActionSlot slot )
	{
		if (!DnDPredicate(slot))
			return;

		m_queue.Move(this, slot);
	}

	public void SetCurrentSlot ( PriorityQueueActionSlot slot )
	{
		m_originalSlot = slot;
	}

	public void TryDropOn ( PriorityQueueActionSlot slot )
	{
		if (!DnDPredicate(slot))
			return;

		m_queue.Move(this, slot);
		//slot.SetDisplay(this);

		//m_dropSucceeded = true;

		OnDnDropEnded(slot);
	}
}
