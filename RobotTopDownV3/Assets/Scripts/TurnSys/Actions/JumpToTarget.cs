using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using System.Linq;

public class JumpToTarget : AEntityAction
{
	public override void Init ( EntityActionData _data, string _linkedEquipmentID, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
		base.Init(_data, _linkedEquipmentID, _performingEntityID, _positionAtActionStartID, _timeAtStart);
		positionAtActionEndID = targetTileIDs == null || targetTileIDs .Length == 0 ? _positionAtActionStartID : targetTileIDs[^1];
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		//check here if can do movement and where to exactly
		if (IsDestinationOccupiedOnNextTurnAction())
			targetTileIDs = null;
			//RefreshDestinatedTile();

		//Only free the tile when the jump is actually going to happen: clearing it for a cancelled move leaves
		//the entity registered nowhere while its Coordinates still point here, and the next unit walks through.
		if (targetTileIDs != null)
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);
	}

	public override bool DoesLeaveTileThisTick ( int _tileID )
	{
		return targetTileIDs != null && targetTileIDs.Length > 0 && targetTileIDs[^1] != _tileID;
	}

	public override void CancelAction ()
	{
		base.CancelAction();

		//Release the tiles this jump had booked for itself, then put the entity back on its own tile.
		if (targetTileIDs != null)
		{
			int currentTileID = PerformingEntity.Displacement.Coordinates.ID;
			foreach (int tileID in targetTileIDs)
			{
				if (currentTileID == tileID)
					continue;
				if (GridManager.Instance.Tiles[tileID].TryGetEntity(false, out Entity bookedEntity) && bookedEntity.ID == performingEntityID)
					GridManager.Instance.Tiles[tileID].SetEntity(null, _isThisTurn: false);
			}
		}

		PerformingEntity.Displacement.RegisterOnCurrentTile();
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (targetTileIDs != null && targetTileIDs.Length > 0/* && thisActionDestination.GetEntity(false) == null*/)
		{
			GameManager.Instance.StartCoroutine(PerformCR());
		}
		else
		{
			DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () =>
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
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.MoveToTile(targetTileIDs[i], null, true, movementSpeed);

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
			if (performingEntity.Displacement.Coordinates.GetTile().GetEntity(false) != null)
				Debug.LogError("CRITICAL ERROR : performing entity " + performingEntity.Data.name + " cant go back to where it was. Hope this never happens"); // solution? insta kill performing entity
			else
				performingEntity.Displacement.Coordinates.GetTile().SetEntity(performingEntity, _isThisTurn: false);
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
				//RefreshDestinatedTile();
				if (targetTileIDs == null)
					doesSelfHaveConflict = true;
			}
		}
		else if (targetTileIDs != null && GridManager.Instance.GetDistanceBetween(PerformingEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[targetTileIDs[0]], Data.movementSpeed, false) != Data.movementSpeed)
		{
			//check if tile too far
			doesSelfHaveConflict = true;
			//RefreshDestinatedTile();
		}
		else if (targetTileIDs == null)
		{
			doesSelfHaveConflict = true;
			//RefreshDestinatedTile();
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
				_otherMoveToTargetAction.targetTileIDs = null;
				doesOtherHaveConflict = true;
			}
			else
			{
				doesSelfHaveConflict = true;
				targetTileIDs = null;
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

		bool hasOtherEntityOnDestinations = false;
		foreach (int tileID in targetTileIDs)
		{
			Entity entity = GridManager.Instance.Tiles[tileID].GetEntity(_isThisTurn: false);
			if ((entity != null && entity.ID != performingEntityID)
				|| GridManager.Instance.Tiles[tileID].IsObstacle(false))
			{
				hasOtherEntityOnDestinations = true;
				break;
			}
		}

		return hasOtherEntityOnDestinations;
	}

	/*private void RefreshDestinatedTile ()
	{
		if (targetTileIDs == null)
			return;

		List<Tile> pathToTile = GridManager.Instance.GetPath(GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[(int)finalTargetTileID], _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count < Data.movementSpeed + 1)
		{
			targetTileIDs = null;
			positionAtActionEndID = GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.ID;
			return;
		}

		pathToTile.Reverse();
		targetTileIDs = new int[Data.movementSpeed];
		for (int i = 0; i < Data.movementSpeed; i++)
		{
			targetTileIDs[i] = pathToTile[i + 1].coordinates.ID;
			positionAtActionEndID = pathToTile[i + 1].coordinates.ID;
		}
	}*/

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
