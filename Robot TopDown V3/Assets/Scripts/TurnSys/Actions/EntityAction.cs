using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EntityAction
{
    public Action onPerform;
    public Action<Entity> onEndPerform;

    public int cost;
    public int cooldown;
    public EntityActionEnum type;
    public Entity performingEntity;

    public void Init(EntityActionData _data, Entity _performingEntity )
	{
        cost = _data.tokenCost;
        cooldown = _data.tokenCooldown;
        type = _data.type;
        performingEntity = _performingEntity;
    }

    public virtual void Prepare ( Entity.EntityState _state )
	{

	}

    public virtual void Perform (Entity.EntityState _state)
	{
        onPerform?.Invoke();
    }

    public virtual bool TileInteractPredicate(Tile _tile )
	{
        return true;
	}

    public virtual void EndPerform ()
    {
        onEndPerform?.Invoke(performingEntity);
    }

    public virtual bool CheckConflict(EntityAction _otherAction )
	{
        return false;
	}
}
