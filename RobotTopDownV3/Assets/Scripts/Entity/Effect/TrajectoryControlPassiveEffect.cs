using UnityEngine;

[CreateAssetMenu(fileName = "TrajectoryControl", menuName = "ScriptableObject/PassiveEffect/TrajectoryControl")]
public class TrajectoryControlPassiveEffect : AEntityPassiveEffect
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
