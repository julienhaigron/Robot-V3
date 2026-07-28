using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveStatus", menuName = "ScriptableObject/PassiveEffect/RemoveStatus")]
public class RemoveStatusPassiveEffect : AEntityPassiveEffect
{
	public bool doApplyToSelf = false;
	public bool doApplyToTile = false;
	public EntityStatusEnumID statusRemoved;


	public override void ApplyEffect ( Entity _entity, Entity _targetEntity, PassiveEffectContainer _effectContainer )
	{
		_targetEntity.RemoveStatus(statusRemoved);
		base.ApplyEffect(_entity, _targetEntity, _effectContainer);
	}

	public override void ApplyEffect(Tile _tile )
	{
		_tile.RemoveStatus(statusRemoved);
	}
}
