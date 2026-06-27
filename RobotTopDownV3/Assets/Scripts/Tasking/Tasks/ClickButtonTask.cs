using UnityEngine;
using System;

public class ClickButtonTask : Task
{
    private readonly BaseButton button;

    public ClickButtonTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate,  BaseButton _button ) 
        : base(_description, _startPredicate)
    {
        this.button = _button;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        button.onClick += OnClicked;
        base.OnStart(_context);
    }

    private void OnClicked ()
    {
        Complete();
    }

    protected override void OnComplete ()
    {
        button.onClick -= OnClicked;
        base.OnComplete();
    }
}