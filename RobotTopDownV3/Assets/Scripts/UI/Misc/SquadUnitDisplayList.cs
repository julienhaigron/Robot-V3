using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SquadUnitDisplayList : MonoBehaviour
{
	[SerializeField] private RectTransform m_rtfm;
	[SerializeField] private UnitMacroDisplay[] m_displays;
	[SerializeField] private Vector3 m_hiddenPosition;
	[SerializeField] private Vector3 m_visiblePosition;
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	public void Init ()
	{
		for (int i = 0; i < m_displays.Length; i++)
		{
			if (GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities.Count > i)
			{
				m_displays[i].Init(GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities[i]);
				m_displays[i].Show();
			}
			else
				m_displays[i].Hide();
		}
	}

	public void Show ( float _duration )
	{
		if (_duration == 0)
			m_rtfm.anchoredPosition = m_visiblePosition;
		else
		{
			m_rtfm.DOAnchorPos(m_visiblePosition, _duration);
		}
	}

	public void Hide ( float _duration )
	{
		if (_duration == 0)
			m_rtfm.anchoredPosition = m_hiddenPosition;
		else
		{
			m_rtfm.DOAnchorPos(m_hiddenPosition, _duration);
		}
	}
}
