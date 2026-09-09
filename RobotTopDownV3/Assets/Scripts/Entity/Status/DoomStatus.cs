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

	public override string GetTickEffectText ( int _remainingDuration, Entity _entity )
	{
		return IsLastActiveTick(_remainingDuration) ? LocalizationManager.Instance.Get(LocalizationKey.log_doom_triggers) : null;
	}

	public override void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
	{
		base.ApplyStatusEffect(_remainingDuration, _entity);

		if (IsLastActiveTick(_remainingDuration))
			_entity.Equipment.InstantDeath();
	}
}
