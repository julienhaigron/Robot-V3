using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;


[CreateAssetMenu(fileName = "EntityActionData", menuName = "ScriptableObject/ActionData")]
public class EntityActionData : ScriptableObject
{
	public string displayName;
	[ReadOnly]
	public EntityActionEnumID enumID = EntityActionEnumID.Unknowned;

	public Sprite icon;
	public Color tileOutlineColor = Color.green;
	[SerializeField] private int m_tokenPreparationDuration;
	[SerializeField] private int m_tokenCooldown;
	public int tokenDuration = 1;

	[Title("Animation")]
	public string preparationAnimationKey;
	public string afterPerformAnimationKey;

	[Title("Condition")]

	public enum ConditionType { Noone, DidNotMoveThisTurn, DidNotAttackThisTurn/*, IsTargetMarked*/ }
	public ConditionType conditionType = ConditionType.Noone;

	[Title("Stats")]
	public float previousActionAttackModificator = 0;

	public enum ActionType
	{
		DistanceAttack,
		MeleeAttack,
		Movement,
		Rotation,
		Special
	}
	public ActionType type;

	public enum ActionSubType
	{
		Fuite,
		Reflexe,
		Esquive,
		Barrage,
		Engagement,
		Poursuite,
		TBD1,
		TBD2
	}
	public ActionSubType subType;

	#region Target Vars
	[Title("Target")] //TODO: see this later https://odininspector.com/attributes/hide-if-group-attribute
	public enum TargetType
	{
		Self,
		OtherEntity,
		Mortar,
		Tile
	}
	public TargetType targetType = TargetType.OtherEntity;
	[ShowIf("@targetType != TargetType.Self")]
	public int minDistance;
	[ShowIf("@targetType != TargetType.Self")]
	public int maxDistance;

	public int minTargetAmount = 1;
	public int maxTargetAmount = 1;
	#endregion

	#region AOE Vars
	[ShowIf("@targetType != TargetType.Self")]
	public bool isAoe = false;
	public enum AOEType
	{
		Circle,
		Ray,
		Cone,
		Arc
	}
	[ShowIf("@isAoe")]
	public AOEType aoeType = AOEType.Circle;
	[ShowIf("@isAoe && aoeType == AOEType.Circle"), Min(1)]
	public int circleRange = 1;
	[ShowIf("@isAoe && aoeType == AOEType.Arc"), Min(1)]
	public int arcRadius = 1;
	[ShowIf("@isAoe && aoeType == AOEType.Ray"), Min(1)]
	public int rayDiameter = 1;
	
	public enum ConeType
	{
		Thin,
		Large
	}
	[ShowIf("@isAoe && aoeType == AOEType.Cone")]
	public ConeType coneType = ConeType.Thin;
	#endregion

	[Title("Damage")]
	public int hitAmount = 1;
	public float damageFactor = 1f;
	public WeaponEquipmentData.DamageType[] usedDamageChannels;

	[Title("Effect")]
	//public AEntityStatus[] appliableStatus;
	public AEntityPassiveEffect.PassiveEffectContainer[] passiveEffects;

	[Title("Misc")]
	[ShowIf("@type == ActionType.Special")] public UnitPreset invocatedEntity;

	public enum PFCResultType
	{
		FirstWins,
		SecondWins,
		Equal,
		Failure
	}

	public static PFCResultType PFC ( EntityActionData _firstAction, EntityActionData _secondAction )
	{
		switch (_firstAction.type)
		{
			case ActionType.MeleeAttack:
				if (_firstAction.subType == ActionSubType.Poursuite)
				{
					if (_secondAction.type == ActionType.Movement)
						return PFCResultType.FirstWins;
					else if (_secondAction.type == ActionType.DistanceAttack)
						return PFCResultType.SecondWins;
					else if (_secondAction.type == ActionType.MeleeAttack)
						return PFCResultType.Equal;
				}
				else if (_firstAction.subType == ActionSubType.Engagement)
				{
					if (_secondAction.type == ActionType.DistanceAttack)
						return PFCResultType.FirstWins;
					else if (_secondAction.type == ActionType.Movement)
						return PFCResultType.SecondWins;
					else if (_secondAction.type == ActionType.MeleeAttack)
						return PFCResultType.Equal;
				}
				break;
			case ActionType.DistanceAttack:
				if (_firstAction.subType == ActionSubType.Barrage)
				{
					if (_secondAction.type == ActionType.Movement)
						return PFCResultType.FirstWins;
					else if (_secondAction.type == ActionType.MeleeAttack)
						return PFCResultType.SecondWins;
					else if (_secondAction.type == ActionType.DistanceAttack)
						return PFCResultType.Equal;
				}
				else if (_firstAction.subType == ActionSubType.Reflexe)
				{
					if (_secondAction.type == ActionType.MeleeAttack)
						return PFCResultType.FirstWins;
					else if (_secondAction.type == ActionType.Movement)
						return PFCResultType.SecondWins;
					else if (_secondAction.type == ActionType.DistanceAttack)
						return PFCResultType.Equal;
				}
				break;
			case ActionType.Movement:
				if (_firstAction.subType == ActionSubType.Esquive)
				{
					if (_secondAction.type == ActionType.MeleeAttack)
						return PFCResultType.FirstWins;
					else if (_secondAction.type == ActionType.DistanceAttack)
						return PFCResultType.SecondWins;
					else if (_secondAction.type == ActionType.Movement)
						return PFCResultType.Equal;
				}
				else if (_firstAction.subType == ActionSubType.Fuite)
				{
					if (_secondAction.type == ActionType.DistanceAttack)
						return PFCResultType.FirstWins;
					else if (_secondAction.type == ActionType.MeleeAttack)
						return PFCResultType.SecondWins;
					else if (_secondAction.type == ActionType.Movement)
						return PFCResultType.Equal;
				}
				break;
			case ActionType.Special:
				if (_firstAction.subType == ActionSubType.TBD1)
				{
					if (_secondAction.type != ActionType.Special)
						return PFCResultType.FirstWins;
					else
						return PFCResultType.Equal;
				}
				else if (_firstAction.subType == ActionSubType.TBD2)
				{
					if (_secondAction.type != ActionType.Special)
						return PFCResultType.SecondWins;
					else
						return PFCResultType.Equal;
				}
				break;
		}

		return PFCResultType.Failure;
	}

	#region Getters

	public bool ContainsEffect(EntityPassiveEffectEnumID _enumID, out AEntityPassiveEffect.PassiveEffectContainer _passiveEffect)
	{
		foreach(AEntityPassiveEffect.PassiveEffectContainer effectContainer in passiveEffects)
		{
			if(effectContainer.enumID == _enumID)
			{
				_passiveEffect = effectContainer;
				return true;
			}
		}

		_passiveEffect = new AEntityPassiveEffect.PassiveEffectContainer() { enumID = EntityPassiveEffectEnumID.Unknown, conditionType = AEntityPassiveEffect.ConditionType.Noone};
		return false;
	}

	public int GetTokenTotalCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		return GetTokenPreparationCost(_action, _performingEntity, _targetEntity) + GetTokenCooldownCost(_action, _performingEntity, _targetEntity) + tokenDuration;
	}

	public int GetTokenPreparationCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		PreparationCostReductionPassiveEffect so = (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.PreparationCostReduction] as PreparationCostReductionPassiveEffect);

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.PreparationCostReduction, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& so.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return m_tokenPreparationDuration - (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.PreparationCostReduction] as PreparationCostReductionPassiveEffect).reductionAmount;
		}

		return m_tokenPreparationDuration;

	}

	public int GetTokenCooldownCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		CooldownCostReductionPassiveEffect so = (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.CooldownCostReduction] as CooldownCostReductionPassiveEffect);

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.CooldownCostReduction, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& so.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return m_tokenCooldown - (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.CooldownCostReduction] as CooldownCostReductionPassiveEffect).reductionAmount;
		}

		return m_tokenCooldown;
	}

	public int GetMaxRange ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		MaxRangeUpPassiveEffect so = (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxRangeUp] as MaxRangeUpPassiveEffect);

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.MaxRangeUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& so.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return maxDistance + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxRangeUp] as MaxRangeUpPassiveEffect).rangeBoostAmount;
		}

		return maxDistance;
	}

	public int GetMaxTargetAmount ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		MaxTargetUpPassiveEffect so = (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxTargetUp] as MaxTargetUpPassiveEffect);

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.MaxTargetUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& so.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return maxTargetAmount + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxTargetUp] as MaxTargetUpPassiveEffect).targetBoostAmount;
		}

		return maxTargetAmount;
	}

	public float GetDamageFactorAmountForType ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity, WeaponEquipmentData.DamageType _damageType )
	{
		DamageUpPassiveEffect so = (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.DamageUpOnMarked] as DamageUpPassiveEffect);

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.DamageUpOnMarked, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& so.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return damageFactor + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.DamageUpOnMarked] as DamageUpPassiveEffect).damageBoostAmount;
		}

		return damageFactor;
	}
	
	public float GetHitAmount ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		HitAmountBoostPassiveEffect so = (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.HitAmountBoost] as HitAmountBoostPassiveEffect);

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.HitAmountBoost, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& so.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return hitAmount + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.HitAmountBoost] as HitAmountBoostPassiveEffect).hitAmountBoost;
		}

		return hitAmount;
	}

	#endregion

	public bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity )
	{
		if (_action == null || _entity == null)
			return false;

		switch (conditionType)
		{
			default:
			case ConditionType.Noone:
				return true;
			case ConditionType.DidNotMoveThisTurn:
				bool recordedCheck = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved >= _action.timeAtStart;
				bool liveCheck = !_entity.Displacement.DidMoveThisTurn;
				return liveCheck && recordedCheck;
			case ConditionType.DidNotAttackThisTurn:
				bool recordedCheck2 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked >= _action.timeAtStart;
				bool liveCheck2 = !_entity.Equipment.DidAttackThisTurn;
				return recordedCheck2 && liveCheck2;
			/*case ConditionType.IsTargetMarked:
				return _targetEntity != null && _targetEntity.Status.Contains(EntityStatusEnumID.Marked);*/
		}
	}
}
