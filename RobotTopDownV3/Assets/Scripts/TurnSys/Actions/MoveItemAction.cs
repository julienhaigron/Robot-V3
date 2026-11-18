using System.Linq;

public class MoveItemAction : SpecialAction
{
	public override bool TileInteractPredicate ( Tile _tile )
	{
		return Data.invocatedItem != null && _tile.GetItem(true) == null && _tile.GetPlannedItemAt(timeAtStart) == null
			&& Data.invocatedItem.CanBePlacedOn(_tile) && GetClosestMovableItem(_tile, false) != null && base.TileInteractPredicate(_tile);
	}

	protected override void ApplyActionEffect ()
	{
		Tile destination = GetDestination();
		Item item = destination == null ? null : GetClosestMovableItem(destination, true);
		if (item == null || destination.GetItem(true) != null || !Data.invocatedItem.CanBePlacedOn(destination))
			return;

		Tile origin = item.CurrentPosition;
		origin.SetItem(null, true);
		destination.SetItem(item, true);
		item.MoveTo(destination);
		Data.invocatedItem.OnItemMoved(item, origin, destination);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ShowGhost();
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		ShowGhost();
	}

	private void ShowGhost ()
	{
		Tile destination = GetDestination();
		Item item = destination == null ? null : GetClosestMovableItem(destination, false);
		if (item != null)
			PlayerController.Instance.AddGhostItemAt(Data.invocatedItem, destination, 0, item.ID);
	}

	private Tile GetDestination ()
	{
		return targetTileIDs == null || targetTileIDs.Length == 0 || targetTileIDs[0] < 0 ? null : GridManager.Instance.Tiles[targetTileIDs[0]];
	}

	private Item GetClosestMovableItem ( Tile _destination, bool _placedOnly )
	{
		Entity user = PerformingEntity;
		return GameManager.Instance.Items
			.Where(item => item != null && item.Data == Data.invocatedItem && item.Invocator == user && item.CurrentPosition != null && (item.IsPlaced || !_placedOnly))
			.OrderBy(item => item.CurrentPosition.coordinates.DistanceTo(_destination.coordinates))
			.FirstOrDefault();
	}
}
