using UnityEngine;
using System;

public class ManualTask : Task
{
    private readonly Action m_stuffToDo;

    public ManualTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, Action _stuffToDo )
        : base(_description, _startPredicate)
    {
        m_stuffToDo = _stuffToDo;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        m_stuffToDo?.Invoke();
    }
}
