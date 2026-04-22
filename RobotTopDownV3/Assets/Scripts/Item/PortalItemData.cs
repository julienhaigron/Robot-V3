using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CreateAssetMenu(fileName = "PortalData", menuName = "ScriptableObjects/Item/Portal")]
public class PortalItemData : AItemData<PortalItemLinkedData>
{

	public override PortalItemLinkedData GetNewLinkedData ()
	{
        return new();
	}

    /*public override bool InvokeItemPredicate ( Tool _invocatingTool, EntityActionData _actionData )
    {
        return _invocatingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary[_invocatingTool.] && base.InvokeItemPredicate(_invocatingTool, _actionData);
    }*/

    public override void OnInvokeItem ( Tool _invokingTool )
    {
        _invokingTool.LinkedEntity.Equipment.ItemsLinkedDataDictionary[_invokingTool.ID].currentInvocationCount++;
    }

    public override bool CanWalkThroughPredicate (AItemLinkedData _linkedData, Item _usedItem, bool _isThisTurn )
	{
        if (_linkedData is not PortalItemLinkedData portalData)
            return false;

        Tile destination = _usedItem.CurrentPosition == portalData.portalATile ? portalData.portalATile : portalData.portalBTile;
        return portalData.portalATile != null && portalData.portalBTile != null
            && (_isThisTurn && destination.currentContent.entity == null || !_isThisTurn && destination.nextTurnActionContent.entity == null);
    }

    public override void OnWalkThrough ( Entity _walkingEntityn, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse )
	{
        if (_linkedData is not PortalItemLinkedData portalLinkedData)
        {
            _onEndUse?.Invoke();
            return;
        }

        _walkingEntityn.Displacement.MoveToTile(_usedItem.CurrentPosition == portalLinkedData.portalATile
            ? portalLinkedData.portalATile.coordinates.ID : portalLinkedData.portalBTile.coordinates.ID, _onEndUse, true, 0);
    }

    public override bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem )
	{
        return false;
    }

    public override void Interract ( Entity _enteringEntity, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse )
    {
        /*if (_linkedData is not PortalItemLinkedData portalLinkedData)
		{
            _onEndUse?.Invoke();
            return;
		}

        _enteringEntity.Displacement.MoveToTile(_usedItem.CurrentPosition == portalLinkedData.portalATile
            ? portalLinkedData.portalATile.coordinates.ID : portalLinkedData.portalBTile.coordinates.ID, _onEndUse, true, 0);*/

        /*_enteringEntity.transform.position = _usedItem.CurrentPosition == portalLinkedData.portalATile 
            ? portalLinkedData.portalATile.transform.position : portalLinkedData.portalBTile.transform.position;*/


        base.Interract(_enteringEntity, _linkedData, _usedItem, _onEndUse);
    }

}

[Serializable]
public class PortalItemLinkedData : AItemLinkedData
{
    public Tile portalATile;
    public Tile portalBTile;
}
