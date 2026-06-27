using UnityEngine;
using System;

public class MoveEntityToTileTask : Task
{
    private readonly TileCoordinates targetTile;


    public MoveEntityToTileTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, TileCoordinates _targetTile )
        : base(_description, _startPredicate)
    {
        this.targetTile = _targetTile;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        EntityDisplacementPlugin.onAnyEntityMovement += HandleEntityMoved;
    }

    private void HandleEntityMoved ( Entity _entity )
    {
        if (!_entity.IsAlliedTo(GameManager.Instance.PlayerID))
            return;

        if (_entity.Displacement.Coordinates.IsEqualTo(targetTile))
            return;

        Complete();
    }

    protected override void OnComplete ()
    {
        EntityDisplacementPlugin.onAnyEntityMovement -= HandleEntityMoved;
        base.OnComplete();
    }
}