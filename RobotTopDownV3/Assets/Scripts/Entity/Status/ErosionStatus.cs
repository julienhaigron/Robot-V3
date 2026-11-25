using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Erosion", menuName = "ScriptableObject/Status/Erosion")]
public class ErosionStatus : AEntityStatus
{
	public float resistanceReduction = .1f;

	public override void OnBoostedHit ( AttackAction _action, Entity _attacker, Entity _target, Dictionary<WeaponEquipmentData.DamageType, int> _damages, bool _isFirstHitOnTarget )
	{
		if (!_isFirstHitOnTarget)
			return;

		int duration = TurnManager.Instance.RemainingTicksInRound;
		foreach (WeaponEquipmentData.DamageType damageType in _damages.Keys)
		{
			_target.AddAdditionaryStatBonus(new EntityEquipmentData.StatBonusBuff()
			{
				statBonus = new EntityEquipmentData.SecondaryStat() { type = GameConfig.current.game.statTypePerDamageResistanceType[damageType], value = -resistanceReduction },
				duration = duration
			});
		}
	}
}
