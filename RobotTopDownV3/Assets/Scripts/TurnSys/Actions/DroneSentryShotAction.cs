public class DroneSentryShotAction : AttackAction
{
	public int originTileID = -1;

	protected override Tile GetAttackOriginTile ()
	{
		return originTileID >= 0 ? GridManager.Instance.Tiles[originTileID] : base.GetAttackOriginTile();
	}
}
