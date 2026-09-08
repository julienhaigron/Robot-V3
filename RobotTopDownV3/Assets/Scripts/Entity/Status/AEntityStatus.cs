using UnityEngine;
using System;

[Serializable]
public abstract class AEntityStatus : ScriptableEnum<EntityStatusEnumID>
{
    public int duration = 1;
    public bool doesNeedRoll = false;
    [TextArea(2, 6)] public string description;

    public Sprite icon;
    public GameObject groundPrefab;

    public string GetTooltip ( int _remainingDuration )
    {
        System.Text.StringBuilder builder = new();
        builder.AppendLine($"<b>{name}</b>");
        builder.AppendLine($"Duration: {duration} turns");
        builder.AppendLine($"Remaining: {_remainingDuration} turns");
        if (doesNeedRoll)
            builder.AppendLine("Applied through a status roll");
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine();
            builder.AppendLine(description);
        }

        return builder.ToString();
    }

    //called each action tick
    public virtual void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
    {

    }

    public virtual void OnRemoveStatusEffect ( Entity _entity )
    {

    }

    public virtual void ApplyStatus(Tile _tile )
	{
        //TODO :
        //- spawn visual prefab (smoke, fire, ...)
	}

    public virtual void RemoveStatus(Tile _tile )
	{

	}

    public virtual void PerformStatusEffectAtBeginingOfRound(Tile _tile )
	{

    }
}