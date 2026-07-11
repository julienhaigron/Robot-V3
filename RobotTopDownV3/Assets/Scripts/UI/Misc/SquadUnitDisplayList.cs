using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Sirenix.OdinInspector;

public class SquadUnitDisplayList : MonoBehaviour
{
    [SerializeField] private UnitMacroDisplay[] m_displays;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void Init ()
	{
        for(int i = 0; i < m_displays.Length; i++)
		{
            if(GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities.Count > i)
			{
				m_displays[i].Init(GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities[i]);
				m_displays[i].Show();
			}
			else
				m_displays[i].Hide();
		}
	}
}
