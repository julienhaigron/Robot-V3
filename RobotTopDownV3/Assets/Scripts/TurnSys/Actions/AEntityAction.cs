using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public abstract class AEntityAction : INetworkSerializable
{
    public Action onPerform;
    public Action<int, bool> onEndTick;

    public EntityActionEnumID enumID;
    public int performingEntityID; //entity
    public Entity PerformingEntity => GameManager.Instance.GetEntityFromID(performingEntityID);
    public int supposedPositionAtActionStartID; //tile
    public int positionAtActionEndID; //tile
    //public int[] statusIds;
    public EntityActionData Data => GameAssets.current.game.entityActionsData[enumID];

    public int preparationDuration = 0;
    public int actualDuration = 1;
    public int cooldownDuration = 0;
    public int TotalCost => preparationDuration + actualDuration + cooldownDuration;

    public int lifetime = 0;
    public int timeAtStart = 0;

    public virtual void NetworkSerialize<T> ( BufferSerializer<T> serializer ) where T : IReaderWriter
    {
        serializer.SerializeValue(ref enumID);
        serializer.SerializeValue(ref performingEntityID);
		serializer.SerializeValue(ref supposedPositionAtActionStartID);
		serializer.SerializeValue(ref positionAtActionEndID);
		//serializer.SerializeValue(ref statusIds);
		serializer.SerializeValue(ref preparationDuration);
		serializer.SerializeValue(ref actualDuration);
		serializer.SerializeValue(ref cooldownDuration);
		serializer.SerializeValue(ref lifetime);
		serializer.SerializeValue(ref timeAtStart);
    }

    public virtual void Init(EntityActionData _data, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
        enumID = _data.enumID;
        performingEntityID = _performingEntityID;
        supposedPositionAtActionStartID = _positionAtActionStartID;
        positionAtActionEndID = _positionAtActionStartID;

        /*statusIds = new int[_data.appliableStatus.Length];
		for (int i = 0; i < _data.appliableStatus.Length; i++)
		{
            statusIds[i] = (int)_data.appliableStatus[i].enumID;
        }*/

        preparationDuration = _data.GetTokenPreparationCost(this, GameManager.Instance.GetEntityFromID(_performingEntityID), null);
        actualDuration = _data.tokenDuration;
        cooldownDuration = _data.GetTokenCooldownCost(this, GameManager.Instance.GetEntityFromID(_performingEntityID), null);

        timeAtStart = _timeAtStart;
    }

    public abstract void Prepare ( Entity.EntityState _state );

    //returns true if action fully performed
    public bool PerformTick ( Entity.EntityState _state )
	{
        lifetime++;
        
        if (lifetime == preparationDuration + 1 )
        {
            Perform(_state);
            return true;
        }
        else
        {
            DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () => EndTick());
            return false;
        }
    }

    protected virtual void Perform (Entity.EntityState _state)
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

    protected virtual void EndTick ()
	{
        bool didEndAction = lifetime >= preparationDuration + actualDuration + cooldownDuration;

        if (didEndAction)
            EndAction();

        onEndTick?.Invoke(performingEntityID, didEndAction);
	}

    protected virtual void EndAction ()
    {
        PerformingEntity.EndPerformAction();
    }

    public abstract bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true );

    public abstract void Display ( TurnManager.RecordedAction _recordedAction );

    public abstract void GhostDisplay ( Entity.EntityState _state );

	public override string ToString ()
	{
		return PerformingEntity.Data.name + "," + enumID.ToString();
	}
}
