using UnityEngine;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[Serializable]
public abstract class AEntityStatus : ScriptableEnum<EntityStatusEnumID>
{
    public enum DurationType { Tick, Round, Charges }
    public enum ChargeConsumer { None, MeleeAttack, Shot, MultiTargetShot, Movement }

    public DurationType durationType = DurationType.Tick;
    [ShowIf(nameof(durationType), DurationType.Charges)] public ChargeConsumer consumedBy = ChargeConsumer.None;
    [ShowIf(nameof(durationType), DurationType.Charges)] public bool doesStackCharges = false;
    [ShowIf(nameof(durationType), DurationType.Charges)] public bool isBoundAtPlanning = false;
    public bool doesNeedRoll = false;
    [TextArea(2, 6)] public string description;

    public Sprite icon;
    public Color color = Color.white;
    public GameObject groundPrefab;

    public enum EntityVisualAnchor { Center, Feet, AboveHead }
    public GameObject entityPrefab;
    public EntityVisualAnchor entityVisualAnchor = EntityVisualAnchor.Center;

    public bool DoesConsumeDurationThisTick ()
    {
        return durationType == DurationType.Tick
            || (durationType == DurationType.Round && (TurnManager.Instance == null || TurnManager.Instance.IsLastTickOfRound));
    }

    public bool IsLastActiveTick ( int _remainingDuration )
    {
        return _remainingDuration <= 1 && DoesConsumeDurationThisTick();
    }

    public bool IsConsumedBy ( AEntityAction _action )
    {
        if (durationType != DurationType.Charges || _action == null || _action.Data == null || _action.Data.isModAction)
            return false;

        EntityActionData data = _action.Data;
        return consumedBy switch
        {
            ChargeConsumer.MeleeAttack => data.type == EntityActionData.ActionType.MeleeAttack,
            ChargeConsumer.Shot => data.type == EntityActionData.ActionType.DistanceAttack,
            ChargeConsumer.MultiTargetShot => data.type == EntityActionData.ActionType.DistanceAttack && data.maxTargetAmount > 1,
            ChargeConsumer.Movement => data.GetMainActionType() == EntityActionData.MainActionType.Movement,
            _ => false
        };
    }

    public virtual void OnBoostedHit ( AttackAction _action, Entity _attacker, Entity _target, Dictionary<WeaponEquipmentData.DamageType, int> _damages, bool _isFirstHitOnTarget )
    {

    }

    public virtual void OnHitReceived ( AttackAction _action, Entity _holder, Entity _attacker )
    {

    }

    public virtual string GetTickEffectText ( int _remainingDuration, Entity _entity )
    {
        return null;
    }

    //called each action tick
    public virtual void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
    {

    }

    public virtual void OnRemoveStatusEffect ( Entity _entity )
    {

    }

    public virtual void ApplyStatus(Tile _tile )
	{
        //TODO :
        //- spawn visual prefab (smoke, fire, ...)
	}

    public virtual void RemoveStatus(Tile _tile )
	{

	}

    public virtual void PerformStatusEffectAtBeginingOfRound(Tile _tile )
	{

    }
}
