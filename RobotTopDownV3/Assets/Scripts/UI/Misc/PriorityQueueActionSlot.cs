using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;


public class PriorityQueueActionSlot : MonoBehaviour, IPointerEnterHandler
{
    [Title("Dependencies")]
	[SerializeField] private Transform m_displayParent;
	public Transform DisplayParent => m_displayParent;
	[SerializeField] private Image m_backgroundImg;
    [SerializeField] private Canvas m_canvas;
    public Canvas Canvas => m_canvas;

	private PriorityQueueActionDisplay m_display;
	public PriorityQueueActionDisplay Display => m_display;

	public void Init ( PriorityQueueActionDisplay _display)
	{
		m_display = _display;
		Show(false);
    }

	public void SetDisplay ( PriorityQueueActionDisplay display )
	{
		m_display = display;

		if (display != null)
		{
			display.transform.SetParent(m_displayParent, false);
			(display.transform as RectTransform).anchoredPosition = Vector2.zero;
		}
	}

	public PriorityQueueActionDisplay GetDisplay ()
	{
		return m_display;
	}

	public void RemoveDisplay ()
	{
		m_display = null;
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		if (eventData.pointerDrag == null)
			return;

		PriorityQueueActionDisplay display = eventData.pointerDrag.GetComponent<PriorityQueueActionDisplay>();

		if (display == null)
			return;

		display.HoverSlot(this);
	}

	public void Show (bool _isInstant)
	{
        gameObject.SetActive(true);
	}
    
    public void Hide ( bool _isInstant )
	{
        gameObject.SetActive(false);
	}
}
