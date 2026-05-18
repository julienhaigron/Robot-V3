using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpecialAction : AEntityAction
{
	public int targetedEntityID;
	public int targetTileID;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref targetedEntityID);
		serializer.SerializeValue(ref targetTileID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		//linkedEquipmentId = PerformingEntity.ComponentLinkedToAction[enumID];
		if (_tile.GetEntity(true))
			targetedEntityID = _tile.GetEntity(true).ID;
		targetTileID = _tile.coordinates.ID;

		base.RegisterInteraction(_tile);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		/*if (targetedEntityID == -1 && GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity != null)
			targetedEntityID = GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity.ID;
		else if(targetedEntityID == -1)
		{
			//TODO : handle this situation
			Debug.Log("ERROR : no available target");
		}*/
	}

	public override ActionConflictResultInfo CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?

		return new() { isFirstActionConflicted = false, isSecondActionConflicted = false };
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);
		//todo : apply effect
	}

	public override void Display ( TurnManager.RecordedAction _recordedAction )
	{
		//TODO ?
	}

	public override void GhostDisplay ( Entity.EntityState _state )
	{

	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		if (Data.targetType == EntityActionData.TargetType.Self && _tile.coordinates.ID == TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID))
			return true;

		int maxDist = Data.GetMaxRange(this, PerformingEntity, null);
		bool isInRange = GridManager.Instance.GetTilesInVisionRange(GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)], maxDist, false, true).Contains(_tile);

		if (Data.targetType == EntityActionData.TargetType.Tile && isInRange)
			return true;

		Entity entity = _tile.GetEntity(true);
		return entity != null && isInRange && !entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID);
	}
}
