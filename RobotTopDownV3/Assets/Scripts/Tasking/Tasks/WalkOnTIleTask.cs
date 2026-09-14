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
        if (IsPlayerEntityOnTargetGround(_walkingEntity))
		{
            Complete();
		}
	}

    private bool IsPlayerEntityOnTargetGround ( Entity _entity )
	{
        if (_entity == null || _entity.OwnerID != GameManager.Instance.PlayerID)
            return false;

        Tile currentTile = _entity.Displacement.Coordinates.GetTile();
        return currentTile != null && currentTile.GroundType == groundType;
	}

    private bool WasTargetGroundAlreadyReached ()
	{
        if (GridManager.Instance != null && GridManager.Instance.WasGroundTypeWalkedOnByPlayer(groundType))
            return true;

        if (GameManager.Instance == null || GameManager.Instance.PlayersEntityAnchor == null)
            return false;

        foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
            if (anchor == null)
                continue;

            foreach (Entity entity in anchor.Entities)
			{
                if (IsPlayerEntityOnTargetGround(entity))
                    return true;
			}
		}

        return false;
	}

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        if (WasTargetGroundAlreadyReached())
		{
            Complete();
            return;
		}

        EntityDisplacementPlugin.onAnyEntityMovement += OnAnyEntityWalkOnTile;
    }

    protected override void OnComplete ()
    {
        EntityDisplacementPlugin.onAnyEntityMovement -= OnAnyEntityWalkOnTile;
        base.OnComplete();
    }
}
