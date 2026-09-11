using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using System.Linq;
using DG.Tweening;

public class JumpToTarget : AEntityAction
{
	private Coroutine m_performCR;
	private Tween m_movementTween;

	public override void Init ( EntityActionData _data, string _linkedEquipmentID, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
		base.Init(_data, _linkedEquipmentID, _performingEntityID, _positionAtActionStartID, _timeAtStart);
		positionAtActionEndID = targetTileIDs == null || targetTileIDs .Length == 0 ? _positionAtActionStartID : targetTileIDs[^1];
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (IsDestinationOccupiedOnNextTurnAction())
			CancelPath();

		//Only free the tile when the jump is actually going to happen: clearing it for a cancelled move leaves
		//the entity registered nowhere while its Coordinates still point here, and the next unit walks through.
		//Current tick slot, same convention as MoveToTargetAction, so a follower reads the tile as free at once.
		if (targetTileIDs != null)
			PerformingEntity.Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: true);
	}

	//A leap has no reroute - it is a single jump onto a chosen tile - so a blocked one is cancelled outright.
	private void CancelPath ()
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

	public override bool DoesLeaveTileThisTick ( int _tileID )
	{
		return targetTileIDs != null && targetTileIDs.Length > 0 && targetTileIDs[^1] != _tileID;
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
		if (targetTileIDs != null && targetTileIDs.Length > 0)
		{
			m_performCR = GameManager.Instance.StartCoroutine(PerformCR());
		}
		else
		{
			m_movementTween = DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () =>
			{
				EndTick();
			});
		}
	}

	private IEnumerator PerformCR ()
	{
		/*Tile from = GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile();
		Tile to = GridManager.Instance.Tiles[thisActionDestinationIDArray[];
		List<Tile> path = GridManager.Instance.GetPath(from, to, false);*/
		int movementAmount = 1;
		float movementSpeed = GameConfig.current.game.actionDuration / movementAmount;

		for (int i = 0; i < movementAmount; i++)
		{
			/*List<Tile> tilesInRange = new();
			foreach (string weaponId in PerformingEntity.Equipment.Weapons.Keys)
				tilesInRange.AddRange(PerformingEntity.Equipment.GetTilesInWeaponRange(this, true));*/
/*
			foreach (Tile tile in tilesInRange)
			{
				tile.UI.SetOutlineColor(Color.blue);
			}*/
			m_movementTween = PerformingEntity.Displacement.MoveToTile(targetTileIDs[i], null, true, movementSpeed);

			if (m_movementTween == null)
				break;

			yield return new WaitForSeconds(movementSpeed);
			/*foreach (Tile tile in tilesInRange)
			{
				tile.UI.ResetOutline();
			}*/
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
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		int maxDistance = TurnManager.Instance.RemainingActionToken[performingEntityID] * Data.movementSpeed;
		int distance = _tile.Distance;

		if (_tile.IsObstacle(true) || distance != maxDistance)
			return false;

		return true;
	}

	//CheckConflict is null safe: every _otherAction use is a pattern match, which fails on null.
	public override bool ConflictCheckAlone ( bool _isCheck = true )
	{
		return CheckConflict(null, _isCheck).isFirstActionConflicted;
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		if (targetTileIDs == null || targetTileIDs.Length == 0)
		{
			//entity move action canceled
			performingEntity.Displacement.Coordinates.GetTile().SetEntity(performingEntity, _isThisTurn: true);
			return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
		}

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;

		//Both branches cancel on the resolve pass: with no reroute to offer, leaving the flag up forever ends
		//in "This action conflict cannot be resolved", which aborts the whole tick. The early return at the
		//top of this method then reports the cancelled jump as unconflicted on the next iteration.
		if (IsDestinationOccupiedOnNextTurnAction())
		{
			doesSelfHaveConflict = true;
			if (!_isCheck)
				CancelPath();
		}
		else if (GridManager.Instance.GetDistanceBetween(PerformingEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[targetTileIDs[0]], Data.movementSpeed, false) != Data.movementSpeed)
		{
			//check if tile too far
			doesSelfHaveConflict = true;
			if (!_isCheck)
				CancelPath();
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
		else if (_otherAction is MoveToTargetAction _otherMoveToTargetAction && _otherMoveToTargetAction.targetTileIDs != null && _otherMoveToTargetAction.targetTileIDs.Any(tileID => targetTileIDs.Contains(tileID)))
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
				CancelPath();
			}
		}

		if (doesSelfHaveConflict == false)
		{
			foreach (int tileID in targetTileIDs)
				GridManager.Instance.Tiles[tileID].SetEntity(performingEntity, _isThisTurn: false);
		}

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
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

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		Vector3 previousPosition = GridManager.Instance.Tiles[supposedPositionAtActionStartID].transform.position;
		bool didBeginning = false;
		foreach (int tileID in targetTileIDs)
		{
			ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
			Vector3 startPos = previousPosition;
			Vector3 destination = GridManager.Instance.Tiles[tileID].transform.position;
			Vector3 position = Vector3.Lerp(startPos, destination, .5f);
			previousPosition = destination;
			arrow.Init(_recordedAction, !didBeginning);
			arrow.transform.position = position;
			arrow.transform.LookAt(GridManager.Instance.Tiles[tileID].transform);

			didBeginning = true;
			PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, false);
		}
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

		for (int i = 0; i < path.Count - 1; i++)
		{
			Tile thisTile = path[i];
			Tile otherTile = path[i+1];
			ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
			Vector3 startPos = thisTile.transform.position;
			Vector3 destination = otherTile.transform.position;
			Vector3 position = Vector3.Lerp(startPos, destination, .5f);
			arrow.SetMaterial(GameAssets.current.ui.ghostEntityStateMaterials[_state]);
			arrow.SetBeginningGoVisibility(i % Data.movementSpeed == 0);
			arrow.transform.position = position;
			arrow.transform.LookAt(otherTile.transform);

			PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, true);
		}
	}
}
