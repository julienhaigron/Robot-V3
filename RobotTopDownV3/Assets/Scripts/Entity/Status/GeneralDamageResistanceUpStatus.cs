using UnityEngine;

[CreateAssetMenu(fileName = "GeneralResistanceUp", menuName = "ScriptableObject/Status/GeneralResistanceUp")]
public class GeneralDamageResistanceUpStatus : AEntityStatus
{
	public float amount = .3f;

	public override void ApplyStatusEffect ( int _remainingDuration, Entity _entity )
	{
		base.ApplyStatusEffect(_remainingDuration, _entity);

		if(_remainingDuration == duration)
			_entity.Equipment.GeneralDamageResistance += amount;
	}

	public override void OnRemoveStatusEffect ( Entity _entity )
	{
		base.OnRemoveStatusEffect(_entity);

		_entity.Equipment.GeneralDamageResistance -= amount;
	}
}
