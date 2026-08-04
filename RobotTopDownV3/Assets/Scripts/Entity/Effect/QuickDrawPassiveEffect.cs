using UnityEngine;

[CreateAssetMenu(fileName = "QuickDraw", menuName = "ScriptableObject/PassiveEffect/QuickDraw")]
public class QuickDrawPassiveEffect : AEntityPassiveEffect
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
