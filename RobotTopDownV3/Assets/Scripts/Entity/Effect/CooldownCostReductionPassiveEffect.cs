using UnityEngine;

[CreateAssetMenu(fileName = "CooldownCostReduction", menuName = "ScriptableObject/PassiveEffect/CooldownCostReduction")]
public class CooldownCostReductionPassiveEffect : AEntityPassiveEffect
{
	public int reductionAmount = 1;


	/*public override void ApplyEffect ( Entity _entity )
	{
		if (_entity.Status.Contains(EntityStatusEnumID.Marked))
		{
			//apply here
		}
	}*/
}
