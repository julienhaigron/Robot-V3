using UnityEngine;
using System;

[Serializable]
public abstract class AEntityPassiveEffect : ScriptableEnum<EntityPassiveEffectEnumID>
{

    public virtual void ApplyEffect ( Entity _entity )
    {

    }
}