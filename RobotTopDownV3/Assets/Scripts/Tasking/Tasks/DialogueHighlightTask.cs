using UnityEngine;

public class DialogueHighlightTask : Task
{
    private readonly DialogueData dialogue;
    private readonly TutorialHighlightZone highlightZone;

    private DialogueManager dialogueManager;

    public DialogueHighlightTask ( string description, bool _canBeSkipped, DialogueData dialogue, TutorialHighlightZone highlightZone )
        : base(description, _canBeSkipped)
    {
        this.dialogue = dialogue;
        this.highlightZone = highlightZone;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        dialogueManager = _context.DialogueManager;

        highlightZone.Show();
        highlightZone.onInteract += CompleteTask;
        dialogueManager.PlayDialogue(dialogue, CompleteTask);
    }

    private void CompleteTask ()
    {
        Complete();
    }

    protected override void OnComplete ()
    {
        highlightZone.Hide();
        highlightZone.onInteract -= CompleteTask;
    }
}