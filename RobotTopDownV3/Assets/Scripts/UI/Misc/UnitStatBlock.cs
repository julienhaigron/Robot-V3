using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StatType = EntityEquipmentData.SecondaryStat.StatType;
using StatDescription = EntityEquipmentData.StatDescription;
using StatDescriptions = SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription>;
using VisionType = NeuronalMembraneEquipmentData.VisionTypes;
using DamageType = WeaponEquipmentData.DamageType;

public class UnitStatBlock : MonoBehaviour
{
	[Serializable]
	private struct StatCell
	{
		public TextMeshProUGUI value;
		public TextMeshProUGUI delta;
		public RectTransform fill;
		public Image deltaSegment;
		public ToolTipTrigger tooltip;
	}

	[Serializable]
	private struct BonusChip
	{
		public GameObject root;
		public Image fill;
		public Image frame;
		public Image icon;
		public TextMeshProUGUI label;
		public ToolTipTrigger tooltip;
	}

	[Serializable]
	private struct DefenseTile
	{
		public Image icon;
		public TextMeshProUGUI value;
		public ToolTipTrigger tooltip;
	}

	private readonly struct Bonus
	{
		public readonly StatDescription description;
		public readonly Color color;
		public readonly string tip;

		public Bonus ( StatDescription _description, Color _color, string _tip )
		{
			description = _description;
			color = _color;
			tip = _tip;
		}
	}

	private static readonly VisionType[] s_visions = { VisionType.Optic, VisionType.Thermic, VisionType.Radar };
	private static readonly DamageType[] s_damageTypes = (DamageType[])Enum.GetValues(typeof(DamageType));

	[Header("Vitals")]
	[SerializeField] private bool m_showHp = true;
	[SerializeField] private bool m_showEnergyCap;
	[SerializeField] private GameObject m_hpPill;
	[SerializeField] private TextMeshProUGUI m_hpTMP;
	[SerializeField] private TextMeshProUGUI m_hpDeltaTMP;
	[SerializeField] private ToolTipTrigger m_hpTooltip;
	[SerializeField] private TextMeshProUGUI m_energyTMP;
	[SerializeField] private TextMeshProUGUI m_energyCapTMP;
	[SerializeField] private TextMeshProUGUI m_energyDeltaTMP;
	[SerializeField] private ToolTipTrigger m_energyTooltip;
	[SerializeField] private Image m_visionIcon;
	[SerializeField] private TextMeshProUGUI m_visionTMP;
	[SerializeField] private ToolTipTrigger m_visionTooltip;

	[Header("Combat matrix")]
	[SerializeField] private StatCell m_rangedAccuracy;
	[SerializeField] private StatCell m_rangedEvasion;
	[SerializeField] private StatCell m_meleeAccuracy;
	[SerializeField] private StatCell m_meleeEvasion;

	[Header("Defense, bonuses, hidden stats")]
	[SerializeField] private DefenseTile[] m_stealthTiles;
	[SerializeField] private ToolTipTrigger m_stealthTooltip;
	[SerializeField] private DefenseTile[] m_resistanceTiles;
	[SerializeField] private Transform m_resistanceSeparator;
	[SerializeField] private ToolTipTrigger m_resistanceTooltip;
	[SerializeField] private HorizontalLayoutGroup m_bonusRow;
	[SerializeField] private TextMeshProUGUI m_noBonusTMP;
	[SerializeField] private BonusChip[] m_bonusChips;
	[SerializeField] private GameObject m_hiddenLine;

	[Header("Style")]
	[SerializeField] private Color m_neutralColor = new(.6f, .64f, .72f);
	[SerializeField] private Color m_textColor = new(.93f, .95f, .97f);
	[SerializeField] private Color m_upColor = new(.32f, .89f, .49f);
	[SerializeField] private Color m_downColor = new(1f, .42f, .42f);
	[SerializeField] private Color m_damageColor = new(1f, .55f, .26f);
	[SerializeField] private Color m_resistanceColor = new(.36f, .78f, 1f);
	[SerializeField] private Color m_statusColor = new(.96f, .84f, .43f);
	[SerializeField] private Color m_chipBaseColor = new(.05f, .04f, .11f, .96f);
	[SerializeField, Range(0f, 1f)] private float m_chipFillTint = .16f;
	[SerializeField, Range(0f, 1f)] private float m_chipFrameAlpha = .55f;
	[SerializeField, Range(0f, 1f)] private float m_conditionalFrameAlpha = .22f;
	[SerializeField, Range(0f, 1f)] private float m_zeroAlpha = .32f;

	private readonly List<Bonus> m_bonuses = new();
	private EntitySavedData m_data;
	private EntitySavedData m_preview;
	private bool m_areStatsVisible;

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += Refresh;
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= Refresh;
	}

	private void Refresh ()
	{
		if (m_data != null)
			Init(m_data, m_areStatsVisible, m_preview);
	}

	public void Init ( EntitySavedData _data, bool _areStatsVisible, EntitySavedData _preview = null )
	{
		m_data = _data;
		m_preview = _preview;
		m_areStatsVisible = _areStatsVisible;

		EntitySavedData shownData = _preview ?? _data;
		StatDescriptions current = _data.GetStatsDesciptions();
		StatDescriptions shown = _preview == null ? current : _preview.GetStatsDesciptions();
		bool isPreview = _preview != null && _areStatsVisible;
		LocalizationManager localization = LocalizationManager.Instance;

		int maxHp = shownData.GetMaxHealth();
		m_hpPill.SetActive(m_showHp);
		m_hpTMP.text = _areStatsVisible ? maxHp.ToString() : "?";
		SetDelta(m_hpDeltaTMP, isPreview, maxHp - _data.GetMaxHealth(), false);
		m_hpTooltip.Init(StatType.BaseHp.GetLocalizedTitle(), localization.Get(LocalizationKey.stat_tip_hp));

		ReactorEquipmentData reactor = shownData.ReactorData;
		bool hasCap = m_showEnergyCap && _areStatsVisible && reactor != null;
		m_energyTMP.text = _areStatsVisible ? Mathf.RoundToInt(Value(shown, StatType.EnergyCost)).ToString() : "?";
		m_energyCapTMP.gameObject.SetActive(hasCap);
		if (hasCap)
			m_energyCapTMP.text = "/ " + reactor.energyProduced;
		SetDelta(m_energyDeltaTMP, isPreview, Delta(shown, current, StatType.EnergyCost, 1f), true);
		m_energyTooltip.Init(StatType.EnergyCost.GetLocalizedTitle(), localization.Get(LocalizationKey.stat_tip_energy));

		NeuronalMembraneEquipmentData membrane = _areStatsVisible ? shownData.NeuronalMembraneData : null;
		m_visionIcon.color = membrane == null ? m_neutralColor : GameConfig.current.ui.GetVisionColor(membrane.visionType);
		m_visionTMP.text = membrane != null ? membrane.visionType.GetLocalizedTitle() : _areStatsVisible ? "-" : "?";
		m_visionTooltip.Init(StatType.VisionType.GetLocalizedTitle(), membrane == null ? "" : VisionTip(membrane.visionType, shownData.GetVisionRange()));

		string flank = MaxFlankRatio().ToPercentText();
		SetCell(m_rangedAccuracy, StatType.DistanceAccuracy, string.Format(localization.Get(LocalizationKey.stat_tip_ranged_accuracy), flank), current, shown, _areStatsVisible, isPreview);
		SetCell(m_rangedEvasion, StatType.DistanceEvasion, string.Format(localization.Get(LocalizationKey.stat_tip_ranged_evasion), GameConfig.current.game.entityCoverBonus.ToPercentText()), current, shown, _areStatsVisible, isPreview);
		SetCell(m_meleeAccuracy, StatType.MeleeAccuracy, string.Format(localization.Get(LocalizationKey.stat_tip_melee_accuracy), flank), current, shown, _areStatsVisible, isPreview);
		SetCell(m_meleeEvasion, StatType.MeleeEvasion, localization.Get(LocalizationKey.stat_tip_melee_evasion), current, shown, _areStatsVisible, isPreview);

		StringBuilder stealthTip = new(localization.Get(LocalizationKey.stat_tip_stealth));
		for (int i = 0; i < s_visions.Length && i < m_stealthTiles.Length; i++)
		{
			float stealth = Value(shown, NeuronalMembraneEquipmentData.GetCamoStat(s_visions[i]));
			string vision = s_visions[i].GetLocalizedTitle();
			SetTile(m_stealthTiles[i], GameConfig.current.ui.GetVisionColor(s_visions[i]), stealth, vision, _areStatsVisible);
			if (_areStatsVisible)
				stealthTip.Append('\n').Append(vision).Append(' ').Append(stealth.ToPercentText());
		}
		m_stealthTooltip.Init(localization.Get(LocalizationKey.stat_block_stealth), stealthTip.ToString());

		SetResistances(shownData, _areStatsVisible);
		SetBonuses(shown, shownData, _areStatsVisible);
	}

	private void SetCell ( StatCell _cell, StatType _stat, string _tip, StatDescriptions _current, StatDescriptions _shown, bool _isVisible, bool _isPreview )
	{
		float value = _isVisible ? Value(_shown, _stat) : 0f;
		float baseValue = Value(_current, _stat);
		_cell.value.text = _isVisible ? Mathf.RoundToInt(value * 100f).ToString() : "?";
		_cell.fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
		SetDelta(_cell.delta, _isPreview, Delta(_shown, _current, _stat, 100f), false);

		bool hasSegment = _isPreview && !Mathf.Approximately(value, baseValue);
		_cell.deltaSegment.gameObject.SetActive(hasSegment);
		if (hasSegment)
		{
			RectTransform segment = _cell.deltaSegment.rectTransform;
			segment.anchorMin = new Vector2(Mathf.Clamp01(Mathf.Min(value, baseValue)), 0f);
			segment.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(value, baseValue)), 1f);
			_cell.deltaSegment.color = value > baseValue ? m_upColor : m_downColor;
		}

		_cell.tooltip.Init(_stat.GetLocalizedTitle(), _tip);
	}

	private void SetDelta ( TextMeshProUGUI _tmp, bool _isShown, int _delta, bool _isCost )
	{
		bool isVisible = _isShown && _delta != 0;
		_tmp.gameObject.SetActive(isVisible);
		if (!isVisible)
			return;

		_tmp.text = (_delta > 0 ? "+" : "-") + Mathf.Abs(_delta);
		_tmp.color = (_delta > 0) != _isCost ? m_upColor : m_downColor;
	}

	private void SetResistances ( EntitySavedData _data, bool _isVisible )
	{
		GameConfig.GameSettings config = GameConfig.current.game;
		LocalizationManager localization = LocalizationManager.Instance;
		StringBuilder tip = new(localization.Get(LocalizationKey.stat_tip_resistances));
		DamageType[] types = s_damageTypes.OrderBy(type => config.damageCategoryPerDamageType[type]).ThenBy(type => type).ToArray();
		for (int i = 0; i < m_resistanceTiles.Length && i < types.Length; i++)
		{
			DamageType type = types[i];
			float resistance = 1f - _data.GetPermanentDamageTakenFactor(type);
			if (GameAssets.current.ui.damageIconPerType.TryGetValue(type, out Sprite sprite))
				m_resistanceTiles[i].icon.sprite = sprite;
			Color color = GameAssets.current.ui.damageTypeColors.TryGetValue(type, out Color typeColor) ? typeColor : m_neutralColor;
			SetTile(m_resistanceTiles[i], color, resistance, type.GetLocalizedTitle(), _isVisible);
			if (_isVisible)
				tip.Append('\n').Append(type.GetLocalizedTitle()).Append(' ').Append(resistance.ToPercentText());
		}

		WeaponEquipmentData.DamageCategory firstCategory = config.damageCategoryPerDamageType[types[0]];
		m_resistanceSeparator.SetSiblingIndex(types.Count(type => config.damageCategoryPerDamageType[type] == firstCategory));
		m_resistanceTooltip.Init(localization.Get(LocalizationKey.stat_block_resistances), tip.ToString());
	}

	private void SetTile ( DefenseTile _tile, Color _color, float _value, string _name, bool _isVisible )
	{
		float alpha = _isVisible && Mathf.Abs(_value) >= .005f ? 1f : m_zeroAlpha;
		Color textColor = _isVisible && _value < 0f ? m_downColor : m_textColor;
		_tile.icon.color = new Color(_color.r, _color.g, _color.b, alpha);
		_tile.value.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
		_tile.value.text = _isVisible ? Mathf.RoundToInt(_value * 100f).ToString() : "?";
		_tile.tooltip.Init(_name, _isVisible ? _value.ToPercentText() : "?");
	}

	private static bool IsDamageResistance ( StatType _stat )
	{
		GameConfig.GameSettings config = GameConfig.current.game;
		return _stat == StatType.GeneralDamageResistance
			|| config.statTypePerDamageResistanceType.ContainsValue(_stat)
			|| config.statTypePerResistanceCategory.ContainsValue(_stat);
	}

	private void SetBonuses ( StatDescriptions _stats, EntitySavedData _data, bool _isVisible )
	{
		m_bonuses.Clear();
		if (_isVisible)
		{
			GameConfig.UI ui = GameConfig.current.ui;
			AddBonuses(ui.damageStats, _stats);
			AddBonuses(ui.resistanceStats, _stats);
			AddBonuses(ui.statusStats, _stats);
			AddConditionalBonuses(_data);
		}

		int regularCount = CountFittingChips();
		bool hasOverflow = regularCount < m_bonuses.Count;
		for (int i = 0; i < m_bonusChips.Length; i++)
		{
			bool isOverflowChip = hasOverflow && i == regularCount;
			m_bonusChips[i].root.SetActive(i < regularCount || isOverflowChip);
			if (i < regularCount)
				SetChip(m_bonusChips[i], m_bonuses[i]);
			else if (isOverflowChip)
				SetOverflowChip(m_bonusChips[i], regularCount);
		}

		m_bonusRow.gameObject.SetActive(_isVisible);
		m_noBonusTMP.gameObject.SetActive(m_bonuses.Count == 0);
		m_hiddenLine.SetActive(!_isVisible);
	}

	private int CountFittingChips ()
	{
		float available = ((RectTransform)transform).rect.width;
		float spacing = m_bonusRow.spacing;
		int total = m_bonuses.Count;
		int count = Mathf.Min(total, m_bonusChips.Length);
		while (count > 0 && ChipsWidth(count, spacing) > available)
			count--;
		if (count == total)
			return count;

		count = Mathf.Min(count, m_bonusChips.Length - 1);
		while (count > 0 && ChipsWidth(count, spacing) + spacing + ChipWidth(m_bonusChips[count], OverflowLabel(count)) > available)
			count--;
		return count;
	}

	private float ChipsWidth ( int _count, float _spacing )
	{
		float width = 0f;
		for (int i = 0; i < _count; i++)
			width += (i > 0 ? _spacing : 0f) + ChipWidth(m_bonusChips[i], ChipLabel(m_bonuses[i].description));
		return width;
	}

	private static float ChipWidth ( BonusChip _chip, string _label )
	{
		HorizontalLayoutGroup layout = _chip.root.GetComponent<HorizontalLayoutGroup>();
		return layout.padding.horizontal + layout.spacing + LayoutUtility.GetPreferredWidth(_chip.icon.rectTransform) + _chip.label.GetPreferredValues(_label).x;
	}

	private string OverflowLabel ( int _from )
	{
		return "+" + (m_bonuses.Count - _from);
	}

	private static string ChipLabel ( StatDescription _description )
	{
		return FormatBonus(_description) + " " + _description.ID.GetLocalizedShortTitle();
	}

	private void AddBonuses ( StatType[] _family, StatDescriptions _stats )
	{
		foreach (StatType stat in _family)
			if (_stats.TryGetValue(stat, out StatDescription description) && description.floatValue != 0f)
				AddBonus(description);
	}

	private void AddConditionalBonuses ( EntitySavedData _data )
	{
		foreach (ChipsetEquipmentData.ConditionalStatBonus chipsetBonus in _data.GetChipsetBonuses())
		{
			if (chipsetBonus.conditionType == Condition.ConditionType.Noone || chipsetBonus.bonus.value == 0f)
				continue;

			StatDescription description = chipsetBonus.bonus.GetDescription();
			description.condition = chipsetBonus.conditionType;
			AddBonus(description);
		}
	}

	private void AddBonus ( StatDescription _description )
	{
		GameConfig.UI ui = GameConfig.current.ui;
		LocalizationManager localization = LocalizationManager.Instance;
		StatType stat = _description.ID;
		if (Array.IndexOf(ui.damageStats, stat) >= 0)
			m_bonuses.Add(new Bonus(_description, m_damageColor, localization.Get(LocalizationKey.stat_tip_bonus_damage)));
		else if (Array.IndexOf(ui.statusStats, stat) >= 0)
			m_bonuses.Add(new Bonus(_description, m_statusColor, localization.Get(LocalizationKey.stat_tip_bonus_status)));
		else if (Array.IndexOf(ui.resistanceStats, stat) >= 0 || IsDamageResistance(stat))
			m_bonuses.Add(new Bonus(_description, m_resistanceColor, localization.Get(LocalizationKey.stat_tip_bonus_resistance)));
		else
			m_bonuses.Add(new Bonus(_description, m_neutralColor, string.Empty));
	}

	private void SetChip ( BonusChip _chip, Bonus _bonus )
	{
		StatDescription description = _bonus.description;
		Paint(_chip, _bonus.color, description.IsConditional);
		_chip.label.text = ChipLabel(description);
		_chip.label.fontStyle = description.IsConditional ? FontStyles.Italic : FontStyles.Normal;

		string tip = _bonus.tip;
		if (description.IsConditional)
		{
			string condition = string.Format(LocalizationManager.Instance.Get(LocalizationKey.stat_tip_conditional), description.condition.GetLocalizedTitle());
			tip = string.IsNullOrEmpty(tip) ? condition : tip + "\n" + condition;
		}
		_chip.tooltip.Init(description.title, tip);
	}

	private void SetOverflowChip ( BonusChip _chip, int _from )
	{
		Paint(_chip, m_neutralColor, false);
		_chip.label.text = OverflowLabel(_from);
		_chip.label.fontStyle = FontStyles.Normal;

		StringBuilder lines = new();
		for (int i = _from; i < m_bonuses.Count; i++)
		{
			if (lines.Length > 0)
				lines.Append('\n');
			lines.Append(m_bonuses[i].description.FullTitle).Append(' ').Append(FormatBonus(m_bonuses[i].description));
		}
		_chip.tooltip.Init(LocalizationManager.Instance.Get(LocalizationKey.stat_block_more), lines.ToString());
	}

	private void Paint ( BonusChip _chip, Color _color, bool _isConditional )
	{
		_chip.fill.color = Color.Lerp(m_chipBaseColor, new Color(_color.r, _color.g, _color.b, m_chipBaseColor.a), m_chipFillTint);
		_chip.frame.color = new Color(_color.r, _color.g, _color.b, _isConditional ? m_conditionalFrameAlpha : m_chipFrameAlpha);
		_chip.icon.color = _color;
	}

	private static string VisionTip ( VisionType _vision, int _range )
	{
		LocalizationKey key = _vision switch
		{
			VisionType.Thermic => LocalizationKey.stat_tip_vision_thermic,
			VisionType.Radar => LocalizationKey.stat_tip_vision_radar,
			_ => LocalizationKey.stat_tip_vision_optic
		};
		return string.Format(LocalizationManager.Instance.Get(key), _range);
	}

	private static float MaxFlankRatio ()
	{
		float max = 0f;
		foreach (float ratio in GameConfig.current.game.entityFlankRatio.Values)
			max = Mathf.Max(max, ratio);
		return max;
	}

	private static float Value ( StatDescriptions _stats, StatType _stat )
	{
		return _stats.TryGetValue(_stat, out StatDescription description) ? description.floatValue : 0f;
	}

	private static int Delta ( StatDescriptions _shown, StatDescriptions _current, StatType _stat, float _scale )
	{
		return Mathf.RoundToInt(Value(_shown, _stat) * _scale) - Mathf.RoundToInt(Value(_current, _stat) * _scale);
	}

	private static string FormatBonus ( StatDescription _description )
	{
		string sign = _description.floatValue > 0f ? "+" : "";
		return sign + (_description.Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Percentage
			? _description.floatValue.ToPercentText()
			: Mathf.RoundToInt(_description.floatValue).ToString());
	}
}
