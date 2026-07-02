using UnityEngine;
using System;

public class DialogueHighlightTask : Task
{
    private readonly DialogueData dialogue;
    private readonly string highlightZoneID;

    public DialogueHighlightTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, DialogueData _dialogue, string _highlightZoneID )
        : base(_description, _startPredicate)
    {
        this.dialogue = _dialogue;
        this.highlightZoneID = _highlightZoneID;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        TutorialHighlightZone highlightZone = FTUEManager.Instance.RegisterdTutorialHighlightZones[highlightZoneID];

        highlightZone.Show();
        highlightZone.onInteract += CompleteTask;
        _context.Dialogue.PlayDialogue(dialogue, CompleteTask);
    }

    private void CompleteTask ()
    {
        Complete();
    }

    protected override void OnComplete ()
    {
        TutorialHighlightZone highlightZone = FTUEManager.Instance.RegisterdTutorialHighlightZones[highlightZoneID];
        highlightZone.Hide();
        highlightZone.onInteract -= CompleteTask;
        base.OnComplete();
    }
}