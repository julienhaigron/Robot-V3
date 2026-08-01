using UnityEngine;
using System;

public class WalkOnTIleTask : Task
{
    private readonly TileGroundType groundType;

    public WalkOnTIleTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, TileGroundType _groundType )
        : base(_description, _startPredicate)
    {
        this.groundType = _groundType;
    }

    private void OnAnyEntityWalkOnTile (Entity _walkingEntity)
	{
        if (_walkingEntity.PlayerOwnerID == GameManager.Instance.PlayerID && _walkingEntity.Displacement.Coordinates.GetTile().GroundType == groundType)
		{
            EntityDisplacementPlugin.onAnyEntityMovement -= OnAnyEntityWalkOnTile;
            Complete();
		}
	}

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        EntityDisplacementPlugin.onAnyEntityMovement += OnAnyEntityWalkOnTile;
    }
}
