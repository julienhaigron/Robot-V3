using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MoveToTargetAction : AMovementAction
{
	public int targetEntiyID;
	public int targetTileID;
	public MoveActionMode mode;

	public int finalTargetTileID = -1; //-1 means its canceled
	public enum MoveActionMode { Coordinate, Entity }

	private readonly HashSet<int> m_tilesTakenAtEndOfTurn = new();

	protected override bool HasPath => base.HasPath && finalTargetTileID != -1;

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
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (HasPath)
			PerformingEntity.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: true);
	}

	protected override IEnumerator PerformCR ()
	{
		int movementAmount = targetTileIDs.Length;
		float stepDuration = TickTimeLeft / movementAmount;

		for (int i = 0; i < movementAmount; i++)
		{
			if (GridManager.Instance.Tiles[targetTileIDs[i]].IsObstacle(true) && !GridManager.CanPhaseThrough(PerformingEntity))
			{
				ReleaseBookedTiles();
				PerformingEntity.Displacement.RegisterOnCurrentTile();
				break;
			}

			m_movementTween = PerformingEntity.Displacement.MoveToTile(targetTileIDs[i], null, true, stepDuration);

			if (m_movementTween == null)
				break;

			yield return new WaitForSeconds(stepDuration);
		}

		EndTick();
	}

	public override void OnSelectActionTileInteractPredicatePrewarm ()
	{
		base.OnSelectActionTileInteractPredicatePrewarm();
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
		int distance = _tile.Distance;
		return !_tile.IsObstacle(true) && distance >= 1 && distance <= MaxMovementDistance && !m_tilesTakenAtEndOfTurn.Contains(_tile.coordinates.ID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetPlannedMoveStart(this)];
		List<Tile> path = GridManager.Instance.GetPath(from, _tile, true, _movingEntity: PerformingEntity, _canTraverseAllies: true);

		if (path == null)
			return;

		path.Reverse();
		int actionCount = 0;
		for (int i = 0; i < path.Count - 1;)
		{
			MoveToTargetAction action = new MoveToTargetAction();

			if (mode == MoveActionMode.Coordinate)
				action.targetTileID = _tile.coordinates.ID;
			else if (mode == MoveActionMode.Entity)
				action.targetEntiyID = _tile.GetEntity(true).ID;
			action.mode = mode;

			int stepEnd = GetStepEnd(path, i, Data.movementSpeed, true);

			List<int> tileIDList = new();
			for (int j = i + 1; j <= stepEnd; j++)
				tileIDList.Add(path[j].coordinates.ID);
			action.targetTileIDs = tileIDList.ToArray();
			action.Init(Data, linkedEquipmentId, performingEntityID, path[i].coordinates.ID, timeAtStart + (actionCount * Data.tokenDuration));

			if (_tile.TryGetPlannedItemAt(timeAtStart + i, out Item _item))
				_item.Data.OnRegisterInteraction(action, _item);

			actionCount++;
			TurnManager.Instance.RegisterAction(performingEntityID, action, TurnManager.Instance.GetDefaultStateOf(performingEntityID));
			i = stepEnd;

			if (TurnManager.Instance.RemainingActionToken[performingEntityID] < Data.tokenDuration)
				break;
		}

		TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (finalTargetTileID == -1 || targetTileIDs == null)
			return StayOnCurrentTile();

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;

		if (IsDestinationOccupiedOnNextTurnAction())
		{
			if (!_isCheck)
				RefreshDestinatedTile();
			doesSelfHaveConflict = _isCheck || finalTargetTileID == -1;
		}
		else if (!IsFirstStepReachable())
		{
			doesSelfHaveConflict = true;
			RefreshDestinatedTile();
		}
		else if (_otherAction is MoveToTargetAction otherMove && (otherMove.TargetsAnyOf(targetTileIDs) || IsSwappingTilesWith(otherMove)))
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

	private bool IsFirstStepReachable ()
	{
		Tile from = PerformingEntity.Displacement.Coordinates.GetTile();
		Tile firstStep = GridManager.Instance.Tiles[targetTileIDs[0]];
		return from.coordinates.DistanceTo(firstStep.coordinates) == 1 && (!firstStep.IsObstacle(false) || GridManager.CanPhaseThrough(PerformingEntity));
	}

	public static int GetStepEnd ( List<Tile> _path, int _from, int _movementSpeed, bool _isThisTurn )
	{
		int stepEnd = Mathf.Min(_from + _movementSpeed, _path.Count - 1);
		while (stepEnd < _path.Count - 1 && _path[stepEnd].IsObstacle(_isThisTurn))
			stepEnd++;

		return stepEnd;
	}

	private bool IsSwappingTilesWith ( MoveToTargetAction _otherAction )
	{
		if (targetTileIDs == null || _otherAction.targetTileIDs == null)
			return false;

		int myTileID = PerformingEntity.Displacement.Coordinates.ID;
		int otherTileID = _otherAction.PerformingEntity.Displacement.Coordinates.ID;

		return System.Array.IndexOf(targetTileIDs, otherTileID) >= 0 && System.Array.IndexOf(_otherAction.targetTileIDs, myTileID) >= 0;
	}

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTileID == -1)
			return;

		ReleaseBookedTiles();

		Tile from = PerformingEntity.Displacement.Coordinates.GetTile();
		List<Tile> pathToTile = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[finalTargetTileID],
			_isThisTurn: false, _movingEntity: PerformingEntity, _canTraverseAllies: false, _canEndOnOccupiedTile: false);

		if (pathToTile == null || pathToTile.Count < 2)
		{
			finalTargetTileID = -1;
			positionAtActionEndID = from.coordinates.ID;
			return;
		}

		pathToTile.Reverse();

		int stepCount = GetStepEnd(pathToTile, 0, Data.movementSpeed, false);
		targetTileIDs = new int[stepCount];
		for (int i = 0; i < stepCount; i++)
			targetTileIDs[i] = pathToTile[i + 1].coordinates.ID;
		positionAtActionEndID = targetTileIDs[^1];
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (positionAtActionEndID == -1)
			return;

		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetPlannedMoveStart(this)];
		List<Tile> path = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[positionAtActionEndID], true, false, PerformingEntity, _canTraverseAllies: true);
		if (path == null)
			return;

		path.Reverse();
		ActionDisplayOnTile.ShowGhost(this, path, Data.movementSpeed, _state);
	}
}
