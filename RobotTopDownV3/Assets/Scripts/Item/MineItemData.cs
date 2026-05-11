using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MineData", menuName = "ScriptableObject/Item/Mine")]
public class MineItemData : AItemData
{
	[SerializeField] public int range = 1;
	[SerializeField] public SerializableDictionary<WeaponEquipmentData.DamageType, int> damages;

	public override AItemLinkedData GetNewLinkedData ()
	{
		return new MineItemLinkedData();
	}

	/*public override void OnInvokeItem ( Tool _invokingTool, Item _item )
	{
		base.OnInvokeItem(_invokingTool, _item);

        if (_item.LinkedData is not PortalItemLinkedData portalLinkedData)
            return;

        if (portalLinkedData.portalATile == null)
            portalLinkedData.portalATile = _item.CurrentPosition;
        else
            portalLinkedData.portalBTile = _item.CurrentPosition;

    }*/

	public override bool CanWalkThroughPredicate ( AItemLinkedData _linkedData, Item _usedItem, bool _isThisTurn )
	{
		return true;
	}

	public override void OnWalkThrough ( Entity _walkingEntity, AItemLinkedData _linkedData, Item _usedItem, bool _isFromTeleportation )
	{
		List<Entity> entities = GridManager.Instance.GetEntitiesInRange(_usedItem.CurrentPosition, range, false);

		foreach (Entity entity in entities)
		{
			entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
			{
				critical = false,
				damages = damages,
				entityAttacker = null,
				entityTargeted = entity,
				hitNormal = Vector3.zero,
				hitPos = Vector3.zero
			});
		}

		//TODO : explosion anim
		/*TurnManager.InPlayEvent explosionEvent = new();
		TurnManager.Instance.AddGameEvent(explosionEvent);
		explosionEvent.EndEvent();*/
	}

	public override bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem )
	{
		return false;
	}

	public override void Interract ( Entity _enteringEntity, AItemLinkedData _linkedData, Item _usedItem, Action _onEndUse )
	{

	}

	/*public override void OnRegisterInteraction ( AEntityAction _action, Item _itemOnTile )
	{
		if (_itemOnTile.CurrentPosition.coordinates.ID != _action.positionAtActionEndID || _itemOnTile.LinkedData is not PortalItemLinkedData portalData || portalData.portalATile == null || portalData.portalBTile == null)
			return;
		_action.positionAtActionEndID = _itemOnTile.CurrentPosition == portalData.portalATile ? portalData.portalBTile.coordinates.ID : portalData.portalATile.coordinates.ID;

		base.OnRegisterInteraction(_action, _itemOnTile);
	}*/

}

[Serializable]
public class MineItemLinkedData : AItemLinkedData
{

}
