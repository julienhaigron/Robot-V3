using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class UseItemAction : SpecialAction
{

	public bool isActionCanceled;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		//serializer.SerializeValue(ref newEntityID);
		serializer.SerializeValue(ref isActionCanceled);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		Item usedItem = GridManager.Instance.Tiles[targetTileID].GetItem(true);
		return usedItem.Data.InteractPredicate(PerformingEntity, usedItem.LinkedData, usedItem) && base.TileInteractPredicate(_tile);
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

		Item usedItem = GridManager.Instance.Tiles[targetTileID].GetItem(true);
		if (!usedItem.Data.InteractPredicate(PerformingEntity, usedItem.LinkedData, usedItem))
		{
			isActionCanceled = true;
		}

		return new() { isFirstActionConflicted = doesSelfHaveConflict, isSecondActionConflicted = doesOtherHaveConflict };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if (isActionCanceled)
		{
			EndTick();
			return;
		}

		Item usedItem = GridManager.Instance.Tiles[targetTileID].GetItem(true);
		usedItem.Data.Interract(PerformingEntity, usedItem.LinkedData, usedItem, EndTick);

		base.Perform(_state);
		//DG.Tweening.DOVirtual.DelayedCall(GameConfig.current.game.actionDuration, EndTick);
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}
}
