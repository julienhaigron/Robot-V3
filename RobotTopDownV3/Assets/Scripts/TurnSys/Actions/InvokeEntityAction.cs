using Unity.Netcode;

public class InvokeEntityAction : SpecialAction
{
	public int newEntityID;

	protected override bool DoesOwnItsCompletion => true;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref newEntityID);
	}

	public override void Prepare ( Entity.EntityState _state )
	{
		base.Prepare(_state);

		if (!isActionCanceled && targetTileIDs != null)
			newEntityID = GameManager.Instance.PlayersEntityAnchor[PerformingEntity.OwnerID].ReserveEntityIDs(PerformingEntity.OwnerID, targetTileIDs.Length);
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if (isActionCanceled)
		{
			EndTick();
			return;
		}

		int ownerID = PerformingEntity.OwnerID;
		for (int i = 0; i < targetTileIDs.Length; i++)
			GameManager.Instance.PlayersEntityAnchor[ownerID].SpawnEntityDuringPlay(Data.invocatedEntity.GetSavedData(), newEntityID + i, ownerID, targetTileIDs[i], EndTick);

		base.Perform(_state);
	}
}
