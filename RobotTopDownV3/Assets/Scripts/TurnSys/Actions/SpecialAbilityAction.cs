using Unity.Netcode;

public class SpecialAbilityAction : AEntityAction
{
	public int chargesSpent;

	public bool IsRetreat => Data.abilityEffect == EntityActionData.SpecialAbilityEffect.Retreat;

	public override void NetworkSerialize<T> ( BufferSerializer<T> serializer )
	{
		base.NetworkSerialize(serializer);
		serializer.SerializeValue(ref chargesSpent);
	}

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return false;
	}

	public override void OnModActionAdded ( AEntityAction _mainAction )
	{
		timeAtStart = _mainAction.timeAtStart;
		supposedPositionAtActionStartID = _mainAction.supposedPositionAtActionStartID;
		positionAtActionEndID = _mainAction.supposedPositionAtActionStartID;
	}
}
