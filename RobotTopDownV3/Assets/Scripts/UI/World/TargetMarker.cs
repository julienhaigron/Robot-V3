using UnityEngine;
using UnityEngine.UI;

public class TargetMarker : MonoBehaviour
{
	public enum State { None, Candidate, Preview, Designated }

	[SerializeField] private Image m_image;
	[SerializeField] private Sprite m_candidateSprite;
	[SerializeField] private Sprite m_designatedSprite;
	[SerializeField] private Color m_color = new(1f, .66f, .23f);
	[SerializeField, Range(0f, 1f)] private float m_previewAlpha = .5f;

	private State m_state = State.None;

	public void Set ( State _state, bool _isForced = false )
	{
		if (_state == m_state && !_isForced)
			return;

		m_state = _state;
		gameObject.SetActive(_state != State.None);
		if (_state == State.None)
			return;

		m_image.sprite = _state == State.Candidate ? m_candidateSprite : m_designatedSprite;
		Color color = m_color;
		if (_state == State.Preview)
			color.a *= m_previewAlpha;
		m_image.color = color;
	}
}
