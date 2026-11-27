using UnityEngine;

[CreateAssetMenu(fileName = "ActivateEnergyShield", menuName = "ScriptableObject/PassiveEffect/ActivateEnergyShield")]
public class ActivateEnergyShieldPassiveEffect : AEntityPassiveEffect
{
	public override void ApplyEffect ( Entity _performingEntity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		base.ApplyEffect(_performingEntity, _targetEntity, _effectContainer);
		foreach (ToolEquipment tool in _targetEntity.Equipment.Tools.Values)
		{
			if (tool is Shield shield && shield.IsActivable)
				shield.Activate();
		}
	}
}
