using UnityEngine;

[CreateAssetMenu(fileName = "PreparationCostReduction", menuName = "ScriptableObject/PassiveEffect/PreparationCostReduction")]
public class PreparationCostReductionPassiveEffect : AEntityPassiveEffect
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
