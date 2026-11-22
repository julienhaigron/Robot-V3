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
	[ShowIf("@isModAction")]
	public bool isPlayedFirst = true;

	[Title("Animation")]
	public string preparationAnimationKey;
	public string afterPerformAnimationKey;
	public string performAnimationKey;
	[ShowIf("@type == ActionType.MeleeAttack || !string.IsNullOrEmpty(performAnimationKey)"), Min(0f)] public float performAnimationHitDelay = .3f;

	public enum PreparationAnimationMode
	{
		None,
		WindUp,
		Trigger
	}

	public enum RecoveryAnimationMode
	{
		None,
		Hold,
		Trigger
	}

	[ShowIf("@m_tokenPreparationDuration > 0"), Tooltip("Ticks de préparation. WindUp : la frappe s'arme lentement jusqu'à la pose armée et la tient, puis repart de là au tick d'action ; un tir vise sa cible dès la préparation. Trigger : joue preparationAnimationKey.")]
	public PreparationAnimationMode preparationAnimationMode;
	[ShowIf("@m_tokenPreparationDuration > 0 && preparationAnimationMode == PreparationAnimationMode.WindUp && type != ActionType.DistanceAttack"), Min(0f), Tooltip("Pose armée : secondes après le déclenchement de la frappe, à la vitesse du contrôleur, avant le délai d'impact.")]
	public float preparationHoldDelay = .2f;
	[ShowIf("@m_tokenPreparationDuration > 0 && preparationAnimationMode == PreparationAnimationMode.WindUp && type != ActionType.DistanceAttack"), Range(.1f, 1f), Tooltip("Part de la préparation passée à armer ; le reste tient la pose.")]
	public float preparationWindUpRatio = .8f;
	[ShowIf("@m_tokenCooldown > 0"), Tooltip("Ticks de refroidissement, après la dernière frappe. Hold : le geste ralentit et se fige à la pose de tenue, puis repart à la fin de l'action ; un tir garde la visée jusqu'à la fin de l'action. Trigger : joue afterPerformAnimationKey à ce moment-là.")]
	public RecoveryAnimationMode recoveryAnimationMode;
	[ShowIf("@m_tokenCooldown > 0 && recoveryAnimationMode != RecoveryAnimationMode.None && type != ActionType.DistanceAttack"), Min(0f), Tooltip("Secondes après le déclenchement de la frappe, à la vitesse du contrôleur, après le délai d'impact.")]
	public float recoveryHoldDelay = .6f;

	public bool IsShot => type == ActionType.DistanceAttack;
	public bool AimsDuringPreparation => IsShot && preparationAnimationMode == PreparationAnimationMode.WindUp;
	public bool HoldsAimDuringCooldown => IsShot && recoveryAnimationMode == RecoveryAnimationMode.Hold;

	public bool KeepsAimAfterVolley ( bool _isLastPerformTick, int _cooldownTicks )
	{
		return KeepsAimAfterVolley(_isLastPerformTick, _cooldownTicks, HoldsAimDuringCooldown);
	}

	public static bool KeepsAimAfterVolley ( bool _isLastPerformTick, int _cooldownTicks, bool _holdsAimDuringCooldown )
	{
		return !_isLastPerformTick || _cooldownTicks > 0 && _holdsAimDuringCooldown;
	}
	[HideIf("@type == ActionType.MeleeAttack || type == ActionType.Movement"), Min(0f)] public float shotRecoilStrength = 1f;
	public SfxId onPerformSingleAttackSFXID;
	public SfxId onSingleAttackHitSFXID;
	public PoolData onSingleAttackHitVFXPool;
	public PoolData onPerformVFXPool;
	public PoolData movementTrailPool;

	public enum VisualEffectType
	{
		Projectile,
		Spell,
		OnlyCharacterAnimation
	}
	[Title("Visual Effect")]
	[Parsing("VisuelEffectType")]
	public VisualEffectType visualEffectType = VisualEffectType.OnlyCharacterAnimation;
	public float visualEffectDuration = .3f;

	[ShowIf("@visualEffectType == VisualEffectType.Projectile")] public PoolData projectilePool;
	[ShowIf("@visualEffectType == VisualEffectType.Projectile")] public float projectileSpeed = 40f;
	[ShowIf("@visualEffectType == VisualEffectType.Spell")] public GameObject spellVfxPrefab;
	[ShowIf("@visualEffectType == VisualEffectType.Spell")] public float spellVfxLifetime = 2f;

	public enum AttackProjectileMode
	{
		None,
		Single,
		Chain,
		Spray
	}
	[Title("Attack Projectile")]
	public AttackProjectileMode attackProjectileMode = AttackProjectileMode.None;
	[ShowIf("@attackProjectileMode != AttackProjectileMode.None")] public PoolData attackProjectilePool;
	[ShowIf("@attackProjectileMode != AttackProjectileMode.None")] public float attackProjectileSpeed = 20f;
	[ShowIf("@attackProjectileMode != AttackProjectileMode.None")] public bool doesAttackProjectileReturn;
	[ShowIf("@attackProjectileMode == AttackProjectileMode.Spray")] public int attackProjectileSprayCount = 10;
	[ShowIf("@attackProjectileMode == AttackProjectileMode.Spray")] public float attackProjectileSprayInterval = .04f;
	[ShowIf("@attackProjectileMode == AttackProjectileMode.Spray")] public bool doesSprayRepeatPerformVFX = true;

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
		Special
	}
	[Parsing("Type")]
	public ActionType type;

	public static bool IsRotationAction ( EntityActionEnumID _enumID )
	{
		return _enumID == EntityActionEnumID.RotateEntity || _enumID == EntityActionEnumID.RotateToEntity;
	}

	public bool IsRotation => IsRotationAction(enumID);

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
		JumpMove,
		MoveItem
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

	[Parsing("Target Amount")]
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

	/*[Title("Effect"), Parsing("Appliable Status")]
	public AEntityStatus[] appliableStatus;*/
	[Parsing("Status Hit Probability")]
	public float statusHitProbability;
	[Parsing("Passif")]
	public AEntityPassiveEffect.PassiveEffectContainer[] passiveEffects;

	[Title("Misc")]
	[ShowIf("@codeType == ActionCodeType.InvokeEntity")] public UnitPreset invocatedEntity;
	[ShowIf("@codeType == ActionCodeType.InvokeItem || codeType == ActionCodeType.MoveItem")] public AItemData invocatedItem;
	[ShowIf("@codeType == ActionCodeType.InvokeEntity || codeType == ActionCodeType.InvokeItem")] public int invocationCountLimit = 1;
	[ShowIf("@type == ActionType.Movement || codeType == ActionCodeType.MoveThenAttack ")] public int movementSpeed = 1;
	[ShowIf("@type == ActionType.Movement || codeType == ActionCodeType.MoveThenAttack "), Tooltip("Style de déplacement : l'état « Move <nom> » du calque Locomotion (Walk, Run, Dash…). Vide = Walk. En vol, l'état « Move Fly » passe devant.")]
	public string movementAnimation = "";


	public enum MainActionType
	{
		Attack,
		Movement,
		Special
	}

	public bool DoesResolveItsOwnTarget ()
	{
		MainActionType mainType = GetMainActionType();
		return targetType == TargetType.OtherEntity
			&& (mainType == MainActionType.Attack || mainType == MainActionType.Special);
	}

	public bool FiresProjectileAtTarget ()
	{
		return type == ActionType.DistanceAttack
			&& (aoeType == AOEType.Noone || (aoeType == AOEType.Circle && aoECenterType == AOECenterType.Target && targetType != TargetType.Self));
	}

	public bool MovesItsUnit => codeType == ActionCodeType.TargetTileMove || codeType == ActionCodeType.JumpMove || codeType == ActionCodeType.MoveThenAttack;

	public bool ClaimsTargetTiles => codeType == ActionCodeType.InvokeItem || codeType == ActionCodeType.InvokeEntity;

	public bool DoesNeedATargetTile ()
	{
		return targetType != TargetType.Self
			&& codeType != ActionCodeType.Wait
			&& !DoesResolveItsOwnTarget();
	}

	public bool TryGetInvocationTool ( Entity _entity, string _linkedEquipmentID, out ToolEquipment _tool )
	{
		_tool = null;
		return codeType == ActionCodeType.InvokeItem && invocatedItem != null && _entity != null && !string.IsNullOrEmpty(_linkedEquipmentID)
			&& _entity.Equipment.Tools.TryGetValue(_linkedEquipmentID, out _tool) && _tool != null;
	}

	public bool CanInvokeMoreItems ( Entity _entity, string _linkedEquipmentID )
	{
		return !TryGetInvocationTool(_entity, _linkedEquipmentID, out ToolEquipment tool) || invocatedItem.InvokeItemPredicate(tool, this);
	}

	public MainActionType GetMainActionType ()
	{
		if (IsRotation)
			return MainActionType.Special;

		switch (type)
		{
			case ActionType.DistanceAttack:
			case ActionType.MeleeAttack:
				return MainActionType.Attack;
			case ActionType.Movement:
				return MainActionType.Movement;
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

	public bool ContainsEffect (Entity _user, EntityPassiveEffectEnumID _enumID, out AEntityPassiveEffect.PassiveEffectContainer _passiveEffect )
	{
		foreach (AEntityPassiveEffect.PassiveEffectContainer effectContainer in passiveEffects)
		{
			if (effectContainer.enumID == _enumID)
			{
				_passiveEffect = effectContainer;
				return true;
			}
		}

		foreach(AEntityPassiveEffect.PassiveEffectContainer effectContainer in _user.AllPassiveEffects)
		{
			if (effectContainer.enumID == _enumID)
			{
				_passiveEffect = effectContainer;
				return true;
			}
		}

		_passiveEffect = new AEntityPassiveEffect.PassiveEffectContainer() { enumID = EntityPassiveEffectEnumID.Unknown, conditionType = Condition.ConditionType.Noone };
		return false;
	}

	public int GetTokenTotalCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		return GetTokenPreparationCost(_action, _performingEntity, _targetEntity) + GetTokenCooldownCost(_action, _performingEntity, _targetEntity) + tokenDuration;
	}

	public int GetTokenPreparationCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		int totalTokenCooldown = m_tokenPreparationDuration;
		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.PreparationCostReduction, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			totalTokenCooldown -= (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.PreparationCostReduction] as PreparationCostReductionPassiveEffect).reductionAmount;
		}

		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.QuickDraw, out AEntityPassiveEffect.PassiveEffectContainer effectContainer2)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer2.conditionType))
		{
			totalTokenCooldown -= (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.QuickDraw] as QuickDrawPassiveEffect).reductionAmount;
		}

		return Mathf.Max(0, totalTokenCooldown);
	}

	public int GetTokenCooldownCost ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		int totalTokenCooldown = m_tokenCooldown;
		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.CooldownCostReduction, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			totalTokenCooldown -= (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.CooldownCostReduction] as CooldownCostReductionPassiveEffect).reductionAmount;
		}

		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.QuickDraw, out AEntityPassiveEffect.PassiveEffectContainer effectContainer2)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer2.conditionType))
		{
			totalTokenCooldown -= (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.QuickDraw] as QuickDrawPassiveEffect).reductionAmount;
		}

		return Mathf.Max(0, totalTokenCooldown);
	}

	public int GetMaxRange ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.MaxRangeUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return maxDistance + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxRangeUp] as MaxRangeUpPassiveEffect).rangeBoostAmount;
		}

		return maxDistance;
	}

	public int GetAoEMaxRange ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.MaxAoERangeUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return aoeMaxEffectRange + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxAoERangeUp] as MaxAoERangeUpPassiveEffect).rangeBoostAmount;
		}

		return aoeMaxEffectRange;
	}

	public int GetMaxTargetAmount ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.MaxTargetUp, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			return maxTargetAmount + (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.MaxTargetUp] as MaxTargetUpPassiveEffect).targetBoostAmount;
		}

		return maxTargetAmount;
	}

	public float GetDamageFactorAmountForType ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity, WeaponEquipmentData.DamageType _damageType )
	{
		float totalDamageFactor = damageFactor;
		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.DamageUpOnMarked, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			totalDamageFactor += (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.DamageUpOnMarked] as DamageUpPassiveEffect).damageBoostAmount;
		}

		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.DoubleDamage, out AEntityPassiveEffect.PassiveEffectContainer effectContainer2)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer2.conditionType))
		{
			totalDamageFactor += (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.DoubleDamage] as DamageUpPassiveEffect).damageBoostAmount;
		}

		return totalDamageFactor;
	}

	public int GetHitAmount ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		int amount = hitAmount;

		if (_action != null && _performingEntity != null && ContainsEffect(_performingEntity, EntityPassiveEffectEnumID.HitAmountBoost, out AEntityPassiveEffect.PassiveEffectContainer effectContainer)
			&& Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, effectContainer.conditionType))
		{
			amount += (GameAssets.current.game.entityEffects[EntityPassiveEffectEnumID.HitAmountBoost] as HitAmountBoostPassiveEffect).hitAmountBoost;
		}

		return Mathf.Max(1, amount);
	}

	public List<ApplyStatusPassiveEffect> GetAppliedStatusEffects ( AEntityAction _action, Entity _performingEntity, Entity _targetEntity )
	{
		List<ApplyStatusPassiveEffect> appliedStatusEffects = new();

		foreach(AEntityPassiveEffect.PassiveEffectContainer pe in passiveEffects)
		{
			if(Condition.UseConditionPredicate(_action, _performingEntity, _targetEntity, pe.conditionType)
				&& GameAssets.current.game.entityEffects[pe.enumID] is ApplyStatusPassiveEffect applyStatusPassiveEffect
				&& GameAssets.current.game.entityStatus[applyStatusPassiveEffect.statusApplied])
				appliedStatusEffects.Add(applyStatusPassiveEffect);
		}

		return appliedStatusEffects;
	}

	#endregion

	protected override string GetSheetID ()
	{
		return GameConfig.current.parsing.actionGUIDPerPage[type];
	}

	public override void OnParse ( ImportedData _data )
	{
#if UNITY_EDITOR
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

		if (aoeType != AOEType.Noone)
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

			if (aoECenterType == AOECenterType.Self)
			{
				aoeMinEffectRange = minDistance;
				aoeMaxEffectRange = maxDistance;
			}
		}

		if (codeType == ActionCodeType.InvokeItem || codeType == ActionCodeType.MoveItem)
			invocatedItem = _data.GetValue<AItemData>("Special Extra Values");
#endif
	}
}
