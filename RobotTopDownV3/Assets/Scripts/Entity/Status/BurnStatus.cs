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

		Dictionary<WeaponEquipmentData.DamageType, int> damage = new();
		damage.Add(WeaponEquipmentData.DamageType.Feu, damageAmount);

		_entity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damage, entityTargeted = _entity });
	}

	public override void PerformStatusEffectAtBeginingOfRound ( Tile _tile )
	{
		base.PerformStatusEffectAtBeginingOfRound(_tile);
		if (_tile.GetEntity(true) != null)
			_tile.GetEntity(true).AddStatus(enumID);
	}
}
