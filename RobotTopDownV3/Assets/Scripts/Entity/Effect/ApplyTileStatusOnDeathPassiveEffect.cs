using UnityEngine;

[CreateAssetMenu(fileName = "ApplyTileStatusOnDeath", menuName = "ScriptableObject/PassiveEffect/ApplyTileStatusOnDeath")]
public class ApplyTileStatusOnDeathPassiveEffect : AEntityPassiveEffect
{
	public EntityStatusEnumID statusApplied = EntityStatusEnumID.Burn1;
	[Min(1)] public int duration = 3;

	public override void OnDeathTrigger ( Entity _deadEntity )
	{
		base.OnDeathTrigger(_deadEntity);
		Tile tile = _deadEntity.Displacement.Coordinates.GetTile();
		if (tile != null)
			tile.AddStatus(statusApplied, duration);
	}
}
