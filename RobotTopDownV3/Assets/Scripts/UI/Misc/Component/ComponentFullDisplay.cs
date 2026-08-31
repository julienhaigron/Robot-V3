using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComponentFullDisplay : MonoBehaviour
{
	[SerializeField] private CanvasGroup m_canvasGroup;
	//[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private Image m_componentTypeIcon;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private StatDisplay[] m_statDisplays;

	private ComponentDisplay m_currentDisplayedComponent;

	private void Awake ()
	{
		ComponentDisplay.onDisplayHovered += OnComponentHovered;
	}

	public void Init ( GameDatas.PlayerSave.Component _componentSavedData )
	{
		EntityEquipmentData componentData = _componentSavedData != null ? _componentSavedData.GetData<EntityEquipmentData>() : null;
		m_componentTypeIcon.sprite = componentData == null ? null : GameAssets.current.ui.componentIcons[componentData.GetEquipmentType()];
		m_componentTypeIcon.gameObject.SetActive(componentData != null);
		m_corpIcon.sprite = componentData == null ? null : GameAssets.current.ui.corporationsIcons[componentData.faction];
		m_corpIcon.gameObject.SetActive(componentData != null);
		//m_icon.sprite = componentData.icon;
		m_titleTMP.text = componentData == null ? null : componentData.displayName;
		m_priceTMP.text = componentData == null ? null : componentData.GetSellingPrice().Item2.ToString();

		if (componentData != null)
		{
			EntityEquipmentData.StatDescription[] statsDescriptions = componentData.GetDesciption();
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
		else
		{
			foreach(StatDisplay statDisplay in m_statDisplays)
				statDisplay.gameObject.SetActive(false);
		}
	}

	private void OnComponentHovered ( ComponentDisplay _display )
	{
		if (!gameObject.activeInHierarchy || _display == m_currentDisplayedComponent)
			return;

		m_currentDisplayedComponent = _display;

		Init(_display.SavedData);
	}
}
