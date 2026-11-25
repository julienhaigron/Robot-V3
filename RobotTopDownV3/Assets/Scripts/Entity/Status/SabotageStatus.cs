using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sabotage", menuName = "ScriptableObject/Status/Sabotage")]
public class SabotageStatus : AEntityStatus
{
	public EntityStatusEnumID appliedStatus = EntityStatusEnumID.Sabotaged;
	public int appliedCharges = 1;

	public override void OnBoostedHit ( AttackAction _action, Entity _attacker, Entity _target, Dictionary<WeaponEquipmentData.DamageType, int> _damages, bool _isFirstHitOnTarget )
	{
		if (_isFirstHitOnTarget)
			_target.AddStatus(appliedStatus, appliedCharges, _attacker);
	}
}
