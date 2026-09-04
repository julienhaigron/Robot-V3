using UnityEngine;

[CreateAssetMenu(fileName = "Doom", menuName = "ScriptableObject/Status/DoomStatus")]
public class DoomStatus : AEntityStatus
{
	//public int damageAmount = 1;

	/*public override void ApplyEffect ( Entity _entity )
	{
		base.ApplyEffect(_entity);

		//_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damage = damageAmount, entityTargeted = _entity });
	}*/

	public override void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
	{
		base.ApplyStatusEffect(_remainingDuration, _entity);

		if (_remainingDuration <= 1)
			_entity.Equipment.InstantDeath();
	}
}
