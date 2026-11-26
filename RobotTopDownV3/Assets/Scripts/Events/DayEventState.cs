using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

[Serializable]
public class DayEventState
{
	public string eventName = "";
	public EntityEquipmentData.EntityFaction faction = EntityEquipmentData.EntityFaction.Noone;
	public List<EntityEquipmentData.EntityFaction> shops = new();
	public List<string> repairedComponentNames = new();

	public DayEventData Data => Find(eventName);
	public bool IsActive => Data != null;

	public static DayEventData Find ( string _eventName )
	{
		return string.IsNullOrEmpty(_eventName) ? null : GameAssets.current.game.dayEvents.Find(dayEvent => dayEvent != null && dayEvent.name == _eventName);
	}

	#region Roll

	public static DayEventState Roll ( GameDatas.PlayerSave _save, string _previousEventName )
	{
		DayEventState state = new();
		DayEventData data = PickEvent(_save, _previousEventName);
		if (data == null)
			return state;

		state.eventName = data.name;
		state.faction = data.factionTargeting switch
		{
			DayEventData.Targeting.Fixed => data.fixedFaction,
			DayEventData.Targeting.Random => GameDatas.PlayerSave.DayData.ShopFactions.ToList().RandomElement(),
			_ => EntityEquipmentData.EntityFaction.Noone
		};
		state.shops = data.shopTargeting switch
		{
			DayEventData.Targeting.Fixed => data.fixedShops.Distinct().ToList(),
			DayEventData.Targeting.Random => GameDatas.PlayerSave.DayData.ShopFactions.OrderBy(_ => UnityEngine.Random.value)
				.Take(UnityEngine.Random.Range(data.randomShopCount.x, data.randomShopCount.y + 1)).ToList(),
			_ => new()
		};
		return state;
	}

	private static DayEventData PickEvent ( GameDatas.PlayerSave _save, string _previousEventName )
	{
		DayEventData pending = Find(_save.pendingDayEventName);
		_save.pendingDayEventName = "";

		DayEventData scripted = FindScripted(_save.cycleCount, _save.dayCount);
		if (scripted != null)
			return scripted;
		if (pending != null)
			return pending;

		return CanRollRandomEvent(_save) && UnityEngine.Random.value < GameConfig.current.game.randomDayEventChance ? PickRandom(_previousEventName) : null;
	}

	public static DayEventData FindScripted ( int _cycleCount, int _dayCount )
	{
		return GameConfig.current.game.scriptedDayEvents.Find(entry => entry.dayEvent != null && entry.cycle == _cycleCount + 1 && entry.day == _dayCount + 1)?.dayEvent;
	}

	private static bool CanRollRandomEvent ( GameDatas.PlayerSave _save )
	{
		return !_save.IsTutorialCycle && (_save.dayCount <= 3 || (_save.dayCount >= 5 && _save.cycleData.hasDeclinedTournament));
	}

	public static DayEventData PickRandom ( string _excludedEventName )
	{
		List<DayEventData> pool = GameAssets.current.game.dayEvents
			.Where(dayEvent => dayEvent != null && dayEvent.isInRandomPool && dayEvent.randomWeight > 0f && dayEvent.name != _excludedEventName).ToList();
		float roll = UnityEngine.Random.value * pool.Sum(dayEvent => dayEvent.randomWeight);
		foreach (DayEventData dayEvent in pool)
		{
			roll -= dayEvent.randomWeight;
			if (roll < 0f)
				return dayEvent;
		}

		return pool.Count > 0 ? pool[^1] : null;
	}

	public void ApplyDayStartRepairs ( GameDatas.PlayerSave _save )
	{
		repairedComponentNames.Clear();
		DayEventData data = Data;
		if (data == null || data.freeRandomRepairs <= 0)
			return;

		List<GameDatas.PlayerSave.Component> damaged = _save.allBuiltUnits.Where(unit => unit != null)
			.SelectMany(unit => unit.GetAllEquipments()).Where(component => component != null && component.isDamaged).ToList();
		for (int i = 0; i < data.freeRandomRepairs && damaged.Count > 0; i++)
		{
			GameDatas.PlayerSave.Component component = damaged.RandomElement();
			damaged.Remove(component);
			component.isDamaged = false;
			repairedComponentNames.Add(component.dataID);
		}
	}

	#endregion

	#region Effects

	public float ShopPriceFactor ( EntityEquipmentData.EntityFaction _shop )
	{
		DayEventData data = Data;
		return data != null && shops.Contains(_shop) ? 1f - data.shopDiscount : 1f;
	}

	public int ShopExtraItems ( EntityEquipmentData.EntityFaction _shop )
	{
		DayEventData data = Data;
		return data != null && shops.Contains(_shop) ? data.shopExtraItems : 0;
	}

	public float RepairPriceFactor
	{
		get
		{
			DayEventData data = Data;
			return data == null ? 1f : 1f - data.repairDiscount;
		}
	}

	public int ApplyRecyclingRule ( int _duration )
	{
		DayEventData data = Data;
		if (data == null)
			return _duration;

		return data.recyclingDurationRule switch
		{
			DayEventData.RecyclingDurationRule.SetTo => Mathf.Min(_duration, data.recyclingDurationValue),
			DayEventData.RecyclingDurationRule.ReduceBy => Mathf.Max(0, _duration - data.recyclingDurationValue),
			_ => _duration
		};
	}

	public int RecyclingYieldBonusPercent
	{
		get
		{
			DayEventData data = Data;
			return data == null ? 0 : Mathf.RoundToInt((data.recyclingYieldFactor - 1f) * 100f);
		}
	}

	public float MatchCountFactor
	{
		get
		{
			DayEventData data = Data;
			return data == null ? 1f : data.matchCountFactor;
		}
	}

	public int DailyMatchCount => Mathf.Max(1, Mathf.CeilToInt(GameConfig.current.game.dailyMatchAmount * MatchCountFactor));

	public bool TryGetForcedMatchType ( out MissionData.MissionType _type )
	{
		DayEventData data = Data;
		_type = data == null ? default : data.forcedMatchType;
		return data != null && data.forcesMatchType;
	}

	public float EnemyFactionShare
	{
		get
		{
			DayEventData data = Data;
			return data == null || faction == EntityEquipmentData.EntityFaction.Noone ? 0f : data.enemyFactionShare;
		}
	}

	public void ApplyRewardModifiers ( MissionData.RewardSet _rewardSet )
	{
		DayEventData data = Data;
		if (data == null)
			return;

		for (int i = 0; i < _rewardSet.creditAmounts.Count; i++)
			_rewardSet.creditAmounts[i] = (ulong)Mathf.RoundToInt(_rewardSet.creditAmounts[i] * data.creditFactor);

		if (!data.doublesComponents)
			return;

		foreach (EntityEquipmentData component in _rewardSet.components.OrderByDescending(MissionData.IsMainComponent).ToList())
		{
			if (_rewardSet.components.Count >= GameConfig.current.game.maxRewardComponentCount)
				break;

			MissionData.AddRandomComponent(_rewardSet, MissionData.IsMainComponent(component));
		}
	}

	public void ApplyUnitBuffs ( IEnumerable<Entity> _units )
	{
		DayEventData data = Data;
		if (data == null || faction == EntityEquipmentData.EntityFaction.Noone || (data.factionDamageBonus <= 0f && data.factionHealthBonus <= 0f))
			return;

		foreach (Entity unit in _units)
		{
			if (unit == null || unit.Data.GetDominentFaction(out _) != faction)
				continue;

			if (data.factionDamageBonus > 0f)
				unit.AddAdditionaryStatBonus(new EntityEquipmentData.StatBonusBuff()
				{
					statBonus = new EntityEquipmentData.SecondaryStat() { type = EntityEquipmentData.SecondaryStat.StatType.GeneralDamageBonus, value = data.factionDamageBonus },
					duration = int.MaxValue
				});

			if (data.factionHealthBonus > 0f)
				unit.Equipment.RaiseMaxHealth(data.factionHealthBonus);
		}
	}

	public void GrantVictoryBonus ( GameDatas.PlayerSave _save )
	{
		DayEventData data = Data;
		if (data != null && data.corpoCreditBonus > 0 && faction != EntityEquipmentData.EntityFaction.Noone)
			_save.AddCurrency(EntityEquipmentData.GetFactionCurrency(faction), (ulong)data.corpoCreditBonus);
	}

	#endregion

	#region Text

	public string GetName ()
	{
		DayEventData data = Data;
		return data == null ? "" : data.GetLocalizedName();
	}

	public List<string> GetEffectLines ()
	{
		List<string> lines = new();
		DayEventData data = Data;
		if (data == null)
			return lines;

		LocalizationManager localization = LocalizationManager.Instance;
		string shopNames = string.Join(", ", shops);
		if (shops.Count > 0 && data.shopDiscount > 0f)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_shop_discount), Percent(data.shopDiscount), shopNames));
		if (shops.Count > 0 && data.shopExtraItems > 0)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_shop_items), data.shopExtraItems, shopNames));

		if (data.repairDiscount >= 1f)
			lines.Add(localization.Get(LocalizationKey.event_effect_repair_free));
		else if (data.repairDiscount > 0f)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_repair_discount), Percent(data.repairDiscount)));

		if (data.freeRandomRepairs > 0)
			lines.Add(repairedComponentNames.Count > 0
				? string.Format(localization.Get(LocalizationKey.event_effect_repaired), string.Join(", ", repairedComponentNames.Select(DisplayNameOf)))
				: localization.Get(LocalizationKey.event_effect_repaired_none));

		if (data.recyclingDurationRule == DayEventData.RecyclingDurationRule.SetTo)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_recycling_set), data.recyclingDurationValue));
		else if (data.recyclingDurationRule == DayEventData.RecyclingDurationRule.ReduceBy)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_recycling_reduce), data.recyclingDurationValue));
		if (data.recyclingYieldFactor > 1f)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_recycling_yield), Factor(data.recyclingYieldFactor)));

		if (TryGetForcedMatchType(out MissionData.MissionType forcedType))
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_match_type), forcedType.GetLocalizedTitle()));
		if (data.matchCountFactor < 1f)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_match_count), DailyMatchCount));
		if (EnemyFactionShare > 0f)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_enemy_faction), Percent(EnemyFactionShare), faction));

		if (data.creditFactor > 1f)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_credits), Factor(data.creditFactor)));
		if (data.doublesComponents)
			lines.Add(localization.Get(LocalizationKey.event_effect_components));
		if (data.corpoCreditBonus > 0 && faction != EntityEquipmentData.EntityFaction.Noone)
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_corpo), data.corpoCreditBonus, faction));
		if (faction != EntityEquipmentData.EntityFaction.Noone && (data.factionDamageBonus > 0f || data.factionHealthBonus > 0f))
			lines.Add(string.Format(localization.Get(LocalizationKey.event_effect_unit_buff), faction, Percent(data.factionDamageBonus), Percent(data.factionHealthBonus)));

		return lines;
	}

	private static string DisplayNameOf ( string _componentName )
	{
		return GameAssets.current.equipments.TryGetValue(_componentName, out EntityEquipmentData component) ? component.GetLocalizedName() : _componentName;
	}

	private static int Percent ( float _ratio )
	{
		return Mathf.RoundToInt(_ratio * 100f);
	}

	private static string Factor ( float _factor )
	{
		CultureInfo culture = LocalizationManager.Instance.CurrentLanguage == SystemLanguage.French ? CultureInfo.GetCultureInfo("fr-FR") : CultureInfo.InvariantCulture;
		return _factor.ToString("0.##", culture);
	}

	public static string FormatPrice ( ulong _price, ulong _basePrice )
	{
		return _price < _basePrice ? "<alpha=#99><s>" + _basePrice + "</s><alpha=#FF> " + _price : _price.ToString();
	}

	#endregion
}
