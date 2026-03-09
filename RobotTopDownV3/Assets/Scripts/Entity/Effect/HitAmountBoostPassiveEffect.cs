using UnityEngine;

[CreateAssetMenu(fileName = "HitAmountBoost", menuName = "ScriptableObject/PassiveEffect/HitAmountBoost")]
public class HitAmountBoostPassiveEffect : AEntityPassiveEffect
{
	public int hitAmountBoost = 1;

	/*public override void ApplyEffect ( Entity _entity )
	{
		if (_entity.Status.Contains(EntityStatusEnumID.Marked))
		{
			//apply here
		}
	}*/
}
