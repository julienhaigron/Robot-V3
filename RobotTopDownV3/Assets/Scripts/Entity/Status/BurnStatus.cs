using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "ScriptableObject/Status/BurnStatus")]
public class BurnStatus : AEntityStatus
{
    public int damageAmount = 1;

	public override void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
	{
		base.ApplyStatusEffect(_remainingDuration, _entity);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = GetTickDamages(_entity), entityTargeted = _entity });
	}

	public override string GetTickEffectText ( int _remainingDuration, Entity _entity )
	{
		return string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_exchange_damage), GetTickDamages(_entity)[WeaponEquipmentData.DamageType.Fire]);
	}

	private Dictionary<WeaponEquipmentData.DamageType, int> GetTickDamages ( Entity _entity )
	{
		Dictionary<WeaponEquipmentData.DamageType, int> damages = new()
		{
			{ WeaponEquipmentData.DamageType.Fire, Mathf.RoundToInt(damageAmount * _entity.GetStatusDamageFactor(enumID)) }
		};
		return _entity.Equipment.GetDamagesAfterResistances(damages);
	}

	public override void PerformStatusEffectAtBeginingOfRound ( Tile _tile )
	{
		base.PerformStatusEffectAtBeginingOfRound(_tile);
		if (_tile.GetEntity(true) != null)
			_tile.GetEntity(true).AddStatus(enumID, _tile.GetRemainingStatusDuration(enumID));
	}
}
