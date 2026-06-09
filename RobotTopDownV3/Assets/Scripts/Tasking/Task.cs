using UnityEngine;
using System;

public abstract class Task
{
    public event Action<Task> onStarted;
    public event Action<Task> onCompleted;

    public string Description { get; }

    public bool IsCompleted { get; private set; }

    public bool CanBeSkipped;

    protected Task ( string _description, bool _canBeSkipped )
    {
        Description = _description;
        CanBeSkipped = _canBeSkipped;
    }

    public void Start ( TaskManager.TaskContext _context )
    {
        onStarted?.Invoke(this);

        OnStart(_context);
    }

    protected abstract void OnStart ( TaskManager.TaskContext _context );

    protected void Complete ()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        OnComplete();

        onCompleted?.Invoke(this);
    }

    protected virtual void OnComplete ()
    {
    }
}