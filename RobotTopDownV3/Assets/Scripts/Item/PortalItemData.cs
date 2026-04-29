using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CreateAssetMenu(fileName = "PortalData", menuName = "ScriptableObject/Item/Portal")]
public class PortalItemData : AItemData
{

	public override AItemLinkedData GetNewLinkedData ()
	{
        return new PortalItemLinkedData();
	}

	public override void OnInvokeItem ( Tool _invokingTool, Item _item )
	{
		base.OnInvokeItem(_invokingTool, _item);

        if (_item.LinkedData is not PortalItemLinkedData portalLinkedData)
            return;

        if (portalLinkedData.portalATile == null)
            portalLinkedData.portalATile = _item.CurrentPosition;
        else
            portalLinkedData.portalBTile = _item.CurrentPosition;

    }

	public override bool CanWalkThroughPredicate (AItemLinkedData _linkedData, Item _usedItem, bool _isThisTurn )
	{
        if (_linkedData is not PortalItemLinkedData portalData)
            return false;

        Tile destination = _usedItem.CurrentPosition == portalData.portalATile ? portalData.portalATile : portalData.portalBTile;
        return portalData.portalATile != null && portalData.portalBTile != null
            && (_isThisTurn && destination.GetEntity(true) == null || !_isThisTurn && destination.GetEntity(false) == null);
    }

    public override void OnWalkThrough ( Entity _walkingEntityn, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse, bool _isFromTeleportation )
	{
        if (_linkedData is not PortalItemLinkedData portalLinkedData)
        {
            _onEndUse?.Invoke();
            return;
        }

        if (_isFromTeleportation)
		{
            //this avoids infinit loop
            return;
		}

        TurnManager.InPlayEvent teleportEvent = new();
        TurnManager.Instance.AddGameEvent(teleportEvent);

        _walkingEntityn.Displacement.TeleportToTile(_usedItem.CurrentPosition == portalLinkedData.portalATile
            ? portalLinkedData.portalBTile.coordinates.ID : portalLinkedData.portalATile.coordinates.ID, teleportEvent.EndEvent);
    }

    public override bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem )
	{
        return false;
    }

    public override void Interract ( Entity _enteringEntity, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse )
    {

    }

	public override void OnRegisterInteraction ( AEntityAction _action, Item _itemOnTile )
	{
        if (_itemOnTile.LinkedData is not PortalItemLinkedData portalData || portalData.portalATile == null || portalData.portalBTile == null)
            return;
        _action.positionAtActionEndID = _itemOnTile.CurrentPosition == portalData.portalATile ? portalData.portalBTile.coordinates.ID : portalData.portalATile.coordinates.ID;

        base.OnRegisterInteraction(_action, _itemOnTile);
	}

}

[Serializable]
public class PortalItemLinkedData : AItemLinkedData
{
    public Tile portalATile;
    public Tile portalBTile;
}
