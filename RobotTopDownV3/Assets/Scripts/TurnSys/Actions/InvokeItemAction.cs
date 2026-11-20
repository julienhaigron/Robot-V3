using Unity.Netcode;
using UnityEngine;

public class InvokeItemAction : SpecialAction
{
	public int itemID = -1;

	protected override bool ClaimsTargetTiles => true;
	protected override bool DoesOwnItsCompletion => true;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref itemID);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return Data.invocatedItem.InvokeItemPredicate(PerformingEntity.Equipment.Tools[linkedEquipmentId], Data) && _tile.GetItem(true) == null && _tile.GetPlannedItemAt(timeAtStart) == null
			&& Data.invocatedItem.CanBePlacedOn(_tile) && base.TileInteractPredicate(_tile);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		PlanItemOn(_tile);
		base.RegisterInteraction(_tile);
	}

	public void PlaceItemOn ( Tile _tile )
	{
		PlanItemOn(_tile);
		targetTileIDs = new int[] { _tile.coordinates.ID };
	}

	private void PlanItemOn ( Tile _tile )
	{
		Item invokedItem = GameManager.Instance.PreSpawnItem(Data.invocatedItem, PerformingEntity, PerformingEntity.Equipment.Tools[linkedEquipmentId], _tile.coordinates);
		_tile.SetPlannedItemAt(invokedItem, timeAtStart);
		itemID = invokedItem.ID;
	}

	public override void CancelAction ()
	{
		ReleasePlannedInvocation();

		foreach (int tileID in targetTileIDs ?? System.Array.Empty<int>())
			GridManager.Instance.Tiles[tileID].SetPlannedItemAt(null, timeAtStart);

		base.CancelAction();
	}

	private void ReleasePlannedInvocation ()
	{
		if (itemID < 0 || GameManager.Instance == null)
			return;

		Item item = GameManager.Instance.GetItemFromID(itemID);
		itemID = -1;
		if (item == null || item.IsPlaced || item.LinkedData == null)
			return;

		item.LinkedData.OnPlannedItemCanceled();
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if (isActionCanceled)
		{
			CancelAction();
			EndTick();
			return;
		}

		foreach (int tileID in targetTileIDs)
		{
			Tile targetTile = GridManager.Instance.Tiles[tileID];
			Item item = targetTile.GetPlannedItemAt(timeAtStart);
			targetTile.SetItem(item, true);
			item.transform.position = targetTile.transform.position;
			item.SetPlaced();

			if (PerformingEntity.IsAlliedTo(GameManager.Instance.PlayerID) || PerformingEntity.IsVisible)
				Weapon.PlayOrientedVFX(Data.onPerformVFXPool, targetTile.transform.position, Vector3.zero, Data.name);
		}

		base.Perform(_state);
		DG.Tweening.DOVirtual.DelayedCall(TickTimeLeft, EndTick);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ShowPlannedItems();
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		ShowPlannedItems();
	}

	private void ShowPlannedItems ()
	{
		foreach (int tileID in targetTileIDs ?? System.Array.Empty<int>())
			PlayerController.Instance.AddGhostItemAt(Data.invocatedItem, GridManager.Instance.Tiles[tileID], 0, itemID);
	}
}
