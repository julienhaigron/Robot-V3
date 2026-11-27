using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Thorns", menuName = "ScriptableObject/Status/Thorns")]
public class ThornsStatus : AEntityStatus
{
	[Range(0f, 1f)] public float reflectRatio = .3f;

	public override void OnDamageTaken ( Entity _holder, EntityEquipmentPlugin.TakeDamageCallback _damageInfo )
	{
		Entity attacker = _damageInfo.entityAttacker;
		if (_damageInfo.isReflected || attacker == null || attacker == _holder || attacker.Equipment.IsDead || _damageInfo.damages == null)
			return;

		int total = LogCard.Sum(_damageInfo.damages, out WeaponEquipmentData.DamageType? mainType);
		int reflected = Mathf.CeilToInt(total * reflectRatio);
		if (reflected <= 0 || mainType == null)
			return;

		LogConsole.AddLog(string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_thorns_reflect), _holder.Data.name, reflected, attacker.Data.name), LogConsole.LogEventType.Status);
		attacker.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback()
		{
			entityAttacker = _holder,
			entityTargeted = attacker,
			damages = new Dictionary<WeaponEquipmentData.DamageType, int> { { mainType.Value, reflected } },
			isReflected = true
		});
	}
}
