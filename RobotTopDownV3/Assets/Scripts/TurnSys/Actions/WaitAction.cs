public class WaitAction : AEntityAction
{
	public override bool TileInteractPredicate ( Tile _tile )
	{
		return _tile == GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(performingEntityID)];
	}

	public override void RegisterInteraction ( Tile _tile )
	{
		TurnManager.Instance.RegisterAction(performingEntityID, TurnManager.Instance.CurrentActionSelected, TurnManager.Instance.GetDefaultStateOf(performingEntityID));
		TurnManager.Instance.RefreshActionDisplay(performingEntityID, true);
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		base.Perform(_state);

		DG.Tweening.DOVirtual.DelayedCall(TickTimeLeft, EndTick);
	}
}
