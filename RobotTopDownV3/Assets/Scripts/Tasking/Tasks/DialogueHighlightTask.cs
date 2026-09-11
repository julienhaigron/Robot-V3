using UnityEngine;
using System;

public class DialogueHighlightTask : Task
{
    private readonly DialogueData dialogue;
    private readonly string highlightZoneID;
    private BaseButton button;
    private TutoConsole tutoConsole;

    public DialogueHighlightTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, DialogueData _dialogue, string _highlightZoneID, BaseButton _button = null )
        : base(_description, _startPredicate)
    {
        this.dialogue = _dialogue;
        this.highlightZoneID = _highlightZoneID;
        this.button = _button;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        bool isInGame = _context.UI.currentPanel is InGamePanel;

        if (FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
        {
            if (button == null)
                button = highlightZone.UsedButton;

            if (!isInGame || button != null)
                highlightZone.Show();
        }
        else
            Debug.LogWarning("No TutorialHighlightZone registered with ID \"" + highlightZoneID + "\", playing " + Description + " without it");

        if (button != null)
            button.onClick += CompleteTask;

        if (isInGame)
        {
            tutoConsole = ((InGamePanel)_context.UI.currentPanel).TutoConsole;
            tutoConsole.PlayDialogue(dialogue, highlightZoneID);

            if (button == null)
                Complete();

            return;
        }

        if (button != null)
            _context.Dialogue.PlayDialogue(dialogue, null);
        else
            _context.Dialogue.PlayDialogue(dialogue, CompleteTask);
    }

    private void CompleteTask ()
    {
        if (IsCompleted)
            return;

        if (tutoConsole != null)
            tutoConsole.GoToNextLineOrDialogue();

        if (FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
            highlightZone.Hide();

        Complete();
    }

    protected override void OnComplete ()
    {
        if (button != null)
            button.onClick -= CompleteTask;

        base.OnComplete();
    }
}
