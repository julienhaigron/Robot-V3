using System.Collections.Generic;
using UnityEngine;

public enum ActionKeywordType
{
	AutoTarget,
	MultiTarget,
	MultiHit,
	Rush,
	Leap,
	Shield,
	Deploy,
	Relocate,
	Free,
	Pulse,
	Blast,
	Ray,
	Cone,
	Arc,
	Chain,
	Lingering,
	Status,
	Push,
	Pull,
	SelfDestruct,
	Steady,
	Hunt,
	Unique,
	NoAttack,
	CurvedShot,
	QuickDraw,
	SpecialAbility
}

public enum ActionKeywordFamily
{
	Target,
	Zone,
	Effect,
	Condition,
	Limit,
	Mechanic,
	Unit
}

public struct ActionKeyword
{
	public ActionKeywordType type;
	public int value;
	public int minValue;
	public int radius;
	public int percent;
	public bool isWide;
	public bool isSelf;
	public bool isRemoval;
	public bool isUnit;
	public int duration;
	public AEntityStatus status;
	public AItemData item;
	public EntityActionData ability;
	public List<AEntityPassiveEffect> bonuses;

	public ActionKeywordFamily Family => type switch
	{
		ActionKeywordType.AutoTarget or ActionKeywordType.MultiTarget or ActionKeywordType.MultiHit => ActionKeywordFamily.Target,
		ActionKeywordType.Pulse or ActionKeywordType.Blast or ActionKeywordType.Ray or ActionKeywordType.Cone or ActionKeywordType.Arc
			or ActionKeywordType.Chain or ActionKeywordType.Lingering => ActionKeywordFamily.Zone,
		ActionKeywordType.Status or ActionKeywordType.Push or ActionKeywordType.Pull or ActionKeywordType.SelfDestruct
			or ActionKeywordType.SpecialAbility => ActionKeywordFamily.Effect,
		ActionKeywordType.Steady or ActionKeywordType.Hunt => ActionKeywordFamily.Condition,
		ActionKeywordType.Unique or ActionKeywordType.NoAttack => ActionKeywordFamily.Limit,
		_ => isUnit ? ActionKeywordFamily.Unit : ActionKeywordFamily.Mechanic
	};

	public bool HasZone => type is ActionKeywordType.Pulse or ActionKeywordType.Blast or ActionKeywordType.Ray
		or ActionKeywordType.Cone or ActionKeywordType.Arc or ActionKeywordType.Chain;

	public string Label
	{
		get
		{
			LocalizationManager localization = LocalizationManager.Instance;
			switch (type)
			{
				case ActionKeywordType.MultiTarget: return string.Format(localization.Get(LocalizationKey.keyword_multi_target), value);
				case ActionKeywordType.MultiHit: return string.Format(localization.Get(LocalizationKey.keyword_multi_hit), value);
				case ActionKeywordType.Deploy: return string.Format(localization.Get(LocalizationKey.keyword_deploy), item.GetLocalizedName());
				case ActionKeywordType.Relocate: return string.Format(localization.Get(LocalizationKey.keyword_relocate), item.GetLocalizedName());
				case ActionKeywordType.Pulse: return string.Format(localization.Get(LocalizationKey.keyword_pulse), minValue, value);
				case ActionKeywordType.Blast: return EntityActionData.AOEType.Circle.GetLocalizedTitle() + " " + value;
				case ActionKeywordType.Ray: return EntityActionData.AOEType.Ray.GetLocalizedTitle() + " " + value;
				case ActionKeywordType.Cone: return ConeType.GetLocalizedTitle() + " " + (minValue > 1 && minValue != value ? minValue + "–" + value : value.ToString());
				case ActionKeywordType.Arc: return (isWide ? EntityActionData.AOEType.LargeArc : EntityActionData.AOEType.ThinArc).GetLocalizedTitle() + " " + value;
				case ActionKeywordType.Chain: return EntityActionData.AOEType.Chain.GetLocalizedTitle() + " ×" + value;
				case ActionKeywordType.Push: return string.Format(localization.Get(LocalizationKey.keyword_push), value);
				case ActionKeywordType.Pull: return string.Format(localization.Get(LocalizationKey.keyword_pull), value);
				case ActionKeywordType.Steady: return string.Format(localization.Get(LocalizationKey.keyword_steady_chip), BonusText);
				case ActionKeywordType.Hunt: return string.Format(localization.Get(LocalizationKey.keyword_hunt_chip), BonusText);
				case ActionKeywordType.Status:
					if (isRemoval)
						return string.Format(localization.Get(LocalizationKey.keyword_status_remove), status.GetLocalizedName());
					if (isSelf)
						return string.Format(localization.Get(LocalizationKey.keyword_status_gain), status.GetLocalizedName()) + " · " + DurationText;
					return status.GetLocalizedName() + " " + string.Format(localization.Get(LocalizationKey.tooltip_percent), percent) + " · " + DurationText;
				default: return Title;
			}
		}
	}

	public string Title
	{
		get
		{
			LocalizationManager localization = LocalizationManager.Instance;
			switch (type)
			{
				case ActionKeywordType.AutoTarget: return localization.Get(LocalizationKey.keyword_auto_target);
				case ActionKeywordType.Rush: return localization.Get(LocalizationKey.keyword_rush);
				case ActionKeywordType.Leap: return localization.Get(LocalizationKey.keyword_leap);
				case ActionKeywordType.Shield: return localization.Get(LocalizationKey.keyword_shield);
				case ActionKeywordType.Free: return localization.Get(LocalizationKey.keyword_free);
				case ActionKeywordType.Lingering: return localization.Get(LocalizationKey.keyword_lingering);
				case ActionKeywordType.SelfDestruct: return localization.Get(LocalizationKey.keyword_self_destruct);
				case ActionKeywordType.Steady: return localization.Get(LocalizationKey.keyword_steady);
				case ActionKeywordType.Hunt: return localization.Get(LocalizationKey.keyword_hunt);
				case ActionKeywordType.Unique: return localization.Get(LocalizationKey.keyword_unique);
				case ActionKeywordType.NoAttack: return localization.Get(LocalizationKey.keyword_no_attack);
				case ActionKeywordType.CurvedShot: return localization.Get(LocalizationKey.keyword_curved_shot);
				case ActionKeywordType.QuickDraw: return localization.Get(LocalizationKey.keyword_quick_draw);
				case ActionKeywordType.Status: return status.GetLocalizedName();
				case ActionKeywordType.SpecialAbility: return ability.GetLocalizedName();
				default: return Label;
			}
		}
	}

	public string Text
	{
		get
		{
			LocalizationManager localization = LocalizationManager.Instance;
			string plural = value > 1 ? "s" : "";
			switch (type)
			{
				case ActionKeywordType.AutoTarget: return localization.Get(LocalizationKey.keyword_auto_target_desc);
				case ActionKeywordType.MultiTarget: return string.Format(localization.Get(LocalizationKey.keyword_multi_target_desc), value);
				case ActionKeywordType.MultiHit: return string.Format(localization.Get(LocalizationKey.keyword_multi_hit_desc), value);
				case ActionKeywordType.Rush: return localization.Get(LocalizationKey.keyword_rush_desc);
				case ActionKeywordType.Leap: return localization.Get(LocalizationKey.keyword_leap_desc);
				case ActionKeywordType.Shield: return localization.Get(LocalizationKey.keyword_shield_desc);
				case ActionKeywordType.Deploy:
					return string.Format(localization.Get(LocalizationKey.keyword_deploy_desc), item.GetLocalizedName())
						+ (value > 0 ? string.Format(localization.Get(LocalizationKey.keyword_deploy_limit), value) : "");
				case ActionKeywordType.Relocate: return string.Format(localization.Get(LocalizationKey.keyword_relocate_desc), item.GetLocalizedName());
				case ActionKeywordType.Free: return localization.Get(LocalizationKey.keyword_free_desc);
				case ActionKeywordType.Pulse: return string.Format(localization.Get(LocalizationKey.keyword_pulse_desc), minValue, value);
				case ActionKeywordType.Blast: return string.Format(localization.Get(LocalizationKey.keyword_blast_desc), value, plural);
				case ActionKeywordType.Ray: return localization.Get(LocalizationKey.keyword_ray_desc);
				case ActionKeywordType.Cone:
					return string.Format(localization.Get(LocalizationKey.keyword_cone_desc)
						, localization.Get(isWide ? LocalizationKey.keyword_cone_wide : LocalizationKey.keyword_cone_narrow)
						, minValue == value
							? string.Format(localization.Get(LocalizationKey.keyword_distance_exact), value)
							: string.Format(localization.Get(LocalizationKey.keyword_distance_range), Mathf.Max(1, minValue), value));
				case ActionKeywordType.Arc: return localization.Get(LocalizationKey.keyword_arc_desc);
				case ActionKeywordType.Chain: return string.Format(localization.Get(LocalizationKey.keyword_chain_desc), value, radius, percent);
				case ActionKeywordType.Lingering: return string.Format(localization.Get(LocalizationKey.keyword_lingering_desc), status.GetLocalizedName(), DurationText);
				case ActionKeywordType.Status: return status.GetLocalizedDescription();
				case ActionKeywordType.Push: return string.Format(localization.Get(LocalizationKey.keyword_push_desc), value, plural, GameConfig.current.game.pushCollisionDamage);
				case ActionKeywordType.Pull: return string.Format(localization.Get(LocalizationKey.keyword_pull_desc), value, plural);
				case ActionKeywordType.SelfDestruct: return localization.Get(LocalizationKey.keyword_self_destruct_desc);
				case ActionKeywordType.Steady: return string.Format(localization.Get(LocalizationKey.keyword_steady_desc), BonusText);
				case ActionKeywordType.Hunt: return string.Format(localization.Get(LocalizationKey.keyword_hunt_desc), BonusText);
				case ActionKeywordType.Unique: return localization.Get(LocalizationKey.keyword_unique_desc);
				case ActionKeywordType.NoAttack: return localization.Get(LocalizationKey.keyword_no_attack_desc);
				case ActionKeywordType.CurvedShot: return localization.Get(LocalizationKey.keyword_curved_shot_desc);
				case ActionKeywordType.QuickDraw: return string.Format(localization.Get(LocalizationKey.keyword_quick_draw_desc), value);
				case ActionKeywordType.SpecialAbility: return ability.GetLocalizedDescription();
				default: return "";
			}
		}
	}

	public string Note => type switch
	{
		ActionKeywordType.Status when !isSelf && !isRemoval => LocalizationManager.Instance.Get(LocalizationKey.keyword_status_chance_note),
		ActionKeywordType.SpecialAbility => LocalizationManager.Instance.Get(ability.abilityEffect == EntityActionData.SpecialAbilityEffect.Retreat
			? LocalizationKey.keyword_special_ability_note_retreat : LocalizationKey.keyword_special_ability_note),
		_ => ""
	};

	public string Tag => isUnit ? LocalizationManager.Instance.Get(LocalizationKey.tooltip_unit_tag)
		: type == ActionKeywordType.SpecialAbility ? LocalizationManager.Instance.Get(LocalizationKey.keyword_special_ability) : "";

	private EntityActionData.AOEType ConeType => isWide ? EntityActionData.AOEType.LargeCone : EntityActionData.AOEType.ThinCone;

	private string DurationText
	{
		get
		{
			bool isRound = status != null && status.durationType == AEntityStatus.DurationType.Round;
			LocalizationKey key = isRound
				? duration > 1 ? LocalizationKey.tooltip_duration_rounds : LocalizationKey.tooltip_duration_round
				: duration > 1 ? LocalizationKey.tooltip_duration_ticks : LocalizationKey.tooltip_duration_tick;
			return string.Format(LocalizationManager.Instance.Get(key), duration);
		}
	}

	private string BonusText
	{
		get
		{
			if (bonuses == null || bonuses.Count == 0)
				return LocalizationManager.Instance.Get(LocalizationKey.bonus_fallback);

			List<string> parts = new(bonuses.Count);
			foreach (AEntityPassiveEffect bonus in bonuses)
				parts.Add(GetBonusText(bonus));
			return string.Join(", ", parts);
		}
	}

	public static string GetBonusText ( AEntityPassiveEffect _effect )
	{
		LocalizationManager localization = LocalizationManager.Instance;
		switch (_effect)
		{
			case MaxRangeUpPassiveEffect rangeUp: return string.Format(localization.Get(LocalizationKey.bonus_range), rangeUp.rangeBoostAmount);
			case MaxAoERangeUpPassiveEffect areaUp: return string.Format(localization.Get(LocalizationKey.bonus_area), areaUp.rangeBoostAmount);
			case MaxTargetUpPassiveEffect targetUp: return string.Format(localization.Get(LocalizationKey.bonus_targets), targetUp.targetBoostAmount);
			case HitAmountBoostPassiveEffect hitBoost: return string.Format(localization.Get(LocalizationKey.bonus_hits), hitBoost.hitAmountBoost);
			case DamageUpPassiveEffect damageUp:
				return damageUp.damageBoostAmount >= 1f && Mathf.Approximately(damageUp.damageBoostAmount, Mathf.Round(damageUp.damageBoostAmount))
					? string.Format(localization.Get(LocalizationKey.bonus_damage_factor), 1 + Mathf.RoundToInt(damageUp.damageBoostAmount))
					: string.Format(localization.Get(LocalizationKey.bonus_damage_percent), damageUp.damageBoostAmount.ToPercent());
			case CooldownCostReductionPassiveEffect cooldown: return string.Format(localization.Get(LocalizationKey.bonus_cooldown), cooldown.reductionAmount);
			case PreparationCostReductionPassiveEffect preparation: return string.Format(localization.Get(LocalizationKey.bonus_preparation), preparation.reductionAmount);
			case QuickDrawPassiveEffect quickDraw: return string.Format(localization.Get(LocalizationKey.bonus_quick_draw), quickDraw.reductionAmount);
			case ApplyStatusPassiveEffect applyStatus: return string.Format(localization.Get(LocalizationKey.bonus_applies), applyStatus.Status.GetLocalizedName());
			default: return _effect.GetLocalizedName();
		}
	}
}
