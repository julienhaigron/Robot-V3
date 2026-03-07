using UnityEngine;

[CreateAssetMenu(fileName = "CooldownCostReduction", menuName = "ScriptableObject/PassiveEffect/CooldownCostReduction")]
public class CooldownCostReductionPassiveEffect : AEntityPassiveEffect
{
	public int reductionAmount = 1;

	public enum ConditionType { Noone, DidNotMoveThisTurn}
	public ConditionType conditionType = ConditionType.Noone; 

	public override bool UseConditionPredicate ( Entity _entity, Entity _targetEntity )
	{
		switch (conditionType)
		{
			default:
			case ConditionType.Noone:
				return true;
			case ConditionType.DidNotMoveThisTurn:
				return !_entity.Displacement.DidMoveThisTurn;
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
