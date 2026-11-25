using UnityEngine;

[CreateAssetMenu(fileName = "Riposte", menuName = "ScriptableObject/Status/Riposte")]
public class RiposteStatus : AEntityStatus
{
	public override void OnHitReceived ( AttackAction _action, Entity _holder, Entity _attacker )
	{
		if (_action.isRiposte || _action.Data.type != EntityActionData.ActionType.MeleeAttack || _attacker == null || _attacker == _holder
			|| _attacker.IsAlliedTo(_holder.OwnerID))
			return;

		_holder.Equipment.Riposte(_attacker);
	}
}
