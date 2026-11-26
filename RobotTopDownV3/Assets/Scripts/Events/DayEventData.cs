using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "DayEvent", menuName = "ScriptableObject/DayEvent")]
public class DayEventData : ScriptableObject
{
	public enum Targeting { None, Random, Fixed }
	public enum RecyclingDurationRule { None, SetTo, ReduceBy }

	public string displayName;
	[TextArea(2, 5)] public string lore;
	public Sprite illustration;
	public bool isInRandomPool = true;
	[ShowIf(nameof(isInRandomPool)), Min(0f)] public float randomWeight = 1f;

	[Title("Faction"), InfoBox("The faction of the unit buff, the corpo credit bonus and the enemy squads.")]
	public Targeting factionTargeting;
	[ShowIf("@factionTargeting == Targeting.Fixed")] public EntityEquipmentData.EntityFaction fixedFaction = EntityEquipmentData.EntityFaction.Commando;

	[Title("Shops")]
	public Targeting shopTargeting;
	[ShowIf("@shopTargeting == Targeting.Random"), MinMaxSlider(1, 3, true)] public Vector2Int randomShopCount = new(1, 3);
	[ShowIf("@shopTargeting == Targeting.Fixed")] public EntityEquipmentData.EntityFaction[] fixedShops = { EntityEquipmentData.EntityFaction.Commando };
	[ShowIf("@shopTargeting != Targeting.None"), Range(0f, 1f)] public float shopDiscount;
	[ShowIf("@shopTargeting != Targeting.None"), Min(0)] public int shopExtraItems;

	[Title("Repair station")]
	[Range(0f, 1f)] public float repairDiscount;
	[Min(0)] public int freeRandomRepairs;

	[Title("Recycling")]
	public RecyclingDurationRule recyclingDurationRule;
	[ShowIf("@recyclingDurationRule != RecyclingDurationRule.None"), Min(0)] public int recyclingDurationValue = 1;
	[Min(1f)] public float recyclingYieldFactor = 1f;

	[Title("Matches of the day")]
	[Range(.1f, 1f)] public float matchCountFactor = 1f;
	public bool forcesMatchType;
	[ShowIf(nameof(forcesMatchType))] public MissionData.MissionType forcedMatchType = MissionData.MissionType.Sabotage;
	[ShowIf("@factionTargeting != Targeting.None"), Range(0f, 1f)] public float enemyFactionShare;

	[Title("Match rewards")]
	[Min(1f)] public float creditFactor = 1f;
	public bool doublesComponents;
	[ShowIf("@factionTargeting != Targeting.None"), Min(0), InfoBox("Corpo credits of the event faction, granted on victory.")] public int corpoCreditBonus;

	[Title("Units of the event faction")]
	[ShowIf("@factionTargeting != Targeting.None"), Range(0f, 1f)] public float factionDamageBonus;
	[ShowIf("@factionTargeting != Targeting.None"), Range(0f, 1f)] public float factionHealthBonus;
}
