using UnityEngine;

[CreateAssetMenu(fileName = "PreparationCostReduction", menuName = "ScriptableObject/PassiveEffect/PreparationCostReduction")]
public class PreparationCostReductionPassiveEffect : AEntityPassiveEffect
{
	public int reductionAmount = 1;

	public enum ConditionType { Noone, DidNotMoveThisTurn, IsFirstAttackThisTurn}
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
			case ConditionType.IsFirstAttackThisTurn:
				bool recordedCheck2 = TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked == -1
					|| TurnManager.Instance.TrackedEventsPerEntity[_entity.ID].firstTimeEntityAttacked >= _action.timeAtStart;
				bool liveCheck2 = !_entity.Equipment.DidAttackThisTurn;
				return recordedCheck2 && liveCheck2;
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
