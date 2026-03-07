using UnityEngine;
using System;

[Serializable]
public abstract class AEntityPassiveEffect : ScriptableEnum<EntityPassiveEffectEnumID>
{
    public abstract bool UseConditionPredicate( Entity _performingEntity, Entity _targetEntity );

    public virtual void ApplyEffect ( Entity _performingEntity, Entity _targetEntity )
    {

    }
}