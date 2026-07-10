using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StateLineDisplay : MonoBehaviour
{
	[SerializeField] private Image m_backgroundImg;

	[SerializeField] private float m_stateLineUnitLenght = 135f;
	[SerializeField] private float m_stateLineUnitBorderLenght = 2.4f;

	public void Init(Entity.EntityState _state, float _duration, float _timeAtStart)
	{
		m_backgroundImg.color = GameAssets.current.ui.entityStateColors[_state];
		Vector2 newSize = (m_backgroundImg.transform as RectTransform).sizeDelta;
		newSize.x = (m_stateLineUnitLenght * _duration) + (_duration > 1 ? (m_stateLineUnitBorderLenght * (_duration - 1)) : 0);
		(m_backgroundImg.transform as RectTransform).sizeDelta = newSize;
		Vector2 newPos = (m_backgroundImg.transform as RectTransform).anchoredPosition;
		newPos.x = (m_stateLineUnitLenght * _timeAtStart) + m_stateLineUnitBorderLenght * _timeAtStart;
		(m_backgroundImg.transform as RectTransform).anchoredPosition = newPos;
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
