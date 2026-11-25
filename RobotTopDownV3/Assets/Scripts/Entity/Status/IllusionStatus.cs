using UnityEngine;

[CreateAssetMenu(fileName = "Illusion", menuName = "ScriptableObject/Status/Illusion")]
public class IllusionStatus : AEntityStatus
{
	public override void OnRemoveStatusEffect ( Entity _entity )
	{
		if (!_entity.Equipment.IsDead)
			_entity.Equipment.InstantDeath(EntityEquipmentPlugin.DeathCause.Vanish);
	}
}
