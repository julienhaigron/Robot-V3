using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class AEntityAction
{
    public Action onPerform;
    public Action<Entity> onEndPerform;

    public int cost;
    public int cooldown;
    public EntityActionEnum type;
    public Entity performingEntity;
    public Tile positionAtActionStart;
    public Tile positionAtActionEnd;

    public virtual void Init(EntityActionData _data, Entity _performingEntity, Tile _positionAtActionStart )
	{
        cost = _data.tokenCost;
        cooldown = _data.tokenCooldown;
        type = _data.type;
        performingEntity = _performingEntity;
        positionAtActionStart = _positionAtActionStart;
        positionAtActionEnd = _positionAtActionStart;
    }

    public virtual void Prepare ( Entity.EntityState _state )
	{

	}

    public virtual void Perform (Entity.EntityState _state)
	{
        onPerform?.Invoke();
    }

    public abstract bool TileInteractPredicate ( Tile _tile );

    public abstract void RegisterInteraction ( Tile _tile);

    public virtual void EndPerform ()
    {
        Debug.Log("End perform");
        onEndPerform?.Invoke(performingEntity);
    }

    public abstract bool CheckConflict ( AEntityAction _otherAction );

    public abstract void Display ();
}
