using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatus", menuName = "ScriptableObject/PassiveEffect/ApplyStatus")]
public class ApplyStatusPassiveEffect : AEntityPassiveEffect
{
	public EntityStatusEnumID statusApplied;
	public int duration = 1;

	public AEntityStatus Status => GameAssets.current.game.entityStatus[statusApplied];


	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		_targetEntity.AddStatus(statusApplied, duration);
		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}

	public override void ApplyEffect(Tile _tile )
	{
		_tile.AddStatus(statusApplied, duration);
	}
}
