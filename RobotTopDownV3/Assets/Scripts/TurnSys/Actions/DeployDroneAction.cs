public class DeployDroneAction : SpecialAction
{
	protected override bool DoesOwnItsCompletion => true;

	public override bool TileInteractPredicate ( Tile _tile )
	{
		return _tile != null && _tile.GroundType != TileGroundType.Wall && base.TileInteractPredicate(_tile);
	}

	protected override void Perform ( Entity.EntityState _state )
	{
		if (!isActionCanceled && targetTileIDs != null && targetTileIDs.Length > 0 && TryGetDrone(out DroneWeapon drone))
			drone.Deploy(GridManager.Instance.Tiles[targetTileIDs[0]], Data.GetAoEMaxRange(this, PerformingEntity, null), Data.deployDuration);

		base.Perform(_state);
	}

	private bool TryGetDrone ( out DroneWeapon _drone )
	{
		_drone = PerformingEntity != null && linkedEquipmentId != null
			&& PerformingEntity.Equipment.Weapons.TryGetValue(linkedEquipmentId, out Weapon weapon) ? weapon as DroneWeapon : null;
		return _drone != null;
	}
}
