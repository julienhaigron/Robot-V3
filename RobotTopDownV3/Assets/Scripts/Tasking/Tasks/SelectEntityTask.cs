using UnityEngine;
using System;

public class SelectEntityTask : Task
{
    public int entityID;

    public SelectEntityTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, int _entityID )
        : base(_description, _startPredicate)
    {
        this.entityID = _entityID;
    }

    private void OnEntitySelected (int? _entityID)
	{
        if (entityID == -1 || (_entityID.HasValue && _entityID.Value == entityID))
		{
            PlayerController.onEntitySelected -= OnEntitySelected;
            Complete();
		}
	}

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        PlayerController.onEntitySelected += OnEntitySelected;
    }
}
