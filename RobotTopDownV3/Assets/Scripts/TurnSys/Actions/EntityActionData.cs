using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "EntityActionData", menuName = "Tools/Scriptables/Entity Action")]
public class EntityActionData : ScriptableObject
{
	public string displayName;
	[ReadOnly]
	public EntityActionEnumID enumID = EntityActionEnumID.Unknowned;

	public Sprite icon;
	public Color tileOutlineColor = Color.green;
	public int tokenCost => tokenPreparationDuration + tokenDuration;
	[Min(0)] public int tokenPreparationDuration;
	[Min(0)] public int tokenCooldown;
	[Min(0)] public int tokenDuration;

	[Title("Animation")]
	public string preparationAnimationKey;
	public string afterPerformAnimationKey;

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

	[Title("Target")] //TODO: see this later https://odininspector.com/attributes/hide-if-group-attribute
	public enum TargetType
	{
		Self,
		Distance,
		Mortar
	}
	public TargetType targetType = TargetType.Distance;
	[ShowIf("@targetType != TargetType.Self")]
	public int minDistance;
	[ShowIf("@targetType != TargetType.Self")]
	public int maxDistance;

	public int minTargetAmount = 1;
	public int maxTargetAmount = 1;

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

	[Title("Damage")]
	public WeaponEquipmentData.DamageType[] usedDamageChannels;

	[Title("Effect")]
	[Min(0)] public int pushStrenght = 0;
	[Min(0)] public int pullStrenght = 0;

	public AEntityStatus[] appliableStatus;
	public AEntityPassiveEffect[] passiveEffects;

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
}
