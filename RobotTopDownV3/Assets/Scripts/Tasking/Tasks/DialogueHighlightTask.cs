using UnityEngine;
using System;

public class DialogueHighlightTask : Task
{
    private readonly DialogueData dialogue;
    private readonly string highlightZoneID;
    private readonly bool completeOnEntitySelected;
    private BaseButton button;
    private TutoConsole tutoConsole;
    private bool didShowZone;

    public DialogueHighlightTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, DialogueData _dialogue, string _highlightZoneID, BaseButton _button = null, bool _completeOnEntitySelected = false )
        : base(_description, _startPredicate)
    {
        this.dialogue = _dialogue;
        this.highlightZoneID = _highlightZoneID;
        this.button = _button;
        this.completeOnEntitySelected = _completeOnEntitySelected;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        bool isInGame = _context.UI.currentPanel is InGamePanel;

        if (FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
        {
            if (button == null)
                button = highlightZone.UsedButton;

            if (!highlightZone.gameObject.activeInHierarchy)
                Debug.LogWarning("TutorialHighlightZone \"" + highlightZoneID + "\" is not in an active hierarchy, " + Description + " will show no highlight", highlightZone.gameObject);
            else if (!isInGame || button != null)
            {
                highlightZone.Show();
                didShowZone = true;
            }
        }
        else
            Debug.LogWarning("No TutorialHighlightZone registered with ID \"" + highlightZoneID + "\", playing " + Description + " without it");

        if (completeOnEntitySelected)
            PlayerController.onEntitySelected += OnEntitySelected;

        if (isInGame)
        {
            if (button != null)
                button.onClick += CompleteTask;

            tutoConsole = ((InGamePanel)_context.UI.currentPanel).TutoConsole;
            tutoConsole.PlayDialogue(dialogue, highlightZoneID);

            if (button == null)
                Complete();

            return;
        }

        _context.Dialogue.PlayDialogue(dialogue, CompleteTask);
    }

    private void OnEntitySelected ( int? _entityID )
    {
        if (_entityID.HasValue)
            CompleteTask();
    }

    private void CompleteTask ()
    {
        if (IsCompleted)
            return;

        if (tutoConsole != null)
            tutoConsole.GoToNextLineOrDialogue();

        Complete();
    }

    protected override void OnComplete ()
    {
        if (button != null)
            button.onClick -= CompleteTask;

        if (completeOnEntitySelected)
            PlayerController.onEntitySelected -= OnEntitySelected;

        if (didShowZone && FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
            highlightZone.Hide();

        base.OnComplete();
    }
}
