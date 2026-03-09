using UnityEngine;

[CreateAssetMenu(fileName = "MaxTargetUp", menuName = "ScriptableObject/PassiveEffect/MaxTargetUp")]
public class MaxTargetUpPassiveEffect : AEntityPassiveEffect
{
	public int targetBoostAmount = 1;

	/*public override void ApplyEffect ( Entity _entity )
	{
		if (_entity.Status.Contains(EntityStatusEnumID.Marked))
		{
			//apply here
		}
	}*/
}
