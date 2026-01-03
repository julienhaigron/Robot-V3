using UnityEngine;
using System;

[Serializable]
public abstract class AEntityEffect : ScriptableObject
{
    public int duration = 1;
    [Range(0, 100)] public float hitProbability = 50f;
    public EntityEffectEnumID enumId;
    public enum EntityEffectEnumID
	{
        Stun,
        Burn1
	}


    public virtual void ApplyEffect (Entity _entity)
	{

	}
}
