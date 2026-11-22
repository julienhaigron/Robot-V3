using System;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class MoveThenAttackAction : AttackAction
{
	private const float c_dashTimeShare = .45f;
	public int positionAfterMovementID = -1;

	private Tween m_movementTween;

	private bool HasDash => positionAfterMovementID != -1;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref positionAfterMovementID);
	}

	private static Tile GetDashStopTile ( Tile _target, Tile _from )
	{
		return _target.Neighbors[GridManager.Instance.GetClosestOrientation(_target, _from)];
	}

	public override Tile GetPlannedStrikeTile ( Tile _from, Tile _target )
	{
		Tile stopTile = _target != _from ? GetDashStopTile(_target, _from) : null;
		return stopTile != null ? stopTile : _from;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		positionAfterMovementID = GetDashStopTile(_tile, GridManager.Instance.Tiles[supposedPositionAtActionStartID]).coordinates.ID;

		positionAtActionEndID = positionAfterMovementID;

		base.RegisterInteraction(_tile);
	}

	public override void SetResolvedTargets ( int[] _targetTileIDs, int[] _targetedEntityIDs )
	{
		base.SetResolvedTargets(_targetTileIDs, _targetedEntityIDs);

		if (_targetTileIDs == null || _targetTileIDs.Length == 0)
			return;

		Tile stopTile = GetDashStopTile(GridManager.Instance.Tiles[_targetTileIDs[0]], GridManager.Instance.Tiles[supposedPositionAtActionStartID]);
		if (stopTile == null)
			return;

		positionAfterMovementID = stopTile.coordinates.ID;
		positionAtActionEndID = positionAfterMovementID;
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		base.Prepare(_state);

		if (HasDash)
			PerformingEntity.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: true);
	}

	protected override Tile GetAttackOriginTile ()
	{
		return HasDash ? GridManager.Instance.Tiles[positionAfterMovementID] : base.GetAttackOriginTile();
	}

	public override void CancelAction ()
	{
		base.CancelAction();

		PerformingEntity.Displacement.RegisterOnCurrentTile();
		ReleaseBookedTile();

		if (m_isPerforming)
		{
			PerformingEntity.Displacement.CancelMovement(m_movementTween);
			DetachMovementTrail();

			EndTick();
		}
	}

	public void ReleaseBookedTile ()
	{
		if (!HasDash || positionAfterMovementID == PerformingEntity.Displacement.Coordinates.ID)
			return;

		if (GridManager.Instance.Tiles[positionAfterMovementID].TryGetEntity(false, out Entity bookedEntity) && bookedEntity.ID == performingEntityID)
			GridManager.Instance.Tiles[positionAfterMovementID].SetEntity(null, _isThisTurn: false);
	}

	private void DropDash ()
	{
		ReleaseBookedTile();
		positionAfterMovementID = -1;
		positionAtActionEndID = supposedPositionAtActionStartID;
	}

	public override bool DoesLeaveTileThisTick ( int _tileID )
	{
		return HasDash && positionAfterMovementID != _tileID;
	}

	public override bool ConflictCheckAlone ( bool _isCheck = true )
	{
		return CheckConflict(null, _isCheck).isFirstActionConflicted;
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		Entity performer = PerformingEntity;
		if (!HasDash)
		{
			Tile currentTile = performer.Displacement.Coordinates.GetTile();
			Entity occupant = currentTile.GetEntity(false);
			if (occupant != null && occupant != performer)
				Debug.LogError("CRITICAL ERROR : performing entity " + performer.Data.name + " cant go back to where it was, " + occupant.Data.name + " took it");
			else
				currentTile.SetEntity(performer, _isThisTurn: false);
			return default;
		}

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;

		if (_otherAction is MoveToTargetAction otherMove && otherMove.targetTileIDs != null
			&& (Array.IndexOf(otherMove.targetTileIDs, positionAfterMovementID) >= 0 || IsSwappingTilesWith(otherMove)))
		{
			if (WinsTieBreak())
			{
				otherMove.DropPath();
				doesOtherHaveConflict = true;
			}
			else
			{
				doesSelfHaveConflict = true;
				DropDash();
			}
		}
		else if (IsDestinationOccupiedOnNextTurnAction() || IsDashTooLong())
		{
			doesSelfHaveConflict = true;
			if (!_isCheck)
				DropDash();
		}

		if (!doesSelfHaveConflict)
			GridManager.Instance.Tiles[positionAfterMovementID].SetEntity(performer, _isThisTurn: false);

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	private bool IsDashTooLong ()
	{
		return GridManager.Instance.GetDistanceBetween(GridManager.Instance.Tiles[supposedPositionAtActionStartID], GridManager.Instance.Tiles[positionAfterMovementID], Data.movementSpeed, false)
			> Data.GetMaxRange(this, PerformingEntity, null) - 1;
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		float dashDuration = TickTimeLeft * c_dashTimeShare;
		if (HasDash)
		{
			AttachMovementTrail();
			m_movementTween = PerformingEntity.Displacement.MoveToTile(positionAfterMovementID, () =>
			{
				DetachMovementTrail();
				base.Perform(_state);
			}, true, dashDuration);
		}
		else
			DOVirtual.DelayedCall(dashDuration, () => base.Perform(_state));
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ActionDisplayOnTile.ShowRecorded(_recordedAction, positionAfterMovementID);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (!HasDash)
			return;

		Tile[] tiles = GridManager.Instance.Tiles;
		ActionDisplayOnTile.ShowGhost(this, tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], tiles[positionAfterMovementID], _state);
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		Tile destination = GridManager.Instance.Tiles[positionAfterMovementID];
		return destination.GetBlockingEntityFor(PerformingEntity) != null || destination.IsObstacle(false);
	}

	private bool IsSwappingTilesWith ( MoveToTargetAction _otherAction )
	{
		if (_otherAction.targetTileIDs == null)
			return false;

		return positionAfterMovementID == _otherAction.PerformingEntity.Displacement.Coordinates.ID
			&& Array.IndexOf(_otherAction.targetTileIDs, PerformingEntity.Displacement.Coordinates.ID) >= 0;
	}
}
