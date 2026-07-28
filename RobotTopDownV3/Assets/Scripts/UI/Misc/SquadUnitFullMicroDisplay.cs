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
	[SerializeField] private StatDisplay[] m_statDisplays;
	[SerializeField] private StatusDisplay[] m_statusDisplays;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_displayStaticStatsFilter;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_displayConditionalStatsFilter;

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
		foreach(EntityEquipmentData.SecondaryStat.StatType stat in keys.ToArray())
		{
			bool conditionalPredicate = m_displayConditionalStatsFilter.Contains(stat) 
				&& (statsDescriptions[stat].floatValue != 0 || statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.String || statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Cell);
			bool staticPredicate = m_displayStaticStatsFilter.Contains(stat);
			if (!conditionalPredicate && !staticPredicate)
				keys.Remove(stat);
		}
		List<EntityEquipmentData.SecondaryStat.StatType> order = GameConfig.current.ui.statsDisplayOrder.ToList();
		keys.OrderByDescending(e => order.IndexOf(e));

		for (int i = 0; i < m_statDisplays.Length; i++)
		{
			if (keys.Count <= i)
				m_statDisplays[i].gameObject.SetActive(false);
			else
			{
				m_statDisplays[i].gameObject.SetActive(true);
				m_statDisplays[i].Init(statsDescriptions[keys[i]]);
			}
		}

		for(int i = 0; i < m_statusDisplays.Length; i++)
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
