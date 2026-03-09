using UnityEngine;
using System;

[Serializable]
public abstract class AEntityStatus : ScriptableEnum<EntityStatusEnumID>
{
    public int duration = 1;
    [Range(0, 100)] public float hitProbability = 50f;


    public virtual void ApplyStatusEffect ( Entity _entity )
    {

    }

    public virtual void ApplyStatus(Tile _tile )
	{
        //TODO :
        //- spawn visual prefab (smoke, fire, ...)
	}

    public virtual void PerformStatusEffectAtBeginingOfRound(Tile _tile )
	{
        //TODO :
        //- apply status effect to entity on tile (ex damage for fire)
    }
}