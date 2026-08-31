using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class RepareUnitFullDisplay : MonoBehaviour
{
	[SerializeField] private CanvasGroup m_canvasGroup;
	//[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private Image m_componentTypeIcon;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private StatDisplay[] m_statDisplays;

	private RepareUnitDisplay m_currentDisplayedComponent;

	private void Awake ()
	{
		RepareUnitDisplay.onDisplayHovered += OnComponentHovered;
	}

	public void Init ( EntitySavedData _componentSavedData )
	{
		if (_componentSavedData == null)
			return;

		//EntityEquipmentData componentData = _componentSavedData.GetData<EntityEquipmentData>();
		/*m_componentTypeIcon.sprite = componentData == null ? null : GameAssets.current.ui.componentIcons[componentData.GetEquipmentType()];
		m_corpIcon.sprite = componentData == null ? null : GameAssets.current.ui.corporationsIcons[componentData.faction];*/
		//m_icon.sprite = componentData.icon;
		m_titleTMP.text = _componentSavedData == null ? null : _componentSavedData.name;
		//m_priceTMP.text = componentData == null ? null : componentData.GetSellingPrice().Item2.ToString();

		EntityEquipmentData.StatDescription[] statsDescriptions = _componentSavedData.GetStatsDesciptions().Values.ToArray();
		for (int i = 0; i < m_statDisplays.Length; i++)
		{
			if (statsDescriptions.Length <= i)
				m_statDisplays[i].gameObject.SetActive(false);
			else
			{
				m_statDisplays[i].gameObject.SetActive(true);
				m_statDisplays[i].Init(statsDescriptions[i]);
			}
		}
	}

	private void OnComponentHovered ( RepareUnitDisplay _display )
	{
		if (!gameObject.activeInHierarchy || _display == m_currentDisplayedComponent)
			return;

		m_currentDisplayedComponent = _display;

		Init(_display.SavedData);
	}
}
