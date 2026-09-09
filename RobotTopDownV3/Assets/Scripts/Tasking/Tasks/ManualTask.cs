using UnityEngine;
using System;

public class ManualTask : Task
{
    private readonly Action m_stuffToDo;
    private bool m_wasApplied = false;

    public ManualTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, Action _stuffToDo )
        : base(_description, _startPredicate)
    {
        m_stuffToDo = _stuffToDo;
    }

    public void Apply ()
    {
        if (m_wasApplied)
            return;

        m_wasApplied = true;
        m_stuffToDo?.Invoke();
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        Apply();
        Complete();
    }
}
