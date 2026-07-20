using UnityEngine;

[CreateAssetMenu(fileName = "DamageUp", menuName = "ScriptableObject/PassiveEffect/DamageUp")]
public class DamageUpPassiveEffect : AEntityPassiveEffect
{
	public float damageBoostAmount = .3f;

	/*public override void ApplyEffect ( Entity _entity )
	{
		if (_entity.Status.Contains(EntityStatusEnumID.Marked))
		{
			//apply here
		}
	}*/
}
