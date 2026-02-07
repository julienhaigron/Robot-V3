using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "EntityActionData", menuName = "Tools/Scriptables/Entity Action")]
public class EntityActionData : ScriptableObject
{
    public string displayName;
    public EntityActionEnumID enumID;
    public Sprite icon;
    public Color tileOutlineColor = Color.green;
    [Min(0)] public int tokenCost;
    [Min(0)] public int tokenPreparationDuration;
    [Min(0)] public int tokenCooldown;
    [Min(0)] public int tokenDuration;

    [Title("Stats")]
    public float previousActionAttackModificator = 0;

    public enum ActionType
	{
        DistanceAttack,
        MeleeAttack,
        Movement,
        Rotation,
        Special
	}
    public ActionType type;

    public enum ActionSubType
	{
        Fuite,
        Reflexe,
        Esquive,
        Barrage,
        Assaut,
        Poursuite
    }
    public ActionSubType subType;

    [Title("Target")] //TODO: see this later https://odininspector.com/attributes/hide-if-group-attribute
    public enum TargetType
    {
        Self,
        Distance,
        Mortar
    }
    public TargetType targetType = TargetType.Distance;
    [ShowIf("@targetType != TargetType.Self")]
    public int minDistance;
    [ShowIf("@targetType != TargetType.Self")]
    public int maxDistance;

    public int minTargetAmount = 1;
    public int maxTargetAmount = 1;

    [ShowIf("@targetType != TargetType.Self")]
    public bool isAoe = false;
    public enum AOEType
	{
        Circle,
        Ray,
        Cone
	}
    [ShowIf("@isAoe")]
    public AOEType aoeType = AOEType.Circle;
    [ShowIf("@isAoe && aoeType == AOEType.Circle"), Min(1)]
    public int circleRadius = 1;
    [ShowIf("@isAoe && aoeType == AOEType.Ray"), Min(1)]
    public int rayDiameter = 1;
    [ShowIf("@isAoe && aoeType == AOEType.Cone"), Min(1)]
    public int coneSpread = 1;

    public AEntityEffect[] appliableEffects;
}
