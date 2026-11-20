using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using System.Linq;
using DG.Tweening;

public class MoveToTargetAction : AEntityAction
{
	public int targetEntiyID;
	public int targetTileID;
	public MoveActionMode mode;

	public int finalTargetTileID = -1; //-1 means its canceled
	public enum MoveActionMode { Coordinate, Entity }

	private Coroutine m_performCR;
	private Tween m_movementTween;
	private readonly HashSet<int> m_tilesTakenAtEndOfTurn = new();

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetEntiyID);
		serializer.SerializeValue(ref targetTileID);
		serializer.SerializeValue(ref mode);
		serializer.SerializeValue(ref finalTargetTileID);
	}

	public override void Init ( EntityActionData _data, string _linkedEquipmentID, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
		base.Init(_data, _linkedEquipmentID, _performingEntityID, _positionAtActionStartID, _timeAtStart);

		switch (mode)
		{
			case MoveActionMode.Coordinate:
				finalTargetTileID = targetTileID;
				break;
			case MoveActionMode.Entity:
				finalTargetTileID = GameManager.Instance.GetEntityFromID(targetEntiyID).Displacement.Coordinates.ID;
				break;
		}

		positionAtActionEndID = targetTileIDs == null ? _positionAtActionStartID : targetTileIDs[^1];
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (finalTargetTileID != -1 && targetTileIDs != null && targetTileIDs.Length > 0)
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: true);
	}

	public override void CancelAction ()
	{
		base.CancelAction();

		PerformingEntity.Displacement.RegisterOnCurrentTile();
		ReleaseBookedTiles();

		if (m_isPerforming)
		{
			if (m_performCR != null)
				GameManager.Instance.StopCoroutine(m_performCR);
			if (m_movementTween != null && m_movementTween.IsActive())
				m_movementTween.Kill();

			EndTick();
		}
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (targetTileIDs != null && targetTileIDs.Length > 0 && finalTargetTileID != -1/* && thisActionDestination.GetEntity(false) == null*/)
		{
			m_performCR = GameManager.Instance.StartCoroutine(PerformCR());
		}
		else
		{
			m_movementTween = DOVirtual.DelayedCall(TickTimeLeft, () =>
			{
				EndTick();
			});
		}
	}

	private IEnumerator PerformCR ()
	{
		int movementAmount = Mathf.Min(targetTileIDs.Length, Data.movementSpeed);
		float movementSpeed = TickTimeLeft / movementAmount;

		for (int i = 0; i < movementAmount; i++)
		{
			m_movementTween = GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.MoveToTile(targetTileIDs[i], null, true, movementSpeed);

			if (m_movementTween == null)
				break;

			yield return new WaitForSeconds(movementSpeed);
		}

		EndTick();
	}

	public override void OnSelectActionTileInteractPredicatePrewarm ()
	{
		base.OnSelectActionTileInteractPredicatePrewarm();

		//for all tiles overall distance calculation
		int maxDistance = TurnManager.Instance.RemainingActionToken[performingEntityID] * Data.movementSpeed;
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		//if (GridManager.Instance.LastBFSOriginTile != from && GridManager.Instance.LastBFSMaxDistance >= maxDistance)
		GridManager.Instance.BFS(from, maxDistance, null, true);

		RefreshTilesTakenAtEndOfTurn();
	}

	private void RefreshTilesTakenAtEndOfTurn ()
	{
		m_tilesTakenAtEndOfTurn.Clear();

		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity == null || entity.ID == performingEntityID || entity.Equipment.IsDead)
					continue;

				m_tilesTakenAtEndOfTurn.Add(TurnManager.Instance.GetLastRegisteredPositionOfEntity(entity.ID));
			}
		}
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		int maxDistance = TurnManager.Instance.RemainingActionToken[performingEntityID] * Data.movementSpeed;
		int distance = _tile.Distance;

		if (_tile.IsObstacle(true) || distance > maxDistance || distance < 1)
			return false;

		return !m_tilesTakenAtEndOfTurn.Contains(_tile.coordinates.ID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		//register all action for destination (calcuate dist, and add X actions in TurnSys (X = distance))
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> path = GridManager.Instance.GetPath(from, _tile, true, _movingEntity: PerformingEntity, _canTraverseAllies: true);

		if (path == null)
			return;

		path.Reverse();
		int actionCount = 0;
		for (int i = 0; i < path.Count - 1; i += Data.movementSpeed)
		{
			MoveToTargetAction action = new MoveToTargetAction();
			/*if (i == 0)
				action = this;*/

			if (mode == MoveActionMode.Coordinate)
				action.targetTileID = _tile.coordinates.ID;
			else if (mode == MoveActionMode.Entity)
				action.targetEntiyID = _tile.GetEntity(true).ID;
			action.mode = mode;

			List<int> tileIDList = new();
			for (int j = 0; j < Data.movementSpeed && i+j < path.Count - 1; j++)
				tileIDList.Add(path[i + j + 1].coordinates.ID);
			action.targetTileIDs = tileIDList.ToArray();
			action.Init(GameAssets.current.game.entityActionsData[enumID], linkedEquipmentId, performingEntityID, path[i].coordinates.ID, timeAtStart + (actionCount * Data.tokenDuration));
			//action.actualDuration = Data.movementSpeed;

			if (_tile.TryGetPlannedItemAt(timeAtStart + i, out Item _item))
				_item.Data.OnRegisterInteraction(action, _item);

			actionCount++;
			TurnManager.Instance.RegisterAction(performingEntityID, action, TurnManager.Instance.GetDefaultStateOf(performingEntityID));

			if (TurnManager.Instance.RemainingActionToken[performingEntityID] < Data.tokenDuration)
				break;
		}

		TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		if (finalTargetTileID == -1 || targetTileIDs == null)
		{
			//entity move action canceled
			/*if (performingEntity.Displacement.Coordinates.GetTile().GetEntity(false) != null)
				Debug.LogError("CRITICAL ERROR : performing entity " + performingEntity.Data.name + " cant go back to where it was. Hope this never happens"); // solution? insta kill performing entity
			else
				*/performingEntity.Displacement.Coordinates.GetTile().SetEntity(performingEntity, _isThisTurn: true);
			return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
		}

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;

		if (IsDestinationOccupiedOnNextTurnAction())
		{
			if (_isCheck)
				doesSelfHaveConflict = true;
			else
			{
				RefreshDestinatedTile();
				if (finalTargetTileID == -1)
					doesSelfHaveConflict = true;
			}
		}
		else if (targetTileIDs != null && GridManager.Instance.GetDistanceBetween(PerformingEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[targetTileIDs[0]], Data.movementSpeed, false) > 1)
		{
			//check if tile too far
			doesSelfHaveConflict = true;
			RefreshDestinatedTile();
		}
		/*else if (_otherAction is MoveToNeighborAction _otherNeighborMoveAction && thisActionDestinationIDArray.Contains(_otherNeighborMoveAction.finalTargetTileID))
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				_otherNeighborMoveAction.finalTargetTileID = -1;
				doesOtherHaveConflict = true;
			}
			else
			{
				doesSelfHaveConflict = true;
				thisActionDestinationIDArray = null;
			}
		}*/

		else if (_otherAction is MoveToTargetAction _otherMoveToTargetAction && _otherMoveToTargetAction.targetTileIDs != null
			&& (_otherMoveToTargetAction.targetTileIDs.Any(tileID => targetTileIDs.Contains(tileID)) || IsSwappingTilesWith(_otherMoveToTargetAction)))
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				//performing entity wins roll
				_otherMoveToTargetAction.ReleaseBookedTiles();
				_otherMoveToTargetAction.targetTileIDs = null;
				doesOtherHaveConflict = true;
			}
			else
			{
				doesSelfHaveConflict = true;
				ReleaseBookedTiles();
				targetTileIDs = null;
			}
		}

		if (!doesSelfHaveConflict)
		{
			foreach (int tileID in targetTileIDs)
				GridManager.Instance.Tiles[tileID].SetEntity(performingEntity, _isThisTurn: false);
		}

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	public override bool DoesLeaveTileThisTick ( int _tileID )
	{
		return targetTileIDs != null && targetTileIDs.Length > 0 && finalTargetTileID != -1
			&& targetTileIDs[^1] != _tileID;
	}

	public override bool ConflictCheckAlone ( bool _isCheck = true )
	{
		return CheckConflict(null, _isCheck).isFirstActionConflicted;
	}

	private bool IsSwappingTilesWith ( MoveToTargetAction _otherAction )
	{
		if (targetTileIDs == null || _otherAction.targetTileIDs == null)
			return false;

		int myTileID = PerformingEntity.Displacement.Coordinates.ID;
		int otherTileID = _otherAction.PerformingEntity.Displacement.Coordinates.ID;

		return targetTileIDs.Contains(otherTileID) && _otherAction.targetTileIDs.Contains(myTileID);
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (targetTileIDs == null)
			return false;

		foreach (int tileID in targetTileIDs)
		{
			Tile tile = GridManager.Instance.Tiles[tileID];
			if (tile.GetStayingEntityOtherThan(PerformingEntity) != null || tile.IsObstacle(false))
				return true;
		}

		return false;
	}

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTileID == -1)
			return;

		ReleaseBookedTiles();

		Tile from = PerformingEntity.Displacement.Coordinates.GetTile();
		List<Tile> pathToTile = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[(int)finalTargetTileID],
			_isThisTurn: false, _movingEntity: PerformingEntity, _canTraverseAllies: false, _canEndOnOccupiedTile: false);

		if (pathToTile == null || pathToTile.Count < 2)
		{
			finalTargetTileID = -1;
			positionAtActionEndID = from.coordinates.ID;
			return;
		}

		pathToTile.Reverse();

		int stepCount = Mathf.Min(Data.movementSpeed, pathToTile.Count - 1);
		targetTileIDs = new int[stepCount];
		for (int i = 0; i < stepCount; i++)
		{
			targetTileIDs[i] = pathToTile[i + 1].coordinates.ID;
			positionAtActionEndID = pathToTile[i + 1].coordinates.ID;
		}
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
			if (GridManager.Instance.Tiles[tileID].TryGetEntity(false, out Entity booked) && booked.ID == performingEntityID)
				GridManager.Instance.Tiles[tileID].SetEntity(null, _isThisTurn: false);
		}
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ActionDisplayOnTile.ShowRecorded(_recordedAction, targetTileIDs);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (positionAtActionEndID == -1)
			return;

		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> path = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[positionAtActionEndID], true, false, PerformingEntity, _canTraverseAllies: true);
		if (path == null)
			return;

		path.Reverse();
		ActionDisplayOnTile.ShowGhost(this, path, Data.movementSpeed, _state);
	}
}
