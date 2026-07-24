using UnityEngine;

[CreateAssetMenu(fileName = "MaxAoERangeUp", menuName = "ScriptableObject/PassiveEffect/MaxAoERangeUp")]
public class MaxAoERangeUpPassiveEffect : AEntityPassiveEffect
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
