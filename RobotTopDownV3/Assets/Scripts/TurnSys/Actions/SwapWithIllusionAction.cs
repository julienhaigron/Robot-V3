public class SwapWithIllusionAction : SpecialAction
{
	protected override bool DoesOwnItsCompletion => true;

	public override void Init ( EntityActionData _data, string _linkedEquipmentID, int _performingEntityID, int _positionAtActionStartID, int _timeAtStart )
	{
		base.Init(_data, _linkedEquipmentID, _performingEntityID, _positionAtActionStartID, _timeAtStart);

		Entity illusion = PerformingEntity == null ? null : PerformingEntity.Illusion;
		if (illusion != null)
			positionAtActionEndID = TurnManager.Instance.GetLastRegisteredPositionOfEntity(illusion.ID);
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		Entity user = PerformingEntity;
		Entity illusion = user == null ? null : user.Illusion;
		if (illusion != null && !illusion.Equipment.IsDead && !user.Equipment.IsDead)
			user.Displacement.SwapWith(illusion.Displacement);

		base.Perform(_state);
		EndTick();
	}
}
