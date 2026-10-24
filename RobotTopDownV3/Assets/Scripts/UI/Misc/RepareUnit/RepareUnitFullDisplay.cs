using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

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

		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> damagedStats = GetDamagedComponentsStats(_componentSavedData);
		List<EntityEquipmentData.SecondaryStat.StatType> keys = UIManager.Instance.GetPanel<EntityConfigPanel>().GetDisplayedStats(damagedStats);
		for (int i = 0; i < m_statDisplays.Length; i++)
		{
			if (keys.Count <= i)
				m_statDisplays[i].gameObject.SetActive(false);
			else
			{
				m_statDisplays[i].gameObject.SetActive(true);
				m_statDisplays[i].Init(damagedStats[keys[i]]);
			}
		}
	}

	private static SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> GetDamagedComponentsStats ( EntitySavedData _unit )
	{
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> stats = new();
		foreach (GameDatas.PlayerSave.Component component in _unit.GetAllEquipments())
		{
			if (component == null || !component.isDamaged || !component.TryGetData(out EntityEquipmentData componentData))
				continue;

			foreach (EntityEquipmentData.StatDescription stat in componentData.GetDesciption())
			{
				if (stats.ContainsKey(stat.ID))
					stats[stat.ID].Add(stat);
				else
					stats.Add(stat.ID, stat);
			}
		}

		stats.Remove(EntityEquipmentData.SecondaryStat.StatType.EnergyCost);
		return stats;
	}

	private void OnComponentHovered ( RepareUnitDisplay _display )
	{
		if (!gameObject.activeInHierarchy || _display == m_currentDisplayedComponent)
			return;

		m_currentDisplayedComponent = _display;

		Init(_display.SavedData);
	}
}
