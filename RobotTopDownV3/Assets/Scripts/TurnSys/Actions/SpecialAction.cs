using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpecialAction : AEntityAction
{
	public string rotatingWeaponID;
	public int targetedEntityID = -1;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref rotatingWeaponID);
		serializer.SerializeValue(ref targetedEntityID);
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		if(targetedEntityID == -1 && GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity != null)
			targetedEntityID = GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity.ID;

		base.RegisterInteraction(_tile);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		if (targetedEntityID == -1 && GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity != null)
			targetedEntityID = GameManager.Instance.GetEntityFromID(performingEntityID).AI.TargetedEntity.ID;
		else if(targetedEntityID == -1)
		{
			//TODO : handle this situation
			Debug.Log("ERROR : no available target");
		}
	}

	public override bool CheckConflict ( AEntityAction _otherAction, bool _isCheck = true )
	{
		//no conflict ?
		return false;
	}

	public override void Perform ( Entity.EntityState _state )
	{
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
		//TODO : select only visible enemies

		Entity entity = _tile.GetEntity(true);
		return entity != null && (Data.targetType == EntityActionData.TargetType.Self == entity.IsAlliedTo(GameManager.Instance.GetEntityFromID(performingEntityID).OwnerID));
	}
}
