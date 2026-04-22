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

    }

}

[Serializable]
public class PortalItemLinkedData : AItemLinkedData
{
    public Tile portalATile;
    public Tile portalBTile;
}
