using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class TurnShieldAction : SpecialAction
{
	public int targetedOrientation = 0;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetedOrientation);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		targetedOrientation = GridManager.Instance.GetClosestOrientation(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], _tile);

		base.RegisterInteraction(_tile);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		base.Prepare(_state);

	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?
		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override bool DoesOwnItsCompletion => true;

	protected override void Perform ( Entity.EntityState _state )
	{
		//todo : apply effect
		GameManager.Instance.GetEntityFromID(performingEntityID).Equipment.Tools[linkedEquipmentId].PerformAction(this, EndTick);
		base.Perform(_state);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		if (TryGetShield(out Shield shield))
			ShieldMarker.ShowPlanned(shield, GridManager.Instance.Tiles[supposedPositionAtActionStartID]
				, TurnManager.Instance.GetPlannedShieldOrientation(shield, timeAtStart), targetedOrientation, TimeAtStartPerform + 1, false);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		Tile pivot = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		Tile hovered = PlayerController.Instance.HoveredTile;
		if (hovered == null || !TileInteractPredicate(hovered) || !TryGetShield(out Shield shield))
			return;

		ShieldMarker.ShowPlanned(shield, pivot, TurnManager.Instance.GetPlannedShieldOrientation(shield)
			, GridManager.Instance.GetClosestOrientation(pivot, hovered), TimeAtStartPerform + 1, true);
	}

	private bool TryGetShield ( out Shield _shield )
	{
		_shield = PerformingEntity != null && linkedEquipmentId != null
			&& PerformingEntity.Equipment.Tools.TryGetValue(linkedEquipmentId, out ToolEquipment tool) ? tool as Shield : null;
		return _shield != null;
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return _tile != null && GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)].Neighbors.Contains(_tile);
	}
}
