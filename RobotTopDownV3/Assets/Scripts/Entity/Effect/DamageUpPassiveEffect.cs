using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "ScriptableObject/PassiveEffect/DamageUp")]
public class DamageUpPassiveEffect : AEntityPassiveEffect
{
	public int damageBoostAmount = 1;

	public enum ConditionType { Noone, DidNotMoveThisTurn, IsTargetMarked}
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
