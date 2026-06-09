using UnityEngine;

public class MoveEntityToTileTask : Task
{
    private readonly TileCoordinates targetTile;


    public MoveEntityToTileTask ( string _description, bool _canBeSkipped, TileCoordinates _targetTile )
        : base(_description, _canBeSkipped)
    {
        this.targetTile = _targetTile;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
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
    }
}