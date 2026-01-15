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
    public EntityActionEnumID enumID;
    public int performingEntityID; //entity
    public Entity PerformingEntity => GameManager.Instance.GetEntityFromID(performingEntityID);
    public int supposedPositionAtActionStartID; //tile
    public int positionAtActionEndID; //tile
    public int[] effectsIds;
    public EntityActionData Data => GameAssets.current.game.entityActionsData[enumID];


    public virtual void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref cooldown);
        serializer.SerializeValue(ref enumID);
        serializer.SerializeValue(ref performingEntityID);
		serializer.SerializeValue(ref supposedPositionAtActionStartID);
		serializer.SerializeValue(ref positionAtActionEndID);
		serializer.SerializeValue(ref effectsIds);
    }

    public virtual void Init(EntityActionData _data, int _performingEntityID, int _positionAtActionStartID )
	{
        cost = _data.tokenCost;
        cooldown = _data.tokenCooldown;
        enumID = _data.typeID;
        performingEntityID = _performingEntityID;
        supposedPositionAtActionStartID = _positionAtActionStartID;
        positionAtActionEndID = _positionAtActionStartID;

        effectsIds = new int[_data.appliableEffects.Length];
		for (int i = 0; i < _data.appliableEffects.Length; i++)
		{
            effectsIds[i] = (int)_data.appliableEffects[i].enumId;
        }
    }

    public abstract void Prepare ( Entity.EntityState _state );

    public virtual void Perform (Entity.EntityState _state)
	{
        onPerform?.Invoke();
    }

    public virtual void OnStartPerform ( Entity.EntityState _state )
    {
        PerformingEntity.StartPerformAction(this);
    }

    public abstract bool TileInteractPredicate ( Tile _tile );

    public virtual void RegisterInteraction ( Tile _tile )
	{
        TurnManager.Instance.AddAction(performingEntityID, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.CurrentStateTypeSelected);

        TurnManager.Instance.RefreshActionDisplay(performingEntityID);
    }

    public virtual void EndPerform ()
    {
        PerformingEntity.EndPerformAction();
        onEndPerform?.Invoke(performingEntityID);
    }

    public abstract bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true );

    public abstract void Display ( Entity.EntityState _state );

    public abstract void GhostDisplay ( Entity.EntityState _state );

	public override string ToString ()
	{
		return PerformingEntity.Data.name + "," + enumID.ToString();
	}
}
