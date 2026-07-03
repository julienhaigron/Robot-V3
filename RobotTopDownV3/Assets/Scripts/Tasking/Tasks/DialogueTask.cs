using UnityEngine;
using System;

public class DialogueTask : Task
{
    private readonly DialogueData dialogue;

    public DialogueTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, DialogueData _dialogue )
        : base(_description, _startPredicate)
    {
        this.dialogue = _dialogue;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);
        if (_context.UI.currentPanel is InGamePanel inGamePanel)
            inGamePanel.TutoConsole.PlayDialogue(dialogue, Complete);
        else
            _context.Dialogue.PlayDialogue(dialogue, Complete);
    }
}
