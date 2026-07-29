using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;


[CreateAssetMenu(fileName = "EntityActionData", menuName = "ScriptableObject/ActionData")]
public class EntityActionData : AParsableScriptableObject
{
	[Parsing("Nom")]
	public string displayName;
	[ReadOnly]
	public EntityActionEnumID enumID = EntityActionEnumID.Unknowned;

	public Sprite icon;
	public Color tileOutlineColor = Color.green;
	[SerializeField, Parsing("Préparation")] private int m_tokenPreparationDuration;
	[SerializeField, Parsing("Refroidissement")] private int m_tokenCooldown;
	[Parsing("Durée")]
	public int tokenDuration = 1;
	[Parsing("Is Mod Action")]
	public bool isModAction = false;

	[Title("Animation")]
	public string preparationAnimationKey;
	public string afterPerformAnimationKey;
	public SfxId onPerformSingleAttackSFXID;
	public SfxId onSingleAttackHitSFXID;

	[Title("Condition")]
	[Parsing("Condition")]
	public Condition.ConditionType conditionType = Condition.ConditionType.Noone;

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
	[Parsing("Type")]
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
	[Parsing("Sous type")]
	public ActionSubType subType;

	[Parsing("CodeType")]
	public ActionCodeType codeType = ActionCodeType.Attack;
	public enum ActionCodeType
	{
		//NeighborMove,
		TargetTileMove,
		Attack,
		MoveThenAttack,
		Special,
		AddEffectToAction,
		ApplyEffect,
		InvokeEntity,
		TurnShield,
		TurnEntity,
		Wait,
		InvokeItem,
		JumpMove
	}

	#region Target Vars
	[Title("Target")] //TODO: see this later https://odininspector.com/attributes/hide-if-group-attribute
	public enum TargetType
	{
		Self,
		OtherEntity,
		Tile
	}
	[Parsing("Target Type")]
	public TargetType targetType = TargetType.OtherEntity;
	public enum TrajectoryType
	{
		Direct,
		Mortar,
		Grenade,
		Underground,
		Throw
	}
	[Parsing("Trajectory Type")]
	public TrajectoryType trajectoryType = TrajectoryType.Direct;
	[ShowIf("@targetType != TargetType.Self"), Parsing("Min Distance")]
	public int minDistance;
	[ShowIf("@targetType != TargetType.Self"), Parsing("Max Distance")]
	public int maxDistance;

	[Parsing("Max Distance")]
	public int maxTargetAmount = 1;
	#endregion

	#region AOE Vars
	public enum AOEType { Noone, Circle, Ray, LargeCone, ThinCone, LargeArc, ThinArc, Chain }

	[Parsing("AoE Type")]
	public AOEType aoeType = AOEType.Noone;
	[ShowIf("@aoeType != AOEType.Noone"), Parsing("Does Affect Tile")]
	public bool doesAffectTile = false;
	public enum AOECenterType
	{
		Self,
		Target
	}
	[ShowIf("@aoeType != AOEType.Noone"), Parsing("AoE Center Type")]
	public AOECenterType aoECenterType = AOECenterType.Self;
	[ShowIf("@aoeType != AOEType.Noone"), Parsing("Min AoE Effect Range")]
	public int aoeMinEffectRange = 0;
	[ShowIf("@aoeType != AOEType.Noone"), Parsing("Max AoE Effect Range")]
	public int aoeMaxEffectRange = 0;
	[ShowIf("@aoeType == AOEType.Chain"), Min(1)]
	public int maxChainedTarget = 1;
	[ShowIf("@aoeType == AOEType.Chain"), Min(1)]
	public float damageReductionOnChain = .1f;

	#endregion

	[Title("Damage"), Parsing("Hit Amount")]
	public int hitAmount = 1;
	public float damageFactor = 1f;
	[Parsing("Base damages")]
	public SerializableDictionary<WeaponEquipmentData.DamageType, int> baseDamages;

	[Title("Effect"), Parsing("Appliable Status")]
	public AEntityStatus[] appliableStatus;
	[Parsing("Status Hit Probability")]
	public float statusHitProbability;
	[Parsing("Passif")]
	public AEntityPassiveEffect.PassiveEffectContainer[] passiveEffects;

	[Title("Misc")]
	[ShowIf("@codeType == ActionCodeType.InvokeEntity")] public UnitPreset invocatedEntity;
	[ShowIf("@codeType == ActionCodeType.InvokeItem")] public AItemData invocatedItem;
	[ShowIf("@codeType == ActionCodeType.InvokeEntity || codeType == ActionCodeType.InvokeItem")] public int invocationCountLimit = 1;
	[ShowIf("@type == ActionType.Movement || codeType == ActionCodeType.MoveThenAttack ")] public int movementSpeed = 1;

	
	public enum MainActionType
	{
		Attack,
		Movement,
		Special
	}

	public MainActionType GetMainActionType ()
	{
		switch (type)
		{
			case ActionType.DistanceAttack:
			case ActionType.MeleeAttack:
				return MainActionType.Attack;
			case ActionType.Movement:
				return MainActionType.Movement;
			case ActionType.Rotation:
			case ActionType.Special:
				return MainActionType.Special;
		}

		return MainActionType.Special;
	}

	public enum PFCResultType
	{
		Failure,
		FirstWins,
		SecondWins,
		Equal
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

		_passiveEffect = new AEntityPassiveEffect.PassiveEffectContainer() { enumID = EntityPassiveEffectEnumID.Unknown, conditionType = Condition.ConditionType.Noone};
		return false;
	}

	public int GetTokenTotalCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		return GetTokenPreparationCost(_action, _performingEntity, _targetEntity) + GetTokenCooldownCost(_action, _performingEntity, _targetEntity) + tokenDuration;
	}

	public int GetTokenPreparationCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.PreparationCostReduction, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return m_tokenPreparationDuration - (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.PreparationCostReduction] as PreparationCostReductionPassiveEffect).reductionAmount;
		}

		return m_tokenPreparationDuration;

	}

	public int GetTokenCooldownCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.CooldownCostReduction, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return m_tokenCooldown - (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.CooldownCostReduction] as CooldownCostReductionPassiveEffect).reductionAmount;
		}

		return m_tokenCooldown;
	}

	public int GetMaxRange ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.MaxRangeUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return maxDistance + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxRangeUp] as MaxRangeUpPassiveEffect).rangeBoostAmount;
		}

		return maxDistance;
	}

	public int GetAoEMaxRange ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.MaxAoERangeUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return aoeMaxEffectRange + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxAoERangeUp] as MaxAoERangeUpPassiveEffect).rangeBoostAmount;
		}

		return aoeMaxEffectRange;
	}

	public int GetMaxTargetAmount ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.MaxTargetUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return maxTargetAmount + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxTargetUp] as MaxTargetUpPassiveEffect).targetBoostAmount;
		}

		return maxTargetAmount;
	}

	public float GetDamageFactorAmountForType ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity, WeaponEquipmentData.DamageType _damageType )
	{
		float totalDamageFactor = damageFactor;
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.DamageUpOnMarked, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			totalDamageFactor += (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.DamageUpOnMarked] as DamageUpPassiveEffect).damageBoostAmount;
		}

		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.DoubleDamage, out AEntityPassiveEffect.PassiveEffectContainer effectContainer2)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer2.conditionType))
		{
			totalDamageFactor += (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.DoubleDamage] as DamageUpPassiveEffect).damageBoostAmount;
		}

		return totalDamageFactor;
	}
	
	public int GetHitAmount ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(EntityPassiveEffectEnumID.HitAmountBoost, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return hitAmount + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.HitAmountBoost] as HitAmountBoostPassiveEffect).hitAmountBoost;
		}

		return hitAmount;
	}

	#endregion

	protected override string GetSheetID ()
	{
		return GameConfig.current.parsing.actionGUIDPerPage[type];
	}

	public override void OnParse ( ImportedData _data )
	{
		/*if(enumID == EntityActionEnumID.Unknowned)
		{
			if (System.Enum.TryParse(typeof(EntityActionEnumID), _data.id, out object result))
				enumID = (EntityActionEnumID)result;
			else
			{
				ScriptableEnumAutoEditor.GenerateEnum(typeof(EntityActionData), typeof(EntityActionEnumID));
				enumID = (EntityActionEnumID)System.Enum.Parse(typeof(EntityActionEnumID), _data.id);
			}
		}*/

		if(aoeType != AOEType.Noone)
		{
			switch (aoeType)
			{
				case AOEType.Chain:
					maxChainedTarget = _data.GetValue<int>("AoE Extra Values");
					break;
				/*case AOEType.Cone:
					coneType = _data.GetValue<ConeType>("AoE Extra Values");
					break;*/
			}

			if(aoECenterType == AOECenterType.Self)
			{
				aoeMinEffectRange = minDistance;
				aoeMaxEffectRange = maxDistance;
			}
		}

		if(codeType == ActionCodeType.InvokeItem)
			invocatedItem = _data.GetValue<AItemData>("Special Extra Values");
	}
}
