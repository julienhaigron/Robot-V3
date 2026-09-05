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
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Title("Dependencies")]
    [SerializeField] private Image m_icon;
	[SerializeField] private CanvasGroup m_canvasGroup;

	private Canvas m_canvas;
    private PriorityQueueActionSlot m_originalSlot;
	public PriorityQueueActionSlot OriginalSlot => m_originalSlot;

	private bool m_isDragging;
	public bool IsDragging => m_isDragging;

	private EntityActionQueue m_queue;

	private EntityActionEnumID m_actionEnumID;
    public EntityActionEnumID ActionEnumId => m_actionEnumID;

    public void Init (EntityActionEnumID _actionID, PriorityQueueActionSlot _slot, EntityActionQueue _queue )
	{
        m_actionEnumID = _actionID;
		m_icon.sprite = GameAssets.current.game.entityActionsData[_actionID].icon;
		m_canvas = _slot.Canvas.rootCanvas;
		m_queue = _queue;

		_slot.SetDisplay(this);
    }

	private bool DnDPredicate ( PriorityQueueActionSlot _slot )
	{
		return m_originalSlot != _slot;
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		if (eventData.pointerDrag != null)
			return;

		EntityActionData data = GameAssets.current.game.entityActionsData[m_actionEnumID];
		ToolTipManager.Instance.Show(data.displayName, data.GetDescription());
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		ToolTipManager.Instance.Hide();
	}

	public void OnBeginDrag ( PointerEventData eventData )
	{
		m_isDragging = true;

		ToolTipManager.Instance.Hide();

		transform.SetParent(m_canvas.transform, true);
		transform.SetAsLastSibling();

		m_canvasGroup.blocksRaycasts = false;
	}

	public void OnDrag ( PointerEventData eventData )
	{
		transform.position = eventData.position;
	}

	public void OnEndDrag ( PointerEventData eventData )
	{
		m_isDragging = false;
		m_canvasGroup.blocksRaycasts = true;

		if (m_originalSlot != null)
			m_originalSlot.SetDisplay(this);

		m_queue.RegisterActionPriorityOrder();
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
}
