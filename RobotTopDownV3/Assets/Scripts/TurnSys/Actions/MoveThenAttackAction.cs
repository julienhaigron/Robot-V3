using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class MoveThenAttackAction : AttackAction
{
	public int positionAfterMovementID = -1;
	public bool isActionCanceled = false;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref positionAfterMovementID);
		serializer.SerializeValue(ref isActionCanceled);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		positionAfterMovementID = _tile.Neighbors[GridManager.Instance.GetClosestOrientation(_tile, GridManager.Instance.Tiles[supposedPositionAtActionStartID])].coordinates.ID;

		base.RegisterInteraction(_tile);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(null, _isThisTurn: false);
		base.Prepare(_state);
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		if (isActionCanceled || positionAfterMovementID == -1)
		{
			//entity move action canceled
			if (GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().GetEntity(false) != null)
				Debug.LogError("CRITICAL ERROR : performing entity " + GameManager.Instance.GetEntityFromID(performingEntityID).Data.name + " cant go back to where it was. Hope this never happens"); // solution? insta kill performing entity
			else
				GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.Coordinates.GetTile().SetEntity(GameManager.Instance.GetEntityFromID(performingEntityID), _isThisTurn: false);
			return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
		}

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;
		Entity user = GameManager.Instance.GetEntityFromID(performingEntityID);
		int maxDist = Data.maxDistance - 1;

		/*if (_otherAction is MoveToNeighborAction && (_otherAction as MoveToNeighborAction).finalTargetTileID == positionAfterMovementID)
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				(_otherAction as MoveToNeighborAction).finalTargetTileID = -1;
				doesOtherHaveConflict = true;
			}
			else
			{
				positionAfterMovementID = -1;
				isActionCanceled = true;
			}
		}
		else */if (_otherAction is MoveToTargetAction && (_otherAction as MoveToTargetAction).thisActionDestinationIDArray.Contains(positionAfterMovementID))
		{
			int roll = UnityEngine.Random.Range((int)0, 2);
			if (roll == 0)
			{
				(_otherAction as MoveToTargetAction).thisActionDestinationIDArray = null;
				doesOtherHaveConflict = true;
			}
			else
			{
				positionAfterMovementID = -1;
				isActionCanceled = false;
			}
		}
		else if (IsDestinationOccupiedOnNextTurnAction())
		{
			doesSelfHaveConflict = true;
			isActionCanceled = false;

			/*if (finalTargetTile != null)
				finalTargetTile.SetEntity(performingEntity, _isThisTurn: false);*/
		}
		//check if tile too far
		else if (positionAfterMovementID != -1 && GridManager.Instance.GetDistanceBetween(GridManager.Instance.Tiles[supposedPositionAtActionStartID], GridManager.Instance.Tiles[(int)positionAfterMovementID], Data.movementSpeed, false) > maxDist)
		{
			doesSelfHaveConflict = true;
			isActionCanceled = false;
		}

		if (doesSelfHaveConflict == false)
		{
			GridManager.Instance.Tiles[(int)positionAfterMovementID].SetEntity(GameManager.Instance.GetEntityFromID(performingEntityID), _isThisTurn: false);
		}

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		List<Tile> tilesInRange = new();
		foreach (string weaponId in PerformingEntity.Equipment.Weapons.Keys)
			tilesInRange.AddRange(PerformingEntity.Equipment.GetTilesInWeaponRange(this, weaponId, true));

		foreach (Tile tile in tilesInRange)
		{
			tile.UI.SetOutlineColor(Color.blue);
		}

		//move to targetTile
		if (positionAfterMovementID != -1/* && thisActionDestination.GetEntity(false) == null*/)
		{
			GameManager.Instance.GetEntityFromID(performingEntityID).Displacement.MoveToTile((int)positionAfterMovementID, () =>
			{
				foreach (Tile tile in tilesInRange)
				{
					tile.UI.ResetOutline();
				}
				base.Perform(_state);
			});

		}
		else
		{
			DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, () =>
			{
				foreach (Tile tile in tilesInRange)
				{
					tile.UI.ResetOutline();
				}
				base.Perform(_state);
			});
		}
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
		Vector3 startPos = GridManager.Instance.Tiles[supposedPositionAtActionStartID].transform.position;
		Vector3 destination = GridManager.Instance.Tiles[(int)positionAfterMovementID].transform.position;
		Vector3 position = Vector3.Lerp(startPos, destination, .5f);
		arrow.Init(_recordedAction);
		arrow.transform.position = position;
		arrow.transform.LookAt(GridManager.Instance.Tiles[(int)positionAfterMovementID].transform);

		PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, false);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		Tile from = GridManager.Instance.Tiles[(int)TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
		List<Tile> pathToTile = GridManager.Instance.GetPath(from, GridManager.Instance.Tiles[(int)positionAfterMovementID], _isThisTurn: false);
		pathToTile.Reverse();

		for (int i = 0; i < pathToTile.Count - 1; i++)
		{
			ActionDisplayOnTile arrow = ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData) as ActionDisplayOnTile;
			Vector3 startPos = pathToTile[i].transform.position;
			Vector3 destination = pathToTile[i + 1].transform.position;
			Vector3 position = Vector3.Lerp(startPos, destination, .5f);
			arrow.SetMaterial(GameAssets.current.ui.ghostEntityStateMaterials[_state]);
			arrow.transform.position = position;
			arrow.transform.LookAt(pathToTile[i + 1].transform);

			PlayerController.Instance.AddActionDisplay(arrow, performingEntityID, true);
		}
	}

	private bool IsDestinationOccupiedOnNextTurnAction ()
	{
		if (positionAfterMovementID == -1)
			return false;

		Entity entityOnDestination = GridManager.Instance.Tiles[(int)positionAfterMovementID].GetEntity(_isThisTurn: false);

		return (entityOnDestination != null && entityOnDestination.ID != performingEntityID) || GridManager.Instance.Tiles[(int)positionAfterMovementID].IsObstacle(false);
	}
}
