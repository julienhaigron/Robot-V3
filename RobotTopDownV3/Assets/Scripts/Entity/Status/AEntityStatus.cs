using UnityEngine;
using System;

[Serializable]
public abstract class AEntityStatus : ScriptableEnum<EntityStatusEnumID>
{
    public int duration = 1;
    [Range(0, 100)] public float hitProbability = 50f;

    public Sprite icon;

    //called each action tick
    public virtual void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
    {

    }

    public virtual void OnRemoveStatusEffect( Entity _entity )
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