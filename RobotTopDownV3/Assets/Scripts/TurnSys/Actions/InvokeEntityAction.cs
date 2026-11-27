using Unity.Netcode;
using UnityEngine;

public class InvokeEntityAction : SpecialAction
{
	public int newEntityID = -1;

	protected override bool DoesOwnItsCompletion => true;

	private EntityAnchor InvokerAnchor => GameManager.Instance.PlayersEntityAnchor[PerformingEntity.OwnerID];

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref newEntityID);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return IsFreeForInvocation(_tile) && !InvokerAnchor.TryGetPendingInvocationOn(_tile, out _)
			&& Data.CanInvokeMore(PerformingEntity, linkedEquipmentId) && base.TileInteractPredicate(_tile);
	}

	private static bool IsFreeForInvocation ( Tile _tile )
	{
		return _tile != null && !_tile.IsObstacle(true) && _tile.GetCurrentEntity() == null;
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		InvokeEntityAction replaced = EntityActionDisplay.SelectedDisplay != null ? EntityActionDisplay.SelectedDisplay.RecordedAction.action as InvokeEntityAction : null;
		base.RegisterInteraction(_tile);
		if (targetTileIDs == null || targetTileIDs.Length == 0)
			return;

		if (replaced != null && replaced != this)
			replaced.ReleasePlannedInvocation();

		ReleasePlannedInvocation();
		newEntityID = InvokerAnchor.ReserveEntityIDs(PerformingEntity.OwnerID);
		InvokerAnchor.SpawnInvocation(Data.invocatedEntity.GetSavedData(), newEntityID, PerformingEntity, linkedEquipmentId, targetTileIDs[0], TimeAtStartPerform);
		TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
	}

	public override void CancelAction ()
	{
		ReleasePlannedInvocation();
		base.CancelAction();
	}

	private void ReleasePlannedInvocation ()
	{
		Entity invocation = newEntityID >= 0 && GameManager.Instance != null ? GameManager.Instance.GetEntityFromID(newEntityID) : null;
		if (invocation == null || !invocation.IsPendingInvocation)
			return;

		newEntityID = -1;
		if (!invocation.Equipment.IsDead)
			GameManager.Instance.PlayersEntityAnchor[invocation.OwnerID].DiscardPendingInvocation(invocation);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		base.Prepare(_state);

		if (!isActionCanceled && newEntityID < 0 && targetTileIDs != null && targetTileIDs.Length > 0)
			newEntityID = InvokerAnchor.ReserveEntityIDs(PerformingEntity.OwnerID);
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		Tile tile = targetTileIDs != null && targetTileIDs.Length > 0 ? GridManager.Instance.Tiles[targetTileIDs[0]] : null;
		Entity invocation = newEntityID >= 0 ? GameManager.Instance.GetEntityFromID(newEntityID) : null;

		if (isActionCanceled || !IsFreeForInvocation(tile) || newEntityID < 0)
		{
			if (!isActionCanceled)
				LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_invocation_failed), PerformingEntity.Data.name), LogConsole.LogEventType.ActionResolution);
			ReleasePlannedInvocation();
		}
		else
		{
			if (invocation != null && invocation.IsPendingInvocation)
				InvokerAnchor.MaterializeInvocation(invocation);
			else
				InvokerAnchor.SpawnInvocation(Data.invocatedEntity.GetSavedData(), newEntityID, PerformingEntity, linkedEquipmentId, tile.coordinates.ID);

			if (PerformingEntity.IsAlliedTo(GameManager.Instance.PlayerID) || tile.IsVisible)
				Weapon.PlayOrientedVFX(Data.onPerformVFXPool, tile.transform.position, Vector3.zero, Data.name);
		}

		base.Perform(_state);
	}
}
