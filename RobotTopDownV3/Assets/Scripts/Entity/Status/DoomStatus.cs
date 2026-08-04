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

	public override void OnRemoveStatusEffect ( Entity _entity )
	{
		base.OnRemoveStatusEffect(_entity);
		_entity.Equipment.InstantDeath();
	}
}
