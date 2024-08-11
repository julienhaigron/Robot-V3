using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityActionData", menuName = "Tools/Scriptables/Entity Action")]
public class EntityActionData : ScriptableObject
{
    public string displayName;
    public EntityActionEnum type;
    public Sprite icon;
    [Min(0)] public int tokenCost;
    [Min(0)] public int tokenCooldown;
}
