using UnityEngine;

[CreateAssetMenu(fileName = "Assault", menuName = "ScriptableObject/Status/Assault")]
public class AssaultStatus : AEntityStatus
{
	public float damageBonusPerTile = .1f;

	public float GetDamageBonus ( AEntityAction _action, Entity _performer )
	{
		int ownMovement = _action is AttackAction attack ? attack.GetOwnMovementLength() : 0;
		return damageBonusPerTile * (_performer.Displacement.TraveledTileCountThisTurn + ownMovement);
	}
}
