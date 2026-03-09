using UnityEngine;

[CreateAssetMenu(fileName = "MaxTargetUp", menuName = "ScriptableObject/PassiveEffect/MaxTargetUp")]
public class MaxTargetUpPassiveEffect : AEntityPassiveEffect
{
	public int targetBoostAmount = 1;

	public enum ConditionType { Noone, DidNotMoveThisTurn}
	public ConditionType conditionType = ConditionType.Noone; 

	public override bool UseConditionPredicate ( AEntityAction _action, Entity _entity, Entity _targetEntity )
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
		}
	}

	/*public override void ApplyEffect ( Entity _entity )
	{
		if (_entity.Status.Contains(EntityStatusEnumID.Marked))
		{
			//apply here
		}
	}*/
}
