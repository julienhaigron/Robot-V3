using UnityEngine;
using System;

public class OpenPanelTask<T> : Task where T : AUIPanel
{

    public OpenPanelTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate )
        : base(_description, _startPredicate)
    {

    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        _context.UI.OpenPanel<T>();
    }
}
