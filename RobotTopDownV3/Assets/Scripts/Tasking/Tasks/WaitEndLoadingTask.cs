using UnityEngine;
using System;

public class WaitEndLoadingTask : Task
{

    public WaitEndLoadingTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate )
        : base(_description, _startPredicate)
    {

    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        LoadingManager.onStartFadeOut += OnStartEndLoading;
    }

    private void OnStartEndLoading ()
	{
        LoadingManager.onStartFadeOut += OnStartEndLoading;
        Complete();
	}
}
