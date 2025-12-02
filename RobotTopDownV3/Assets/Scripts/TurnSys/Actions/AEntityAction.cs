using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public abstract class AEntityAction : INetworkSerializable
{
    public Action onPerform;
    public Action<int> onEndPerform;

    public int cost;
    public int cooldown;
    public EntityActionType type;
    public int performingEntityID; //entity
    public int supposedPositionAtActionStartID; //tile
    public int positionAtActionEndID; //tile

    public virtual void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref cooldown);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref performingEntityID);
		serializer.SerializeValue(ref supposedPositionAtActionStartID);
		serializer.SerializeValue(ref positionAtActionEndID);
    }

    public virtual void Init(EntityActionData _data, int _performingEntityID, int _positionAtActionStartID )
	{
        cost = _data.tokenCost;
        cooldown = _data.tokenCooldown;
        type = _data.type;
        performingEntityID = _performingEntityID;
        supposedPositionAtActionStartID = _positionAtActionStartID;
        positionAtActionEndID = _positionAtActionStartID;
    }

    public abstract void Prepare ( Entity.EntityState _state );

    public virtual void Perform (Entity.EntityState _state)
	{
        GameManager.Instance.GetEntityFromID(performingEntityID).StartPerformAction(this);
        onPerform?.Invoke();
    }

    public abstract bool TileInteractPredicate ( Tile _tile );

    public virtual void RegisterInteraction ( Tile _tile )
	{
        TurnManager.Instance.AddAction(performingEntityID, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.CurrentStateTypeSelected);

        TurnManager.Instance.RefreshActionDisplay(performingEntityID);
    }

    public virtual void EndPerform ()
    {
        GameManager.Instance.GetEntityFromID(performingEntityID).EndPerformAction();
        onEndPerform?.Invoke(performingEntityID);
    }

    public abstract bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true );

    public abstract void Display ();

	public override string ToString ()
	{
		return GameManager.Instance.GetEntityFromID(performingEntityID).Data.name + "," + type.ToString();
	}
}
