using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpToTarget : AMovementAction
{
	public override void Prepare ( Entity.EntityState _state )
	{
		if (IsDestinationOccupiedOnNextTurnAction())
			DropPath();

		if (targetTileIDs != null)
			PerformingEntity.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: true);
	}

	protected override IEnumerator PerformCR ()
	{
		float jumpDuration = TickTimeLeft;
		bool isVisible = PerformingEntity.IsAlliedTo(GameManager.Instance.PlayerID) || PerformingEntity.IsVisible;
		Tile departure = PerformingEntity.Displacement.Coordinates.GetTile();
		Tile landing = GridManager.Instance.Tiles[targetTileIDs[0]];
		if (isVisible)
			Weapon.PlayOrientedVFX(Data.onPerformVFXPool, departure.transform.position, Vector3.zero, Data.name);
		AttachMovementTrail();

		m_movementTween = PerformingEntity.Displacement.MoveToTile(landing.coordinates.ID, null, true, jumpDuration);
		if (m_movementTween != null)
			yield return new WaitForSeconds(jumpDuration);

		DetachMovementTrail();
		if (isVisible && m_movementTween != null)
			Weapon.PlayOrientedVFX(Data.onSingleAttackHitVFXPool, landing.transform.position, Vector3.zero, Data.name);

		EndTick();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return !_tile.IsObstacle(true) && _tile.Distance == MaxMovementDistance;
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (targetTileIDs == null || targetTileIDs.Length == 0)
			return StayOnCurrentTile();

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;

		if (IsDestinationOccupiedOnNextTurnAction()
			|| GridManager.Instance.GetDistanceBetween(PerformingEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[targetTileIDs[0]], Data.movementSpeed, false) != Data.movementSpeed)
		{
			doesSelfHaveConflict = true;
			if (!_isCheck)
				DropPath();
		}
		else if (_otherAction is MoveToTargetAction otherMove && otherMove.TargetsAnyOf(targetTileIDs))
		{
			if (WinsTieBreak())
			{
				otherMove.DropPath();
				doesOtherHaveConflict = true;
			}
			else
			{
				doesSelfHaveConflict = true;
				DropPath();
			}
		}

		if (!doesSelfHaveConflict)
			BookTargetTiles();

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (positionAtActionEndID == -1)
			return;

		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> path = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[positionAtActionEndID], true, false);
		if (path == null)
			return;

		path.Reverse();
		ActionDisplayOnTile.ShowGhost(this, path, Data.movementSpeed, _state);
	}
}
