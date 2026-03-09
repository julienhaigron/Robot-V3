using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatus", menuName = "ScriptableObject/PassiveEffect/ApplyStatus")]
public class ApplyStatusPassiveEffect : AEntityPassiveEffect
{
	public bool doApplyToSelf = false;
	public bool doApplyToTile = false;
	public EntityStatusEnumID statusApplied;

	public override bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity )
	{
		if (_action == null || _entity == null)
			return false;

		return true;
	}

	public override void ApplyEffect ( Entity _entity, Entity _targetEntity )
	{
		if(doApplyToSelf)
			_entity.AddStatus(statusApplied);
		else
			_targetEntity.AddStatus(statusApplied);
	}

	public void ApplyEffect(Tile _tile )
	{
		_tile.AddStatus(statusApplied);
	}
}
