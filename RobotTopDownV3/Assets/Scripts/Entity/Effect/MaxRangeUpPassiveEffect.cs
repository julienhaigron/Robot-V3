using UnityEngine;

[CreateAssetMenu(fileName = "MaxRangeUp", menuName = "ScriptableObject/PassiveEffect/MaxRangeUp")]
public class MaxRangeUpPassiveEffect : AEntityPassiveEffect
{
	public int rangeBoostAmount = 1;

	/*public override void ApplyEffect ( Entity _entity )
	{
		if (_entity.Status.Contains(EntityStatusEnumID.Marked))
		{
			//apply here
		}
	}*/
}
