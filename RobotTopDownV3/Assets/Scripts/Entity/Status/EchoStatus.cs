using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Echo", menuName = "ScriptableObject/Status/Echo")]
public class EchoStatus : AEntityStatus
{
	public override void OnBoostedHit ( AttackAction _action, Entity _attacker, Entity _target, Dictionary<WeaponEquipmentData.DamageType, int> _damages, bool _isFirstHitOnTarget )
	{
		_target.QueueEchoDamage(_attacker, _damages);
	}
}
