using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityActionData", menuName = "Tools/Scriptables/Entity Action")]
public class EntityActionData : ScriptableObject
{
    public string displayName;
    public EntityActionEnumID typeID;
    public Sprite icon;
    public Color tileOutlineColor = Color.green;
    [Min(0)] public int tokenCost;
    [Min(0)] public int tokenCooldown;
    public AEntityEffect[] appliableEffects;

    public enum ActionType
	{
        DistanceAttack,
        MeleeAttack,
        Movement,
        Rotation,
        Special
	}
    public ActionType type;

}
