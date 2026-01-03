using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityActionData", menuName = "Tools/Scriptables/Entity Action")]
public class EntityActionData : ScriptableObject
{
    public string displayName;
    public EntityActionEnumID type;
    public Sprite icon;
    public Color tileOutlineColor = Color.green;
    [Min(0)] public int tokenCost;
    [Min(0)] public int tokenCooldown;
    public AEntityEffect[] appliableEffects;
}
