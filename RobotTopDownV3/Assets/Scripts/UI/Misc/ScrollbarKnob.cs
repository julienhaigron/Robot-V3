using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class ScrollbarKnob : MonoBehaviour
{
	[SerializeField] private RectTransform m_knob;

	private Scrollbar m_scrollbar;

	private void Awake ()
	{
		m_scrollbar = GetComponent<Scrollbar>();
		m_scrollbar.onValueChanged.AddListener(Place);
	}

	private void OnEnable ()
	{
		Place(m_scrollbar.value);
	}

	private void OnDestroy ()
	{
		m_scrollbar.onValueChanged.RemoveListener(Place);
	}

	private void Place ( float _value )
	{
		Scrollbar.Direction direction = m_scrollbar.direction;
		float value = Mathf.Clamp01(_value);
		float position = direction is Scrollbar.Direction.TopToBottom or Scrollbar.Direction.RightToLeft ? 1f - value : value;
		Vector2 anchor = direction is Scrollbar.Direction.BottomToTop or Scrollbar.Direction.TopToBottom
			? new Vector2(.5f, position)
			: new Vector2(position, .5f);

		m_knob.anchorMin = anchor;
		m_knob.anchorMax = anchor;
		m_knob.anchoredPosition = Vector2.zero;
	}
}
