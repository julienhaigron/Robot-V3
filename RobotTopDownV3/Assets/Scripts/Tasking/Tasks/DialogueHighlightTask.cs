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

        //The in game console draws its own highlight from the id, it never needs the zone object.
        if (_context.UI.currentPanel is InGamePanel inGamePanel)
		{
            inGamePanel.TutoConsole.PlayDialogue(dialogue, null, highlightZoneID);
            Complete();
            return;
        }

        //Indexing the dictionary here used to throw out of TaskManager.Update and stall the whole tutorial
        //whenever a zone was not registered yet; the dialogue is worth playing even without its highlight.
        if (FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
        {
            highlightZone.Show();
            highlightZone.onInteract += CompleteTask;
        }
        else
            Debug.LogWarning("No TutorialHighlightZone registered with ID \"" + highlightZoneID + "\", playing " + Description + " without it");

        _context.Dialogue.PlayDialogue(dialogue, CompleteTask);
    }

    private void CompleteTask ()
    {
        if (IsCompleted)
            return;

        if (FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
            highlightZone.Hide();

        Complete();
    }

    /*protected override void OnComplete ()
    {
        TutorialHighlightZone highlightZone = FTUEManager.Instance.RegisterdTutorialHighlightZones[highlightZoneID];
        //highlightZone.Hide();
        //highlightZone.onInteract -= CompleteTask;
        base.OnComplete();
    }*/
}