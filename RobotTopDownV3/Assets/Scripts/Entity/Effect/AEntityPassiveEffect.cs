using UnityEngine;
using System;

[Serializable]
public abstract class AEntityPassiveEffect : ScriptableEnum<EntityPassiveEffectEnumID>
{
	public enum ConditionType { Noone, DidNotMoveThisTurn, IsTargetMarked }
	public ConditionType conditionType = ConditionType.Noone;

	public virtual bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity )
	{
		if (_action == null || _entity == null)
			return false;

		switch (conditionType)
		{
			default:
			case ConditionType.Noone:
				return true;
			case ConditionType.DidNotMoveThisTurn:
				bool recordedCheck = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityMoved >= _action.timeAtStart;
				bool liveCheck = !_entity.Displacement.DidMoveThisTurn;
				return liveCheck && recordedCheck;
			case ConditionType.IsTargetMarked:
				return _targetEntity.Status.Contains(EntityStatusEnumID.Marked);
		}
	}

	public virtual void ApplyEffect ( Entity _performingEntity, Entity _targetEntity )
    {

    }
}