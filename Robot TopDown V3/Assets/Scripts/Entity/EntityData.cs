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
    public List<EntityActionType> knownedActions = new();
    [Min(0)] public int visibilityRange = 8;

    [Title("AI")]
    public List<EntityCapacityAsset.EntityCapacityType> capacities = new();

    [Title("Structure")]
    public float maxHealth;
}
