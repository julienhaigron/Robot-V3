using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;

public class SquadUnitFullMicroDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private TextMeshProUGUI m_hpValueTMP;
	[SerializeField] private Image m_mainFactionIconImg;
	[SerializeField] private TextMeshProUGUI m_mainFactionPercentageTMP;
	[SerializeField] private StatDisplay[] m_statDisplays;
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	private Entity m_linkedEntity;

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

		SerializableDictionary<EntityEquipmentData.StatBonus.StatType, EntityEquipmentData.StatDescription> statsDescriptions = entity.Data.GetStatsDesciptions();
		List<EntityEquipmentData.StatBonus.StatType> keys = statsDescriptions.Keys.ToList();
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
	}
}
