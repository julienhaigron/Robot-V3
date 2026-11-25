using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cable", menuName = "ScriptableObject/Items/Cable")]
public class CableItemData : AItemData
{
	public Color allyColor = new(0.25f, 0.6f, 1f);
	public Color enemyColor = new(1f, 0.35f, 0.25f);

	private static MaterialPropertyBlock s_propertyBlock;
	private static readonly int s_colorID = Shader.PropertyToID("_Color");

	public override AItemLinkedData GetNewLinkedData ()
	{
		return new CableItemLinkedData();
	}

	public override bool InvokeItemPredicate ( ToolEquipment _invocatingTool, EntityActionData _actionData )
	{
		return _actionData.invocationCountLimit <= 0 || base.InvokeItemPredicate(_invocatingTool, _actionData);
	}

	public override bool CanBePlacedOn ( Tile _tile )
	{
		return !_tile.IsObstacle(true);
	}

	public override void OnInvokeItem ( ToolEquipment _invokingTool, Item _item )
	{
		base.OnInvokeItem(_invokingTool, _item);

		s_propertyBlock ??= new MaterialPropertyBlock();
		Color color = _invokingTool.LinkedEntity.IsAlliedTo(GameManager.Instance.PlayerID) ? allyColor : enemyColor;
		foreach (Renderer renderer in _item.GetComponentsInChildren<Renderer>())
		{
			renderer.GetPropertyBlock(s_propertyBlock);
			s_propertyBlock.SetColor(s_colorID, color);
			renderer.SetPropertyBlock(s_propertyBlock);
		}
	}

	public override void OnWalkThrough ( Entity _walkingEntity, AItemLinkedData _linkedData, Item _usedItem, bool _isFromTeleportation )
	{

	}

	public override bool InteractPredicate ( Entity _interactingEntity, AItemLinkedData _linkedData, Item _usedItem )
	{
		return false;
	}

	public static List<Tile> GetPlacedCableTiles ( int _ownerID )
	{
		List<Tile> tiles = new();
		foreach (Item item in GameManager.Instance.Items)
		{
			if (item != null && item.IsPlaced && item.Data is CableItemData && item.Invocator != null && item.Invocator.OwnerID == _ownerID)
				tiles.Add(item.CurrentPosition);
		}

		return tiles;
	}
}

[System.Serializable]
public class CableItemLinkedData : AItemLinkedData
{

}
