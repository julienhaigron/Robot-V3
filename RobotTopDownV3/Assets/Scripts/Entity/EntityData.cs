using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObject/EntityData", order = 1)]
public class EntityData : ScriptableObject
{
    public Entity.EntityFaction faction;

    [Title("Action")]
    public int actionTokenAmount = 8;
    public EntityActionType[] knownedActions;
    [Min(0)] public int visibilityRange = 8;

    [Title("AI")]
    public EntityCapacityAsset.EntityCapacityType[] capacities;
    public Entity.EntityState[] knownedStates;

    [Title("Structure")]
    public int maxHealth;
}
