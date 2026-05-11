using UnityEngine;
using Sirenix.OdinInspector;
using System;

[Serializable]
public abstract class AItemData : ScriptableObject
{
    public Item itemPrefab;

    public abstract AItemLinkedData GetNewLinkedData ();

    public virtual bool InvokeItemPredicate ( Tool _invocatingTool, EntityActionData _actionData )
	{
        return !_invocatingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary.ContainsKey(_invocatingTool.ID)
            || _invocatingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary[_invocatingTool.ID].currentInvocationCount < _actionData.invocationCountLimit;
    }

    public virtual void OnInvokeItem ( Tool _invokingTool, Item _item )
	{
        _invokingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary[_invokingTool.ID].currentInvocationCount++;
    }

    public abstract bool CanWalkThroughPredicate ( AItemLinkedData _linkedData, Item _usedItem, bool _isThisTurn );

    public abstract void OnWalkThrough ( Entity _walkingEntityn, AItemLinkedData _linkedData, Item _usedItem, bool _isFromTeleportation );

    public abstract bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem );

    public virtual void Interract ( Entity _user, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse)
    {
        _onEndUse?.Invoke();
    }

    public virtual void OnRegisterInteraction(AEntityAction _action, Item _itemOnTile )
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
}