using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SquadUnitFullMicroDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private TextMeshProUGUI m_hpValueTMP;
	[SerializeField] private Image m_mainFactionIconImg;
	[SerializeField] private TextMeshProUGUI m_mainFactionPercentageTMP;
	[SerializeField] private StatusDisplay[] m_statusDisplays;

	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_conditionalStat;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_damagesSectionFilter;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_resistanceSectionFilter;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_statusSectionFilter;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, StatDisplay> m_staticDisplays;
	[SerializeField] private StatSectionDisplay m_damageSectionDisplay;
	[SerializeField] private StatSectionDisplay m_resistanceSectionDisplay;
	[SerializeField] private StatSectionDisplay m_statusSectionDisplay;

	private void Awake ()
	{
		PlayerController.onEntitySelected += OnEntitySelected;
	}

	private void OnDestroy ()
	{
		PlayerController.onEntitySelected -= OnEntitySelected;
	}

	private void OnEntitySelected (int? _entityID)
	{
		RefreshVisual(_entityID);
	}

	private void RefreshVisual ( int? _entityID )
	{
		if (_entityID == null || !_entityID.HasValue)
			return;

		Entity entity = GameManager.Instance.GetEntityFromID(_entityID.Value);
		m_nameTMP.text = entity.Data.name;
		m_hpValueTMP.text = entity.Equipment.CurrentHealth + "-" + entity.Equipment.MaxHealth;
		FrameEquipmentData.EntityFaction mainFaction = entity.Data.GetDominentFaction(out float percentage);
		m_mainFactionPercentageTMP.text = (percentage*100f).ToString() + "%" ;
		m_mainFactionIconImg.sprite = GameAssets.current.ui.corporationsIcons[mainFaction];

		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDescriptions = entity.Data.GetStatsDesciptions();
		List<EntityEquipmentData.SecondaryStat.StatType> keys = statsDescriptions.Keys.ToList();
		List<EntityEquipmentData.SecondaryStat.StatType> order = GameConfig.current.ui.statsDisplayOrder.ToList();
		keys.OrderByDescending(e => order.IndexOf(e));

		foreach(EntityEquipmentData.SecondaryStat.StatType statType in m_staticDisplays.Keys)
		{
			if (statsDescriptions.ContainsKey(statType) && (!m_conditionalStat.Contains(statType) || statsDescriptions[statType].floatValue > 0))
			{
				m_staticDisplays[statType].gameObject.SetActive(true);
				m_staticDisplays[statType].Init(statsDescriptions[statType]);
			}
			else
				m_staticDisplays[statType].gameObject.SetActive(false);
		}

		List<EntityEquipmentData.StatDescription> damageStats = new();
		List<EntityEquipmentData.StatDescription> resStats = new();
		List<EntityEquipmentData.StatDescription> statusStats = new();
		foreach (EntityEquipmentData.SecondaryStat.StatType statType in keys)
		{
			if (m_damagesSectionFilter.Contains(statType))
				damageStats.Add(statsDescriptions[statType]);
			else if (m_resistanceSectionFilter.Contains(statType))
				resStats.Add(statsDescriptions[statType]);
			else if (m_statusSectionFilter.Contains(statType))
				statusStats.Add(statsDescriptions[statType]);
		}
		if(damageStats.Count > 0)
		{
			m_damageSectionDisplay.Init("Damages", damageStats);
			m_damageSectionDisplay.gameObject.SetActive(true);
		}
		else
			m_damageSectionDisplay.gameObject.SetActive(false);
		if (resStats.Count > 0)
		{
			m_resistanceSectionDisplay.Init("Resistances", resStats);
			m_resistanceSectionDisplay.gameObject.SetActive(true);
		}
		else
			m_resistanceSectionDisplay.gameObject.SetActive(false);
		if (statusStats.Count > 0)
		{
			m_statusSectionDisplay.Init("Status stats", statusStats);
			m_statusSectionDisplay.gameObject.SetActive(true);
		}
		else
			m_statusSectionDisplay.gameObject.SetActive(false);

		for (int i = 0; i < m_statusDisplays.Length; i++)
		{
			if (entity.Status.Count <= i)
				m_statusDisplays[i].Hide();
			else
			{
				m_statusDisplays[i].Init(GameAssets.current.game.entityStatus[entity.Status[i]]);
				m_statusDisplays[i].Show();
			}
		}
	}

/*#if UNITY_EDITOR
	[Button]
	private void Test ()
	{
		foreach(EntityEquipmentData.SecondaryStat.StatType type in m_displayStaticStatsFilter)
		{
			if (!m_statDisplayOrder.Contains(type))
				Debug.Log("Missing type: " + type);
		}

		foreach (EntityEquipmentData.SecondaryStat.StatType type in m_displayConditionalStatsFilter)
		{
			if (!m_statDisplayOrder.Contains(type))
				Debug.Log("Missing type: " + type);
		}
	}

	[Button]
	private void Test2 ()
	{
		foreach (EntityEquipmentData.SecondaryStat.StatType type in m_statDisplayOrder)
		{
			if(!m_displayStaticStatsFilter.Contains(type) && !m_displayConditionalStatsFilter.Contains(type))
				Debug.Log("Missing type: " + type);
		}

		foreach (EntityEquipmentData.SecondaryStat.StatType type in m_statDisplayOrder)
		{
			if (m_displayStaticStatsFilter.Contains(type) && m_displayConditionalStatsFilter.Contains(type))
				Debug.Log("Extra type: " + type);
		}
	}

	[Button]
	private void Test3 ()
	{
		List<EntityEquipmentData.SecondaryStat.StatType> alreadySeen = new();
		foreach (EntityEquipmentData.SecondaryStat.StatType type in m_displayStaticStatsFilter)
		{
			if (alreadySeen.Contains(type))
			{
				Debug.Log("Double type in static: " + type);
				continue;
			}
			alreadySeen.Add(type);
		}

		alreadySeen.Clear();
		foreach (EntityEquipmentData.SecondaryStat.StatType type in m_displayConditionalStatsFilter)
		{
			if (alreadySeen.Contains(type))
			{
				Debug.Log("Double type in conditional: " + type);
				continue;
			}
			alreadySeen.Add(type);
		}
	}

#endif*/
}
