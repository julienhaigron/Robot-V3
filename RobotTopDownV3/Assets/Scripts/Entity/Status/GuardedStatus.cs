using UnityEngine;

[CreateAssetMenu(fileName = "Guarded", menuName = "ScriptableObject/Status/Guarded")]
public class GuardedStatus : AEntityStatus
{
	public override bool IsBroken ( Entity _holder )
	{
		return !TryGetGuardian(_holder, out _);
	}

	public override bool TryRedirectDamage ( Entity _holder, ref EntityEquipmentPlugin.TakeDamageCallback _damageInfo )
	{
		if (_damageInfo.isRedirected || _damageInfo.entityAttacker == null || _damageInfo.entityAttacker == _holder || !TryGetGuardian(_holder, out Entity guardian))
			return false;

		_damageInfo.isRedirected = true;
		_damageInfo.entityTargeted = guardian;
		_damageInfo.hitTile = guardian.Displacement.Coordinates.GetTile();
		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_guard_intercept), guardian.Data.name, _holder.Data.name), LogConsole.LogEventType.Status);
		return true;
	}

	private bool TryGetGuardian ( Entity _holder, out Entity _guardian )
	{
		_guardian = _holder.GetStatusSource(enumID);
		return _guardian != null && !_guardian.Equipment.IsDead && _guardian.IsAdjacentTo(_holder);
	}
}
