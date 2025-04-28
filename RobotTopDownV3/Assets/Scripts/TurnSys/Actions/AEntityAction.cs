using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public abstract class AEntityAction : INetworkSerializable
{
    public Action onPerform;
    public Action<Entity> onEndPerform;

    public int cost;
    public int cooldown;
    public EntityActionType type;
    public Entity performingEntity;
    public Tile supposedPositionAtActionStart;
    public Tile positionAtActionEnd;
    public bool isVisible;

    public virtual void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref cooldown);
        serializer.SerializeValue(ref type);
        //TODO : replace Entity and Tile to id of said objects
        /*serializer.NetworkSerialize(ref performingEntity);
        serializer.NetworkSerialize(ref supposedPositionAtActionStart);
        serializer.NetworkSerialize(ref positionAtActionEnd);*/
        serializer.SerializeValue(ref isVisible);
    }

    public virtual void Init(EntityActionData _data, Entity _performingEntity, Tile _positionAtActionStart )
	{
        cost = _data.tokenCost;
        cooldown = _data.tokenCooldown;
        type = _data.type;
        performingEntity = _performingEntity;
        supposedPositionAtActionStart = _positionAtActionStart;
        positionAtActionEnd = _positionAtActionStart;
    }

    public abstract void Prepare ( Entity.EntityState _state );

    public virtual void Perform (Entity.EntityState _state)
	{
        onPerform?.Invoke();
    }

    public abstract bool TileInteractPredicate ( Tile _tile );

    public virtual void RegisterInteraction ( Tile _tile )
	{
        TurnManager.Instance.AddAction(performingEntity, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.CurrentStateTypeSelected);

        TurnManager.Instance.RefreshActionDisplay(performingEntity);
    }

    public virtual void EndPerform ()
    {
        onEndPerform?.Invoke(performingEntity);
    }

    public abstract bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true );

    public abstract void Display ();

	public override string ToString ()
	{
		return performingEntity.Data.name + "," + type.ToString();
	}
}
