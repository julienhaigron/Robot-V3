using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObject/EntityData", order = 1)]
public class EntityData : ScriptableObject
{
    public int actionTokenAmount = 8;
    public List<EntityActionEnum> knownedActions = new();
}
