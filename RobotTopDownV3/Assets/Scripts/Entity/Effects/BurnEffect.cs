using UnityEngine;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "ScriptableObject/Effect/BurnEffect")]
public class BurnEffect : AEntityEffect
{
    public int damageAmount = 1;

	public override void ApplyEffect ( Entity _entity )
	{
		base.ApplyEffect(_entity);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damage = damageAmount, entityTargeted = _entity });
	}
}
