using UnityEngine;
using Sirenix.OdinInspector;
using System;

[Serializable]
public abstract class AItemData : ScriptableObject
{
    public Item itemPrefab;

    [Title("Grid")]
    public bool canWalkThrough = true;
    public bool isTargetable = false;
    [ShowIf(nameof(isTargetable)), Min(1)] public int maxHealth = 10;

    public abstract AItemLinkedData GetNewLinkedData ();

    public virtual bool InvokeItemPredicate ( ToolEquipment _invocatingTool, EntityActionData _actionData )
	{
        return !_invocatingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary.TryGetValue(_invocatingTool.ID, out AItemLinkedData linkedData)
            || linkedData.currentInvocationCount < _actionData.invocationCountLimit;
    }

    public virtual bool CanBePlacedOn ( Tile _tile )
	{
        return true;
	}

    public virtual void OnInvokeItem ( ToolEquipment _invokingTool, Item _item )
	{
        AItemLinkedData linkedData = _invokingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary[_invokingTool.ID];
        linkedData.currentInvocationCount++;
        linkedData.plannedInvocationCount++;
    }

    public virtual bool CanWalkThroughPredicate ( AItemLinkedData _linkedData, Item _usedItem, bool _isThisTurn )
	{
        return canWalkThrough;
	}

    public abstract void OnWalkThrough ( Entity _walkingEntityn, AItemLinkedData _linkedData, Item _usedItem, bool _isFromTeleportation );

    public abstract bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem );

    public virtual void Interract ( Entity _user, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse)
    {
        _onEndUse?.Invoke();
    }

    public virtual void OnRegisterInteraction(AEntityAction _action, Item _itemOnTile )
	{

	}

    public virtual void OnItemMoved ( Item _item, Tile _from, Tile _to )
	{

	}

    public virtual void OnActionTickStart(int _actionTick, AItemLinkedData _linkedData, Item _usedItem )
	{

	}
}

[Serializable]
public abstract class AItemLinkedData
{
    public int currentInvocationCount = 0;
    public int plannedInvocationCount = 0;

    public int PlacedInvocationCount => Mathf.Max(0, currentInvocationCount - plannedInvocationCount);

    public void OnPlannedItemPlaced ()
	{
        plannedInvocationCount = Mathf.Max(0, plannedInvocationCount - 1);
	}

    public void OnPlannedItemCanceled ()
	{
        currentInvocationCount = Mathf.Max(0, currentInvocationCount - 1);
        plannedInvocationCount = Mathf.Max(0, plannedInvocationCount - 1);
	}
}