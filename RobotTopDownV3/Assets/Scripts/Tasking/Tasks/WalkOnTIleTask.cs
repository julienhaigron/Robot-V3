using UnityEngine;
using System;

public class WalkOnTileTask : Task
{
    private readonly TileGroundType groundType;

    public WalkOnTileTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, TileGroundType _groundType )
        : base(_description, _startPredicate)
    {
        this.groundType = _groundType;
    }

    private void OnAnyEntityWalkOnTile (Entity _walkingEntity)
	{
        if (_walkingEntity.OwnerID == GameManager.Instance.PlayerID && _walkingEntity.Displacement.Coordinates.GetTile().GroundType == groundType)
		{
            Complete();
		}
	}

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        EntityDisplacementPlugin.onAnyEntityMovement += OnAnyEntityWalkOnTile;
    }

    protected override void OnComplete ()
    {
        EntityDisplacementPlugin.onAnyEntityMovement -= OnAnyEntityWalkOnTile;
        base.OnComplete();
    }
}
