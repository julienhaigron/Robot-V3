using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class RepareUnitFullDisplay : MonoBehaviour
{
	[System.Serializable]
	private struct DamageRow
	{
		public GameObject root;
		public Image icon;
		public TextMeshProUGUI nameTMP;
		public TextMeshProUGUI kindTMP;
	}

	[SerializeField] private GameObject m_contentRoot;
	[SerializeField] private GameObject m_emptyGO;

	[Header("Header")]
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_factionIcon;
	[SerializeField] private TextMeshProUGUI m_summaryTMP;
	[SerializeField] private GameObject m_outOfServiceGO;

	[Header("Damage and losses")]
	[SerializeField] private DamageRow[] m_damageRows;
	[SerializeField] private TextMeshProUGUI m_moreDamagesTMP;
	[SerializeField] private StatDisplay[] m_statDisplays;

	[Header("Repairs")]
	[SerializeField] private TextMeshProUGUI m_necessaryPriceTMP;
	[SerializeField] private TextMeshProUGUI m_necessaryDescriptionTMP;
	[SerializeField] private TextMeshProUGUI m_fullPriceTMP;
	[SerializeField] private TextMeshProUGUI m_fullDescriptionTMP;

	[SerializeField] private Color m_damagedIconColor = new(.91f, .27f, .24f);
	[SerializeField] private Color m_mainKindColor = new(.91f, .27f, .24f);
	[SerializeField] private Color m_secondaryKindColor = new(.6f, .61f, .69f);
	[SerializeField] private ContentSwap m_swap;

	private EntitySavedData m_unit;

	private void Awake ()
	{
		RepareUnitDisplay.onDisplayHovered += OnDisplayHovered;
	}

	private void OnDestroy ()
	{
		RepareUnitDisplay.onDisplayHovered -= OnDisplayHovered;
	}

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += Refresh;
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= Refresh;
	}

	public void Show ( EntitySavedData _unit )
	{
		if (_unit != null && m_unit != null && _unit != m_unit && m_swap != null)
			m_swap.Play();
		m_unit = _unit;
		Refresh();
	}

	private void OnDisplayHovered ( RepareUnitDisplay _display )
	{
		if (gameObject.activeInHierarchy && _display.SavedData != null && _display.SavedData != m_unit)
			Show(_display.SavedData);
	}

	private void Refresh ()
	{
		bool hasUnit = m_unit != null && !string.IsNullOrEmpty(m_unit.name);
		m_contentRoot.SetActive(hasUnit);
		m_emptyGO.SetActive(!hasUnit);
		if (!hasUnit)
			return;

		UnitRepairInfo info = new(m_unit);

		m_icon.sprite = m_unit.FrameData != null ? m_unit.FrameData.icon : null;
		m_titleTMP.text = m_unit.name;
		m_factionIcon.sprite = GameAssets.current.ui.corporationsIcons[m_unit.GetDominentFaction(out _)];
		m_summaryTMP.text = string.Format(LocalizationManager.Instance.Get(info.parts.Count == 1 ? LocalizationKey.hub_repair_summary_one : LocalizationKey.hub_repair_summary),
			m_unit.FrameData != null ? m_unit.FrameData.GetLocalizedName() : string.Empty, info.componentCount, info.parts.Count);
		m_outOfServiceGO.SetActive(info.IsOutOfService);

		RefreshDamageRows(info);
		RefreshLosses();

		m_necessaryPriceTMP.text = info.necessaryPriceText;
		m_necessaryDescriptionTMP.text = info.IsOutOfService
			? string.Format(LocalizationManager.Instance.Get(LocalizationKey.hub_necessary_description),
				string.Join(", ", info.parts.Where(part => part.isMain).Select(part => EntityEquipmentData.GetLocalizedTypeName(part.Type)).Distinct()))
			: LocalizationManager.Instance.Get(LocalizationKey.hub_necessary_none);
		m_fullPriceTMP.text = info.fullPriceText;
		m_fullDescriptionTMP.text = string.Format(LocalizationManager.Instance.Get(info.parts.Count == 1 ? LocalizationKey.hub_full_description_one : LocalizationKey.hub_full_description), info.parts.Count);
	}

	private void RefreshDamageRows ( UnitRepairInfo _info )
	{
		string mainLabel = LocalizationManager.Instance.Get(LocalizationKey.hub_repair_main);
		string secondaryLabel = LocalizationManager.Instance.Get(LocalizationKey.hub_repair_secondary);

		for (int i = 0; i < m_damageRows.Length; i++)
		{
			bool isShown = i < _info.parts.Count;
			m_damageRows[i].root.SetActive(isShown);
			if (!isShown)
				continue;

			UnitRepairInfo.DamagedPart part = _info.parts[i];
			m_damageRows[i].icon.sprite = GameAssets.current.ui.componentIcons[part.Type];
			m_damageRows[i].icon.color = m_damagedIconColor;
			m_damageRows[i].nameTMP.text = part.data.GetLocalizedName();
			m_damageRows[i].kindTMP.text = part.isMain ? mainLabel : secondaryLabel;
			m_damageRows[i].kindTMP.color = part.isMain ? m_mainKindColor : m_secondaryKindColor;
		}

		int hiddenCount = _info.parts.Count - m_damageRows.Length;
		m_moreDamagesTMP.gameObject.SetActive(hiddenCount > 0);
		if (hiddenCount > 0)
			m_moreDamagesTMP.text = string.Format(LocalizationManager.Instance.Get(hiddenCount == 1 ? LocalizationKey.hub_repair_more_one : LocalizationKey.hub_repair_more), hiddenCount);
	}

	private void RefreshLosses ()
	{
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> damagedStats = GetDamagedComponentsStats(m_unit);
		List<EntityEquipmentData.SecondaryStat.StatType> keys = UIManager.Instance.GetPanel<EntityConfigPanel>().GetDisplayedStats(damagedStats);
		for (int i = 0; i < m_statDisplays.Length; i++)
		{
			bool isShown = i < keys.Count;
			m_statDisplays[i].gameObject.SetActive(isShown);
			if (isShown)
				m_statDisplays[i].Init(damagedStats[keys[i]]);
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
				EntitySavedData.MergeDescription(stats, stat);
		}

		stats.Remove(EntityEquipmentData.SecondaryStat.StatType.EnergyCost);
		return stats;
	}
}
