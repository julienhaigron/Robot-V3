using UnityEngine;

public class DialogueTask : Task
{
    private readonly DialogueData dialogue;
    private DialogueManager dialogueManager;

    public DialogueTask ( string _description, bool _canBeSkipped, DialogueData _dialogue )
        : base(_description, _canBeSkipped)
    {
        this.dialogue = _dialogue;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        dialogueManager = _context.DialogueManager;
        dialogueManager.PlayDialogue(dialogue, Complete);
    }
}
