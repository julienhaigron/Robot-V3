using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class StatSectionDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image[] m_statIcons;

	private List<EntityEquipmentData.StatDescription> m_statsDescriptions;

	public void Init ( string _title, List<EntityEquipmentData.StatDescription> _statsDesciptions )
	{
		m_titleTMP.text = _title;
		m_statsDescriptions = _statsDesciptions;

		for(int i = 0; i < m_statIcons.Length; i++)
		{
			if (i >= _statsDesciptions.Count)
				m_statIcons[i].gameObject.SetActive(false);
			else
			{
				m_statIcons[i].gameObject.SetActive(true);
				m_statIcons[i].sprite = GameAssets.current.ui.statsIcons[_statsDesciptions[i].ID];
			}
		}
	}


}
