using UnityEngine;

[CreateAssetMenu(fileName = "MaxRangeUp", menuName = "ScriptableObject/PassiveEffect/MaxRangeUp")]
public class MaxRangeUpPassiveEffect : AEntityPassiveEffect
{
	public int rangeBoostAmount = 1;

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
				return _entity.Displacement.CoordinateAtStartOfTurn.ID == _action.supposedPositionAtActionStartID;
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
