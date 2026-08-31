using UnityEngine;
using TMPro;

public class StatDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private GameObject[] m_slots;
	[SerializeField] private TextMeshProUGUI m_valueTMP;

	private EntityEquipmentData.StatDescription m_currentDescription;

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += RefreshTitle;
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= RefreshTitle;
	}

	public void Init ( EntityEquipmentData.StatDescription _statDesciption )
	{
		m_currentDescription = _statDesciption;
		m_titleTMP.text = _statDesciption.title;
		if (string.IsNullOrEmpty(_statDesciption.stringValue))
		{
			for (int i = 0; i < m_slots.Length; i++)
			{
				m_slots[i].SetActive(i < _statDesciption.floatValue);
			}
			m_valueTMP.gameObject.SetActive(false);
		}
		else
		{
			foreach (GameObject go in m_slots)
				go.SetActive(false);
			m_valueTMP.gameObject.SetActive(true);
			m_valueTMP.text = _statDesciption.stringValue;
		}
	}

	private void RefreshTitle ()
	{
		if (m_currentDescription == null)
			return;

		m_titleTMP.text = m_currentDescription.title;
	}
}
