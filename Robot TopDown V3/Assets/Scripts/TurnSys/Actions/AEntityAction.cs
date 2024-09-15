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
    public Tile supposedPositionAtActionStart;
    public Tile positionAtActionEnd;
    public bool isVisible;

    public virtual void Init(EntityActionData _data, Entity _performingEntity, Tile _positionAtActionStart )
	{
        cost = _data.tokenCost;
        cooldown = _data.tokenCooldown;
        type = _data.type;
        performingEntity = _performingEntity;
        supposedPositionAtActionStart = _positionAtActionStart;
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

    public virtual void RegisterInteraction ( Tile _tile )
	{
        TurnManager.Instance.AddAction(performingEntity, TurnManager.Instance.CurrentActionSelected);
    }

    public virtual void EndPerform ()
    {
        Debug.Log("End perform");
        onEndPerform?.Invoke(performingEntity);
    }

    public abstract bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true );

    public abstract void Display ();
}
