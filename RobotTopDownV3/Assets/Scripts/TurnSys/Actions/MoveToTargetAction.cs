using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using System.Linq;

public class MoveToTargetAction : AEntityAction
{
	public int targetEntiyID;
	public int targetTileID;
	public MoveActionMode mode;

	public int finalTargetTileID = -1;
	public int[] thisActionDestinationIDArray = null;
	public enum MoveActionMode { Coordinate, Entity }

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetEntiyID);
		serializer.SerializeValue(ref targetTileID);
		serializer.SerializeValue(ref mode);
		serializer.SerializeValue(ref finalTargetTileID);
		serializer.SerializeValue(ref thisActionDestinationIDArray);
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

		positionAtActionEndID = thisActionDestinationIDArray == null ? _positionAtActionStartID : thisActionDestinationIDArray[^1];
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		//check here if can do movement and where to exactly
		RefreshDestinatedTile();

		GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		//move to targetTile
		if (thisActionDestinationIDArray != null && thisActionDestinationIDArray.Length > 0 && finalTargetTileID != -1/* && thisActionDestination.GetEntity(false) == null*/)
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
		int movementAmount = Mathf.Min(thisActionDestinationIDArray.Length, Data.movementSpeed);
		float movementSpeed = GameConfig.current.game.actionDuration / movementAmount;

		for (int i = 0; i < movementAmount; i++)
		{
			List<Tile> tilesInRange = new();
			foreach (string weaponId in PerformingEntity.Equipment.Weapons.Keys)
				tilesInRange.AddRange(PerformingEntity.Equipment.GetTilesInWeaponRange(this, weaponId, true));

			foreach (Tile tile in tilesInRange)
			{
				tile.UI.SetOutlineColor(Color.blue);
			}
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.MoveToTile(thisActionDestinationIDArray[i], null, true, movementSpeed);

			yield return new WaitForSeconds(movementSpeed);
			foreach (Tile tile in tilesInRange)
			{
				tile.UI.ResetOutline();
			}
		}

		EndTick();
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		int maxDistance = TurnManager.Instance.RemainingActionToken[performingEntityID] * Data.movementSpeed;
		//for all tiles overall distance calculation
		if (GridManager.Instance.LastBFSOriginTile != from && GridManager.Instance.LastBFSMaxDistance >= maxDistance)
			GridManager.Instance.BFS(from, maxDistance, null, true);

		int distance = GridManager.Instance.GetDistanceBetween(from, _tile, maxDistance, true);

		if (_tile.IsObstacle(true) || distance > maxDistance || distance < 1)
			return false;

		return true;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		//register all action for destination (calcuate dist, and add X actions in TurnSys (X = distance))
		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> path = GridManager.Instance.GetPath(from, _tile, true);

		path.Reverse();
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
			action.thisActionDestinationIDArray = tileIDList.ToArray();
			action.Init(GameAssets.current.game.entityActionsData[enumID], linkedEquipmentId, performingEntityID, path[i].coordinates.ID, timeAtStart + i);

			if (_tile.TryGetPlannedItemAt(timeAtStart + i, out Item _item))
				_item.Data.OnRegisterInteraction(action, _item);

			TurnManager.Instance.AddAction(performingEntityID, action, TurnManager.Instance.CurrentStateTypeSelected);
		}

		TurnManager.Instance.RefreshActionDisplay(performingEntityID);
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		Entity performingEntity = GameManager.Instance.GetEntityFromID(performingEntityID);
		if (finalTargetTileID == -1)
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
				RefreshDestinatedTile();
				if (finalTargetTileID == -1)
					doesSelfHaveConflict = true;
			}
		}
		else if (thisActionDestinationIDArray != null && GridManager.Instance.GetDistanceBetween(PerformingEntity.Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[thisActionDestinationIDArray[^1]], Data.movementSpeed, false) > Data.movementSpeed)
		{
			//check if tile too far
			doesSelfHaveConflict = true;
			RefreshDestinatedTile();
		}
		else if (thisActionDestinationIDArray == null)
		{
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
		else if (_otherAction is MoveToTargetAction _otherMoveToTargetAction && _otherMoveToTargetAction.thisActionDestinationIDArray == thisActionDestinationIDArray)
		{
			bool doesHaveCollision = false;
			foreach (int ourID in thisActionDestinationIDArray)
			{
				if (_otherMoveToTargetAction.thisActionDestinationIDArray.Contains(ourID))
				{
					doesHaveCollision = true;
					break;
				}
			}
			if (doesHaveCollision)
			{

				int roll = UnityEngine.Random.Range((int)0, 2);
				if (roll == 0)
				{
					//performing entity wins roll
					_otherMoveToTargetAction.thisActionDestinationIDArray = null;
					doesOtherHaveConflict = true;
				}
				else
				{
					doesSelfHaveConflict = true;
					thisActionDestinationIDArray = null;
				}
			}
		}

		if (doesSelfHaveConflict == false)
		{
			foreach (int tileID in thisActionDestinationIDArray)
				GridManager.Instance.Tiles[tileID].SetEntity(performingEntity, _isThisTurn: false);
		}

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (thisActionDestinationIDArray == null)
			return false;

		bool hasOtherEntityOnDestinations = false;
		foreach (int tileID in thisActionDestinationIDArray)
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

	private void RefreshDestinatedTile ()
	{
		if (finalTargetTileID == -1)
			return;

		List<Tile> pathToTile = GridManager.Instance.GetPath(GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile(), GridManager.Instance.Tiles[(int)finalTargetTileID], _isThisTurn: false);

		if (pathToTile == null || pathToTile.Count < 2)
		{
			finalTargetTileID = -1;
			positionAtActionEndID = GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.ID;
			return;
		}

		pathToTile.Reverse();
		for (int i = 0; i < Data.movementSpeed; i++)
		{
			thisActionDestinationIDArray[i] = pathToTile[i + 1].coordinates.ID;
			positionAtActionEndID = pathToTile[i + 1].coordinates.ID;
		}
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		Vector3 previousPosition = GridManager.Instance.Tiles[supposedPositionAtActionStartID].transform.position;
		foreach (int tileID in thisActionDestinationIDArray)
		{
			ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
			Vector3 startPos = previousPosition;
			Vector3 destination = GridManager.Instance.Tiles[tileID].transform.position;
			Vector3 position = Vector3.Lerp(startPos, destination, .5f);
			previousPosition = destination;
			arrow.Init(_recordedAction);
			arrow.transform.position = position;
			arrow.transform.LookAt(GridManager.Instance.Tiles[tileID].transform);

			PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, false);
		}
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		if (positionAtActionEndID == -1)
			return;

		Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> path = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[positionAtActionEndID], true, false);

		for (int i = 0; i < path.Count - 1; i++)
		{
			Tile thisTile = path[i];
			Tile otherTile = path[i+1];
			ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
			Vector3 startPos = thisTile.transform.position;
			Vector3 destination = otherTile.transform.position;
			Vector3 position = Vector3.Lerp(startPos, destination, .5f);
			arrow.SetMaterial(GameAssets.current.ui.ghostEntityStateMaterials[_state]);
			arrow.transform.position = position;
			arrow.transform.LookAt(otherTile.transform);

			PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, true);
		}
	}
}
