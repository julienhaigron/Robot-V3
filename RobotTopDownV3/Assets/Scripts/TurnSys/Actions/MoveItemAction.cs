using UnityEngine;

public class MoveItemAction : SpecialAction
{
	private int m_movableItemCheckFrame = -1;
	private bool m_hasMovableItem;

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return Data.invocatedItem != null && _tile.GetItem(true) == null && _tile.GetPlannedItemAt(timeAtStart) == null
			&& Data.invocatedItem.CanBePlacedOn(_tile) && HasMovableItem() && base.TileInteractPredicate(_tile);
	}

	private bool HasMovableItem ()
	{
		if (m_movableItemCheckFrame == Time.frameCount)
			return m_hasMovableItem;

		m_movableItemCheckFrame = Time.frameCount;
		m_hasMovableItem = false;
		Entity user = PerformingEntity;
		foreach (Item item in GameManager.Instance.Items)
		{
			if (IsMovable(item, user, false))
			{
				m_hasMovableItem = true;
				break;
			}
		}

		return m_hasMovableItem;
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
		Item closestItem = null;
		int closestDistance = int.MaxValue;
		foreach (Item item in GameManager.Instance.Items)
		{
			if (!IsMovable(item, user, _placedOnly))
				continue;

			int distance = item.CurrentPosition.coordinates.DistanceTo(_destination.coordinates);
			if (distance < closestDistance)
			{
				closestItem = item;
				closestDistance = distance;
			}
		}

		return closestItem;
	}

	private bool IsMovable ( Item _item, Entity _user, bool _placedOnly )
	{
		return _item != null && _item.Data == Data.invocatedItem && _item.Invocator == _user && _item.CurrentPosition != null && (_item.IsPlaced || !_placedOnly);
	}
}
