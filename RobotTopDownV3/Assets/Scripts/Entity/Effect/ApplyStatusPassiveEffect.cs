using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatus", menuName = "ScriptableObject/PassiveEffect/ApplyStatus")]
public class ApplyStatusPassiveEffect : AEntityPassiveEffect
{
	public bool doApplyToSelf = false;
	public EntityStatusEnumID statusApplied;

	public override bool UseConditionPredicate ( Entity _entity, Entity _targetEntity )
	{
		return true;
	}

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity )
	{
		if(doApplyToSelf)
			_entity.AddStatus(statusApplied);
		else
			_targetEntity.AddStatus(statusApplied);
	}
}
