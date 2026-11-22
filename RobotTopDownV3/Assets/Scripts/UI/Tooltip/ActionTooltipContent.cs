using System.Collections.Generic;
using UnityEngine;

public sealed class ActionTooltipContent
{
	private readonly struct OwnedEffect
	{
		public readonly AEntityPassiveEffect.PassiveEffectContainer container;
		public readonly AEntityPassiveEffect asset;
		public readonly bool isUnit;

		public OwnedEffect ( AEntityPassiveEffect.PassiveEffectContainer _container, AEntityPassiveEffect _asset, bool _isUnit )
		{
			container = _container;
			asset = _asset;
			isUnit = _isUnit;
		}

		public bool IsUnconditional => container.conditionType == Condition.ConditionType.Noone;
	}

	public readonly EntityActionData data;
	public readonly EntitySavedData unit;
	public readonly int preparation;
	public readonly int cooldown;
	public readonly int maxRange;
	public readonly int targets;
	public readonly int hits;
	public readonly int steadyRangeBonus;
	public readonly List<ActionKeyword> keywords = new();
	public readonly bool hasExchange;
	public readonly ActionExchange exchange;

	private readonly List<OwnedEffect> m_effects = new();
	private readonly EntityActionData.MainActionType m_mainType;

	public ActionTooltipContent ( EntityActionData _data, EntitySavedData _unit )
	{
		data = _data;
		unit = _unit;
		m_mainType = _data.GetMainActionType();
		CollectEffects();

		preparation = Mathf.Max(0, _data.GetTokenPreparationCost(null, null, null)
			- Amount<PreparationCostReductionPassiveEffect>(EntityPassiveEffectEnumID.PreparationCostReduction, _effect => _effect.reductionAmount)
			- Amount<QuickDrawPassiveEffect>(EntityPassiveEffectEnumID.QuickDraw, _effect => _effect.reductionAmount));
		cooldown = Mathf.Max(0, _data.GetTokenCooldownCost(null, null, null)
			- Amount<CooldownCostReductionPassiveEffect>(EntityPassiveEffectEnumID.CooldownCostReduction, _effect => _effect.reductionAmount)
			- Amount<QuickDrawPassiveEffect>(EntityPassiveEffectEnumID.QuickDraw, _effect => _effect.reductionAmount));
		maxRange = _data.maxDistance + Amount<MaxRangeUpPassiveEffect>(EntityPassiveEffectEnumID.MaxRangeUp, _effect => _effect.rangeBoostAmount);
		targets = _data.maxTargetAmount + Amount<MaxTargetUpPassiveEffect>(EntityPassiveEffectEnumID.MaxTargetUp, _effect => _effect.targetBoostAmount);
		hits = Mathf.Max(1, _data.hitAmount + Amount<HitAmountBoostPassiveEffect>(EntityPassiveEffectEnumID.HitAmountBoost, _effect => _effect.hitAmountBoost));

		CollectKeywords(out steadyRangeBonus);
		hasExchange = ActionExchange.TryGet(_data, out exchange);
	}

	public int Duration => data.tokenDuration;

	public string Name => data.GetLocalizedName();

	public string TypeLabel => data.IsRotation ? LocalizationManager.Instance.Get(LocalizationKey.action_type_rotation) : data.type.GetLocalizedTitle();

	public string TargetLabel
	{
		get
		{
			LocalizationManager localization = LocalizationManager.Instance;
			if (data.targetType == EntityActionData.TargetType.Self)
				return localization.Get(LocalizationKey.tooltip_target_self);
			if (data.DoesResolveItsOwnTarget())
				return localization.Get(LocalizationKey.tooltip_target_auto);
			if (data.targetType == EntityActionData.TargetType.OtherEntity)
				return localization.Get(LocalizationKey.tooltip_target_enemy);
			if (m_mainType == EntityActionData.MainActionType.Movement)
				return localization.Get(LocalizationKey.tooltip_target_destination);
			return targets > 1 ? string.Format(localization.Get(LocalizationKey.tooltip_target_tiles), targets) : localization.Get(LocalizationKey.tooltip_target_tile);
		}
	}

	public string CostValue => data.isModAction ? "0" : IsWalk ? "1" : (preparation + Duration + cooldown).ToString();

	public string CostUnit
	{
		get
		{
			LocalizationManager localization = LocalizationManager.Instance;
			if (IsWalk)
				return localization.Get(LocalizationKey.tooltip_cost_per_tile);
			return localization.Get(!data.isModAction && preparation + Duration + cooldown > 1 ? LocalizationKey.tooltip_cost_ticks : LocalizationKey.tooltip_cost_tick);
		}
	}

	public bool HasTempo => !data.isModAction;

	public bool HasSpeed => IsWalk;

	public string SpeedValue => string.Format(LocalizationManager.Instance.Get(LocalizationKey.tooltip_speed_value), data.movementSpeed);

	public bool HasRange => !HasSpeed && !data.isModAction && data.targetType != EntityActionData.TargetType.Self && maxRange > 0;

	public string RangeValue => data.minDistance == maxRange ? maxRange.ToString() : data.minDistance + "–" + maxRange;

	public bool HasDamage => data.baseDamages != null && data.baseDamages.Count > 0;

	public string Hint
	{
		get
		{
			LocalizationManager localization = LocalizationManager.Instance;
			if (data.isModAction)
				return "";
			if (data.DoesResolveItsOwnTarget())
				return localization.Get(LocalizationKey.action_desc_designation_hint);
			if (data.targetType == EntityActionData.TargetType.Self || data.codeType == EntityActionData.ActionCodeType.Wait)
				return localization.Get(LocalizationKey.tooltip_hint_queue);
			if (m_mainType == EntityActionData.MainActionType.Movement)
				return localization.Get(LocalizationKey.tooltip_hint_destination);
			return targets > 1 ? string.Format(localization.Get(LocalizationKey.tooltip_hint_tiles), targets) : localization.Get(LocalizationKey.tooltip_hint_tile);
		}
	}

	private bool IsWalk => data.codeType == EntityActionData.ActionCodeType.TargetTileMove && !data.isModAction;

	public string GetDamageText ( System.Func<WeaponEquipmentData.DamageType, Color?> _colorOf )
	{
		List<string> parts = new();
		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in data.baseDamages)
		{
			Color? color = _colorOf?.Invoke(pair.Key);
			string typeName = pair.Key.GetLocalizedTitle();
			parts.Add(pair.Value + " " + (color.HasValue ? "<color=#" + ColorUtility.ToHtmlStringRGB(color.Value) + ">" + typeName + "</color>" : typeName));
		}
		return string.Join(" + ", parts);
	}

	private void CollectEffects ()
	{
		List<AEntityPassiveEffect.PassiveEffectContainer> actionEffects = new();
		if (data.passiveEffects != null)
			actionEffects.AddRange(data.passiveEffects);

		foreach (AEntityPassiveEffect.PassiveEffectContainer container in actionEffects)
			AddEffect(container, false);

		if (unit == null)
			return;

		List<AEntityPassiveEffect.PassiveEffectContainer> unitEffects = unit.GetPassiveEffects(data.enumID);
		foreach (AEntityPassiveEffect.PassiveEffectContainer container in actionEffects)
		{
			int index = unitEffects.FindIndex(_other => _other.enumID == container.enumID && _other.conditionType == container.conditionType && _other.targetType == container.targetType);
			if (index >= 0)
				unitEffects.RemoveAt(index);
		}

		foreach (AEntityPassiveEffect.PassiveEffectContainer container in unitEffects)
			AddEffect(container, true);
	}

	private void AddEffect ( AEntityPassiveEffect.PassiveEffectContainer _container, bool _isUnit )
	{
		if (GameAssets.current.game.entityEffects.TryGetValue(_container.enumID, out AEntityPassiveEffect asset) && asset != null)
			m_effects.Add(new OwnedEffect(_container, asset, _isUnit));
	}

	private int Amount<T> ( EntityPassiveEffectEnumID _enumID, System.Func<T, int> _amountOf ) where T : AEntityPassiveEffect
	{
		foreach (OwnedEffect effect in m_effects)
			if (effect.container.enumID == _enumID)
				return effect.IsUnconditional && effect.asset is T typed ? _amountOf(typed) : 0;

		return 0;
	}

	private bool TryGetUnconditional ( EntityPassiveEffectEnumID _enumID, out OwnedEffect _effect )
	{
		foreach (OwnedEffect effect in m_effects)
		{
			if (effect.container.enumID != _enumID)
				continue;

			_effect = effect;
			return effect.IsUnconditional;
		}

		_effect = default;
		return false;
	}

	private void CollectKeywords ( out int _steadyRangeBonus )
	{
		if (data.DoesResolveItsOwnTarget())
			Add(ActionKeywordType.AutoTarget);
		if (m_mainType != EntityActionData.MainActionType.Movement && targets > 1)
			Add(ActionKeywordType.MultiTarget, targets);
		if (m_mainType == EntityActionData.MainActionType.Attack && hits > 1)
			Add(ActionKeywordType.MultiHit, hits);

		switch (data.codeType)
		{
			case EntityActionData.ActionCodeType.MoveThenAttack: Add(ActionKeywordType.Rush); break;
			case EntityActionData.ActionCodeType.JumpMove: Add(ActionKeywordType.Leap); break;
			case EntityActionData.ActionCodeType.TurnShield: Add(ActionKeywordType.Shield); break;
			case EntityActionData.ActionCodeType.InvokeItem:
			case EntityActionData.ActionCodeType.InvokeEntity:
				if (data.invocatedItem != null)
					keywords.Add(new ActionKeyword { type = ActionKeywordType.Deploy, item = data.invocatedItem, value = data.invocationCountLimit });
				break;
			case EntityActionData.ActionCodeType.MoveItem:
				if (data.invocatedItem != null)
					keywords.Add(new ActionKeyword { type = ActionKeywordType.Relocate, item = data.invocatedItem });
				break;
		}
		if (data.isModAction)
			Add(ActionKeywordType.Free);

		CollectZoneKeywords();

		List<AEntityStatus> appliedStatuses = new();
		foreach (OwnedEffect effect in m_effects)
		{
			if (effect.isUnit || !effect.IsUnconditional)
				continue;

			switch (effect.asset)
			{
				case ApplyStatusPassiveEffect applyStatus when applyStatus.Status != null && !appliedStatuses.Contains(applyStatus.Status):
					appliedStatuses.Add(applyStatus.Status);
					bool isSelf = effect.container.targetType == EntityActionData.TargetType.Self;
					keywords.Add(new ActionKeyword
					{
						type = ActionKeywordType.Status,
						status = applyStatus.Status,
						duration = applyStatus.duration,
						isSelf = isSelf,
						percent = isSelf ? 0 : StatusChance.ToPercent()
					});
					break;
				case RemoveStatusPassiveEffect removeStatus when GameAssets.current.game.entityStatus.TryGetValue(removeStatus.statusRemoved, out AEntityStatus removed) && removed != null:
					keywords.Add(new ActionKeyword { type = ActionKeywordType.Status, status = removed, isRemoval = true });
					break;
				case PushOrPullPassiveEffect pushOrPull when pushOrPull.movementStrength != 0:
					Add(pushOrPull.movementStrength > 0 ? ActionKeywordType.Push : ActionKeywordType.Pull, Mathf.Abs(pushOrPull.movementStrength));
					break;
				case InstantDeathPassiveEffect when effect.container.targetType == EntityActionData.TargetType.Self:
					Add(ActionKeywordType.SelfDestruct);
					break;
			}
		}

		List<AEntityPassiveEffect> steadyBonuses = CollectBonuses(Condition.ConditionType.DidNotMoveThisTurn, appliedStatuses);
		_steadyRangeBonus = steadyBonuses.Find(_bonus => _bonus is MaxRangeUpPassiveEffect) is MaxRangeUpPassiveEffect steadyRange ? steadyRange.rangeBoostAmount : 0;
		if (steadyBonuses.Count > 0)
			keywords.Add(new ActionKeyword { type = ActionKeywordType.Steady, bonuses = steadyBonuses });

		List<AEntityPassiveEffect> huntBonuses = CollectBonuses(Condition.ConditionType.IsTargetMarked, null);
		if (huntBonuses.Count > 0)
			keywords.Add(new ActionKeyword { type = ActionKeywordType.Hunt, bonuses = huntBonuses });

		if (data.conditionType == Condition.ConditionType.DidNotUseThisGame)
			Add(ActionKeywordType.Unique);
		if (data.conditionType == Condition.ConditionType.DidNotAttackThisTurn)
			Add(ActionKeywordType.NoAttack);

		foreach (OwnedEffect effect in m_effects)
		{
			if (effect.container.enumID == EntityPassiveEffectEnumID.TrajectoryControl && effect.IsUnconditional && data.FiresProjectileAtTarget())
			{
				keywords.Add(new ActionKeyword { type = ActionKeywordType.CurvedShot, isUnit = effect.isUnit });
				break;
			}
		}

		if (TryGetUnconditional(EntityPassiveEffectEnumID.QuickDraw, out OwnedEffect quickDraw) && quickDraw.asset is QuickDrawPassiveEffect quickDrawAsset
			&& (data.GetTokenPreparationCost(null, null, null) > 0 || data.GetTokenCooldownCost(null, null, null) > 0))
			keywords.Add(new ActionKeyword { type = ActionKeywordType.QuickDraw, value = quickDrawAsset.reductionAmount, isUnit = quickDraw.isUnit });
	}

	private void CollectZoneKeywords ()
	{
		int areaMax = data.aoeMaxEffectRange + Amount<MaxAoERangeUpPassiveEffect>(EntityPassiveEffectEnumID.MaxAoERangeUp, _effect => _effect.rangeBoostAmount);
		bool isCenteredOnSelf = data.aoECenterType == EntityActionData.AOECenterType.Self;

		switch (data.aoeType)
		{
			case EntityActionData.AOEType.Circle:
				if (data.targetType == EntityActionData.TargetType.Self || isCenteredOnSelf)
					keywords.Add(new ActionKeyword { type = ActionKeywordType.Pulse, minValue = data.aoeMinEffectRange, value = areaMax });
				else if (areaMax >= 1)
					keywords.Add(new ActionKeyword { type = ActionKeywordType.Blast, value = areaMax });
				break;
			case EntityActionData.AOEType.Ray:
				if (isCenteredOnSelf)
					keywords.Add(new ActionKeyword { type = ActionKeywordType.Ray, value = maxRange });
				break;
			case EntityActionData.AOEType.LargeCone:
			case EntityActionData.AOEType.ThinCone:
				keywords.Add(new ActionKeyword
				{
					type = ActionKeywordType.Cone,
					isWide = data.aoeType == EntityActionData.AOEType.LargeCone,
					minValue = isCenteredOnSelf ? data.minDistance : data.aoeMinEffectRange,
					value = isCenteredOnSelf ? maxRange : areaMax
				});
				break;
			case EntityActionData.AOEType.LargeArc:
			case EntityActionData.AOEType.ThinArc:
				keywords.Add(new ActionKeyword { type = ActionKeywordType.Arc, isWide = data.aoeType == EntityActionData.AOEType.LargeArc, value = areaMax });
				break;
			case EntityActionData.AOEType.Chain:
				keywords.Add(new ActionKeyword
				{
					type = ActionKeywordType.Chain,
					value = data.maxChainedTarget,
					radius = areaMax,
					percent = data.damageReductionOnChain.ToPercent()
				});
				break;
		}

		if (data.aoeType == EntityActionData.AOEType.Noone || !data.doesAffectTile)
			return;

		foreach (OwnedEffect effect in m_effects)
		{
			if (!effect.isUnit && effect.IsUnconditional && effect.asset is ApplyStatusPassiveEffect applyStatus && applyStatus.Status != null)
			{
				keywords.Add(new ActionKeyword { type = ActionKeywordType.Lingering, status = applyStatus.Status, duration = applyStatus.duration });
				return;
			}
		}
	}

	private List<AEntityPassiveEffect> CollectBonuses ( Condition.ConditionType _condition, List<AEntityStatus> _alreadyApplied )
	{
		List<AEntityPassiveEffect> bonuses = new();
		foreach (OwnedEffect effect in m_effects)
		{
			if (effect.container.conditionType != _condition || bonuses.Contains(effect.asset))
				continue;
			if (effect.isUnit && effect.asset is ApplyStatusPassiveEffect)
				continue;
			if (_alreadyApplied != null && effect.asset is ApplyStatusPassiveEffect applyStatus && _alreadyApplied.Contains(applyStatus.Status))
				continue;

			bonuses.Add(effect.asset);
		}
		return bonuses;
	}

	private float StatusChance
	{
		get
		{
			float chance = data.statusHitProbability;
			if (unit == null)
				return Mathf.Clamp01(chance);

			foreach (GameDatas.PlayerSave.Component component in unit.GetAllEquipments())
			{
				if (component.TryGetData(out EntityEquipmentData equipment) && System.Array.IndexOf(equipment.knownedActions, data.enumID) >= 0)
				{
					chance += equipment.statusHitProbability;
					break;
				}
			}

			chance += unit.GetStatBonusFromAll(EntityEquipmentData.SecondaryStat.StatType.StatusChance);
			return Mathf.Clamp01(chance);
		}
	}

	private void Add ( ActionKeywordType _type, int _value = 0 )
	{
		keywords.Add(new ActionKeyword { type = _type, value = _value });
	}
}
