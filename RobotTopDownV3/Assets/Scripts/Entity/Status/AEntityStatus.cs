using UnityEngine;
using System;

[Serializable]
public abstract class AEntityStatus : ScriptableEnum<EntityStatusEnumID>
{
    public enum DurationType { Tick, Round }

    public DurationType durationType = DurationType.Tick;
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
            || TurnManager.Instance == null
            || TurnManager.Instance.IsLastTickOfRound;
    }

    public bool IsLastActiveTick ( int _remainingDuration )
    {
        return _remainingDuration <= 1 && DoesConsumeDurationThisTick();
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