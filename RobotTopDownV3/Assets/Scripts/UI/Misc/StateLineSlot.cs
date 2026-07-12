using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StateLineSlot : MonoBehaviour, IDropHandler
{
	[SerializeField] private Transform m_displayParent;
	[SerializeField] private Canvas m_canvas;
	public Canvas Canvas => m_canvas;

	[SerializeField] private float m_stateLineUnitLenght = 135f;
	[SerializeField] private float m_stateLineUnitBorderLenght = 2.4f;


	private float m_timeAtStart;
	public float TimeAtStart => m_timeAtStart;
	private Entity.EntityState m_state;
	public Entity.EntityState State => m_state;

	private StateLineDisplay m_display;

	public void Init( Entity.EntityState _state, float _timeAtStart )
	{
		m_timeAtStart = _timeAtStart;
		m_state = _state;
	}

	public void RefresSizeAndPosition ( float _duration, float _timeAtStart )
	{
		Vector2 newSize = (transform as RectTransform).sizeDelta;
		newSize.x = (m_stateLineUnitLenght * _duration) + (_duration > 1 ? (m_stateLineUnitBorderLenght * (_duration - 1)) : 0);
		(transform as RectTransform).sizeDelta = newSize;
		Vector2 newPos = (transform as RectTransform).anchoredPosition;
		newPos.x = (m_stateLineUnitLenght * _timeAtStart) + m_stateLineUnitBorderLenght * _timeAtStart;
		(transform as RectTransform).anchoredPosition = newPos;
	}

	public void SetDisplay ( StateLineDisplay display )
	{
		m_display = display;

		if (display != null)
		{
			display.transform.SetParent(m_displayParent, false);
			(display.transform as RectTransform).anchoredPosition = Vector2.zero;
		}
	}

	public StateLineDisplay GetDisplay ()
	{
		return m_display;
	}

	public void RemoveDisplay ()
	{
		m_display = null;
	}

	public void OnDrop ( PointerEventData eventData )
	{
		StateLineDisplay display = eventData.pointerDrag?.GetComponent<StateLineDisplay>();

		if (display == null)
			return;

		display.TryDropOn(this);
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
