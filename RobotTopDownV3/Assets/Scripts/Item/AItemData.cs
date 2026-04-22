using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public abstract class AItemData<TItemLinkedData> : ScriptableObject where TItemLinkedData : AItemLinkedData
{
    public Item itemPrefab;

    public abstract TItemLinkedData GetNewLinkedData ();

    public virtual bool InvokeItemPredicate ( Tool _invocatingTool, EntityActionData _actionData )
	{
        return _invocatingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary[_invocatingTool.ID].currentInvocationCount < _actionData.invocationCountLimit;
    }

    public abstract void OnInvokeItem ( Tool _invokingTool );

    public abstract bool CanWalkThroughPredicate ( AItemLinkedData _linkedData, Item _usedItem, bool _isThisTurn );

    public abstract void OnWalkThrough ( Entity _walkingEntityn, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse );

    public abstract bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem );

    public virtual void Interract ( Entity _user, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse)
    {
        _onEndUse?.Invoke();
    }
}

[Serializable]
public abstract class AItemLinkedData
{
    public int currentInvocationCount = 0;
}