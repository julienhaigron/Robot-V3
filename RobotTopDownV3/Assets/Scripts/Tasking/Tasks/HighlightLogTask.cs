using UnityEngine;
using System;

public class HighlightLogTask : Task
{
    private readonly LogConsole.LogEventType eventType;
    private readonly bool isHighlighted;

    public HighlightLogTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, LogConsole.LogEventType _eventType, bool _isHighlighted )
        : base(_description, _startPredicate)
    {
        this.eventType = _eventType;
        this.isHighlighted = _isHighlighted;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        if (_context.UI.currentPanel is InGamePanel inGamePanel)
            inGamePanel.LogConsole.SetEventTypeHighlighted(eventType, isHighlighted);
        else
            Debug.LogWarning("No InGamePanel opened, " + Description + " did nothing");

        Complete();
    }
}
