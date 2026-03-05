using UnityEngine;
using System;

[Serializable]
public abstract class AEntityStatus : ScriptableEnum<EntityStatusEnumID>
{
    public int duration = 1;
    [Range(0, 100)] public float hitProbability = 50f;


    public virtual void ApplyStatus ( Entity _entity )
    {

    }
}