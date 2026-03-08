using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "ScriptableObject/PassiveEffect/DamageUp")]
public class DamageUpPassiveEffect : AEntityPassiveEffect
{
	public int damageBoostAmount = 1;

	public enum ConditionType { Noone, DidNotMoveThisTurn, IsTargetMarked}
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
			case ConditionType.IsTargetMarked:
				return _targetEntity.Status.Contains(EntityStatusEnumID.Marked);
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
