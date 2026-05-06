using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class InvokeItemAction : SpecialAction
{
	public bool isActionCanceled;
	public int itemID;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		//serializer.SerializeValue(ref newEntityID);
		serializer.SerializeValue(ref isActionCanceled);
		serializer.SerializeValue(ref itemID);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return Data.invocatedItem.InvokeItemPredicate(PerformingEntity.Equipment.Tools[linkedEquipmentId], Data) && _tile.GetItem(true) == null && _tile.GetPlannedItemAt(timeAtStart) == null && base.TileInteractPredicate(_tile);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		targetTileID = _tile.coordinates.ID;
		Item invokedItem = GameManager.Instance.PreSpawnItem(Data.invocatedItem, PerformingEntity, PerformingEntity.Equipment.Tools[linkedEquipmentId], GridManager.Instance.Tiles[targetTileID].coordinates);
		_tile.SetPlannedItemAt(invokedItem, timeAtStart);
		itemID = invokedItem.ID;

		base.RegisterInteraction(_tile);
	}

	public override void CancelAction ()
	{
		Tile targetTile = GridManager.Instance.Tiles[targetTileID];
		targetTile.SetPlannedItemAt(null, timeAtStart);
		/*targetTile.plannedContentsPerTick[timeAtStart].item.Cancel();
		targetTile.plannedContentsPerTick[timeAtStart].item = null;*/

		base.CancelAction();
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		isActionCanceled = false;
		//newEntityID = GameManager.Instance.PlayersEntityAnchor[GameManager.Instance.PlayerID].Entities[^1].ID + 1;
		//GridManager.Instance.Tiles[targetTileID].SetItem(GameManager.Instance.GetEntityFromID(newEntityID), false);
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		EntityActionData.PFCResultType result = EntityActionData.PFC(Data, _otherAction.Data);
		if (isActionCanceled)
			return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };

		bool doesSelfHaveConflict = false;
		bool doesOtherHaveConflict = false;

		/*if (_otherAction is MoveToNeighborAction _otherNeighborMoveAction && _otherNeighborMoveAction.finalTargetTileID == targetTileID)
		{
			if (result == EntityActionData.PFCResultType.FirstWins)
			{
				_otherNeighborMoveAction.finalTargetTileID = -1;
				doesOtherHaveConflict = true;
			}
			else if (result == EntityActionData.PFCResultType.SecondWins)
			{
				isActionCanceled = true;
				doesSelfHaveConflict = true;
			}
			else
			{
				int roll = UnityEngine.Random.Range((int)0, 2);
				if (roll == 0)
				{
					_otherNeighborMoveAction.finalTargetTileID = -1;
					doesOtherHaveConflict = true;
				}
				else
				{
					isActionCanceled = true;
					doesSelfHaveConflict = true;
				}
			}

		}
		else */if (_otherAction is MoveToTargetAction _otherMoveToTargetAction && _otherMoveToTargetAction.thisActionDestinationIDArray.Contains(targetTileID))
		{
			if (result == EntityActionData.PFCResultType.FirstWins)
			{
				_otherMoveToTargetAction.thisActionDestinationIDArray = null;
				doesOtherHaveConflict = true;
			}
			else if (result == EntityActionData.PFCResultType.SecondWins)
			{
				isActionCanceled = true;
				doesSelfHaveConflict = true;
			}
			else
			{
				int roll = Random.Range((int)0, 2);
				if (roll == 0)
				{
					_otherMoveToTargetAction.thisActionDestinationIDArray = null;
					doesOtherHaveConflict = true;
				}
				else
				{
					isActionCanceled = true;
					doesSelfHaveConflict = true;
				}
			}
		}

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if (isActionCanceled)
		{
			CancelAction();
			EndTick();
			return;
		}
		Tile targetTile = GridManager.Instance.Tiles[targetTileID];
		Item item = targetTile.GetPlannedItemAt(timeAtStart);
		targetTile.SetItem(item, true);
		item.transform.position = targetTile.transform.position;

		base.Perform(_state);
		DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, EndTick);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		PlayerController.Instance.AddGhostItemAt(Data.invocatedItem, GridManager.Instance.Tiles[targetTileID], 0, itemID);
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{
		PlayerController.Instance.AddGhostItemAt(Data.invocatedItem, GridManager.Instance.Tiles[targetTileID], 0, itemID);
	}

}
