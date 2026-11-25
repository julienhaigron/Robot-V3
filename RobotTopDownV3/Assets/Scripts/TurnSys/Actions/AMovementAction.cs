using System.Collections;
using DG.Tweening;
using UnityEngine;

public abstract class AMovementAction : AEntityAction
{
	protected Coroutine m_performCR;
	protected Tween m_movementTween;

	protected virtual bool HasPath => targetTileIDs != null && targetTileIDs.Length > 0;
	protected int MaxMovementDistance => TurnManager.Instance.RemainingActionToken[performingEntityID] * Data.movementSpeed;

	public override void Init ( EntityActionData _data, string _linkedEquipmentID, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
		base.Init(_data, _linkedEquipmentID, _performingEntityID, _positionAtActionStartID, _timeAtStart);
		positionAtActionEndID = targetTileIDs == null || targetTileIDs.Length == 0 ? _positionAtActionStartID : targetTileIDs[^1];
	}

	public override void OnSelectActionTileInteractPredicatePrewarm ()
	{
		base.OnSelectActionTileInteractPredicatePrewarm();
		GridManager.Instance.BFS(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], MaxMovementDistance, null, true, _movingEntity: PerformingEntity);
	}

	public override bool DoesLeaveTileThisTick ( int _tileID )
	{
		return HasPath && targetTileIDs[^1] != _tileID;
	}

	public override bool ConflictCheckAlone ( bool _isCheck = true )
	{
		return CheckConflict(null, _isCheck).isFirstActionConflicted;
	}

	public override void CancelAction ()
	{
		base.CancelAction();

		PerformingEntity.Displacement.RegisterOnCurrentTile();
		ReleaseBookedTiles();

		if (!m_isPerforming)
			return;

		if (m_performCR != null)
			GameManager.Instance.StopCoroutine(m_performCR);
		PerformingEntity.Displacement.CancelMovement(m_movementTween);
		DetachMovementTrail();

		EndTick();
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		if (HasPath)
			m_performCR = GameManager.Instance.StartCoroutine(PerformCR());
		else
			m_movementTween = DOVirtual.DelayedCall(TickTimeLeft, EndTick);
	}

	protected abstract IEnumerator PerformCR ();

	public void DropPath ()
	{
		ReleaseBookedTiles();
		targetTileIDs = null;
		positionAtActionEndID = PerformingEntity.Displacement.Coordinates.ID;
	}

	public void ReleaseBookedTiles ()
	{
		if (targetTileIDs == null)
			return;

		int currentTileID = PerformingEntity.Displacement.Coordinates.ID;
		foreach (int tileID in targetTileIDs)
		{
			if (currentTileID == tileID)
				continue;
			if (GridManager.Instance.Tiles[tileID].TryGetEntity(false, out Entity bookedEntity) && bookedEntity.ID == performingEntityID)
				GridManager.Instance.Tiles[tileID].SetEntity(null, _isThisTurn: false);
		}
	}

	protected void BookTargetTiles ()
	{
		Entity performer = PerformingEntity;
		foreach (int tileID in targetTileIDs)
			GridManager.Instance.Tiles[tileID].SetEntity(performer, _isThisTurn: false);
	}

	protected ActionConflictResultInfo StayOnCurrentTile ()
	{
		Entity performer = PerformingEntity;
		performer.Displacement.Coordinates.GetTile().SetEntity(performer, _isThisTurn: true);
		return default;
	}

	protected bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (targetTileIDs == null)
			return false;

		Entity performer = PerformingEntity;
		foreach (int tileID in targetTileIDs)
		{
			Tile tile = GridManager.Instance.Tiles[tileID];
			if (tile.GetStayingEntityOtherThan(performer) != null || tile.IsObstacle(false))
				return true;
		}

		return false;
	}

	public bool TargetsAnyOf ( int[] _tileIDs )
	{
		if (targetTileIDs == null || _tileIDs == null)
			return false;

		foreach (int tileID in targetTileIDs)
		{
			if (System.Array.IndexOf(_tileIDs, tileID) >= 0)
				return true;
		}

		return false;
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ActionDisplayOnTile.ShowRecorded(_recordedAction, targetTileIDs);
	}
}
