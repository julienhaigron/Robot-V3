using UnityEngine;

[CreateAssetMenu(fileName = "RemoveStatus", menuName = "ScriptableObject/PassiveEffect/RemoveStatus")]
public class RemoveStatusPassiveEffect : AEntityPassiveEffect
{
	public bool doApplyToSelf = false;
	public bool doApplyToTile = false;
	public EntityStatusEnumID statusRemoved;


	public override void ApplyEffect ( Entity _entity, Entity _targetEntity )
	{
		if(doApplyToSelf)
			_entity.RemoveStatus(statusRemoved);
		else
			_targetEntity.RemoveStatus(statusRemoved);
	}

	public override void ApplyEffect(Tile _tile )
	{
		_tile.RemoveStatus(statusRemoved);
	}
}
