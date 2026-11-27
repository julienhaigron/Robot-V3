using System.Collections.Generic;
using Unity.Netcode;

public class EscortAction : MoveToTargetAction
{
	public int escortedID = -1;

	private Entity Escorted => escortedID >= 0 ? GameManager.Instance.GetEntityFromID(escortedID) : null;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref escortedID);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		Entity ally = _tile.GetEntity(true);
		return ally != null && ally.ID != performingEntityID && !ally.Equipment.IsDead && ally.IsAlliedTo(PerformingEntity.OwnerID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		Entity ally = _tile.GetEntity(true);
		if (ally == null || TurnManager.Instance.RemainingActionToken[performingEntityID] < Data.GetTokenTotalCost(this, PerformingEntity, null))
			return;

		escortedID = ally.ID;
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetPlannedMoveStart(this)];
		Tile allyTile = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(ally.ID)];
		PlanPathToward(from, GetEscortTile(from, allyTile, true));
		Init(Data, linkedEquipmentId, performingEntityID, from.coordinates.ID, timeAtStart);

		TurnManager.Instance.RegisterAction(performingEntityID, this, TurnManager.Instance.GetDefaultStateOf(performingEntityID));
		TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
	}

	public override void ConflictCheckPrewarm ()
	{
		base.ConflictCheckPrewarm();

		Entity escorted = Escorted;
		if (escorted == null || escorted.Equipment.IsDead)
			return;

		Tile from = PerformingEntity.Displacement.Coordinates.GetTile();
		Tile allyTile = GridManager.Instance.Tiles[TurnManager.Instance.GetEntityPositionAtEndOfTick(escortedID, escorted.Displacement.Coordinates.ID)];
		Tile destination = GetEscortTile(from, allyTile, false) ?? from;
		targetTileID = finalTargetTileID = destination.coordinates.ID;
		RefreshDestinatedTile();
	}

	private void PlanPathToward ( Tile _from, Tile _destination )
	{
		List<Tile> path = _destination == null || _destination == _from ? null
			: GridManager.Instance.GetPath(_from, _destination, true, _movingEntity: PerformingEntity, _canTraverseAllies: true, _canEndOnOccupiedTile: false);

		if (path == null || path.Count < 2)
		{
			targetTileIDs = null;
			targetTileID = finalTargetTileID = _from.coordinates.ID;
			return;
		}

		path.Reverse();
		int stepEnd = GetStepEnd(path, 0, Data.movementSpeed, true);
		targetTileIDs = new int[stepEnd];
		for (int i = 0; i < stepEnd; i++)
			targetTileIDs[i] = path[i + 1].coordinates.ID;

		targetTileID = finalTargetTileID = _destination.coordinates.ID;
	}

	private Tile GetEscortTile ( Tile _from, Tile _allyTile, bool _isThisTurn )
	{
		if (_from.coordinates.DistanceTo(_allyTile.coordinates) == 1)
			return _from;

		Tile best = null;
		int bestDistance = int.MaxValue;
		foreach (Tile neighbor in _allyTile.Neighbors)
		{
			if (neighbor == null || neighbor.IsObstacle(_isThisTurn) || (neighbor.GetEntity(_isThisTurn) is Entity occupant && occupant != PerformingEntity))
				continue;

			int distance = _from.coordinates.DistanceTo(neighbor.coordinates);
			if (distance >= bestDistance)
				continue;

			best = neighbor;
			bestDistance = distance;
		}

		return best;
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		PerformingEntity.Escort(Escorted);
		base.Perform(_state);
	}
}
