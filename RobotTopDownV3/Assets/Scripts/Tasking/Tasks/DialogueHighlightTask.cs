using UnityEngine;
using System;
using System.Collections.Generic;

public class DialogueHighlightTask : Task
{
    private readonly DialogueData dialogue;
    private readonly string[] highlightZoneIDs;
    private readonly BaseButton overrideButton;
    private readonly bool completeOnEntitySelected;
    private readonly List<BaseButton> buttons = new();
    private readonly List<TutorialHighlightZone> shownZones = new();
    private TutoConsole tutoConsole;

    public DialogueHighlightTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, DialogueData _dialogue, string _highlightZoneID, BaseButton _button = null, bool _completeOnEntitySelected = false )
        : this(_description, _startPredicate, _dialogue, new string[] { _highlightZoneID }, _button, _completeOnEntitySelected)
    {
    }

    public DialogueHighlightTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, DialogueData _dialogue, string[] _highlightZoneIDs, BaseButton _button = null, bool _completeOnEntitySelected = false )
        : base(_description, _startPredicate)
    {
        this.dialogue = _dialogue;
        this.highlightZoneIDs = _highlightZoneIDs ?? new string[0];
        this.overrideButton = _button;
        this.completeOnEntitySelected = _completeOnEntitySelected;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        bool isInGame = _context.UI.currentPanel is InGamePanel;
        List<TutorialHighlightZone> zonesToShow = new();

        foreach (string highlightZoneID in highlightZoneIDs)
        {
            if (!FTUEManager.Instance.TryGetTutorialHighlightZone(highlightZoneID, out TutorialHighlightZone highlightZone))
            {
                Debug.LogWarning("No TutorialHighlightZone registered with ID \"" + highlightZoneID + "\", playing " + Description + " without it");
                continue;
            }

            if (!highlightZone.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("TutorialHighlightZone \"" + highlightZoneID + "\" is not in an active hierarchy, " + Description + " will not show it", highlightZone.gameObject);
                continue;
            }

            if (overrideButton == null && highlightZone.UsedButton != null)
                buttons.Add(highlightZone.UsedButton);

            zonesToShow.Add(highlightZone);
        }

        if (overrideButton != null)
            buttons.Add(overrideButton);

        if (zonesToShow.Count > 0 && (!isInGame || buttons.Count > 0))
        {
            TutorialHighlightZone.HideAllActive();

            foreach (TutorialHighlightZone zone in zonesToShow)
            {
                zone.Show();
                shownZones.Add(zone);
            }
        }

        if (completeOnEntitySelected)
            PlayerController.onEntitySelected += OnEntitySelected;

        if (isInGame)
        {
            foreach (BaseButton zoneButton in buttons)
                zoneButton.onClick += CompleteTask;

            tutoConsole = ((InGamePanel)_context.UI.currentPanel).TutoConsole;
            tutoConsole.PlayDialogue(dialogue, highlightZoneIDs);

            if (buttons.Count == 0)
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
        foreach (BaseButton zoneButton in buttons)
            if (zoneButton != null)
                zoneButton.onClick -= CompleteTask;

        if (completeOnEntitySelected)
            PlayerController.onEntitySelected -= OnEntitySelected;

        foreach (TutorialHighlightZone zone in shownZones)
            if (zone != null)
                zone.Hide();

        base.OnComplete();
    }
}
