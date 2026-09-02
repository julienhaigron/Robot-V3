using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TaskManager : SingletonPersistant<TaskManager>
{
    private TaskContext m_context = null;
    public TaskContext Context => m_context;
    private List<TaskSequence> m_activeSequences = new();

    public class TaskContext
    {
        public TurnManager Turn;
        public PlayerController Player;
        public UIManager UI;
        public GridManager Grid;
        public DialogueManager Dialogue;
        public LogConsole Log;
        public GameManager Game;
    }

	private void Start ()
	{
        m_context = new()
        {
            Turn = TurnManager.Instance,
            Player = PlayerController.Instance,
            UI = UIManager.Instance,
            Grid = GridManager.Instance,
            Dialogue = DialogueManager.Instance,
            Log = LogConsole.Instance,
            Game = GameManager.Instance
        };
    }

	private void Update ()
	{
        if (m_context == null)
            return;

		foreach (TaskSequence seq in m_activeSequences.ToArray())
		{
			if (seq.IsCompleted)
			{
				m_activeSequences.Remove(seq);
				continue;
			}

			if (!seq.IsPerforming)
			{
				if (!seq.TryStart(m_context))
                {
                    if (seq.SkipPredicate != null && seq.SkipPredicate(m_context))
                        seq.Complete();
                }
			}
			else if (seq.SkipPredicate != null && seq.SkipPredicate(m_context))
				seq.Complete();
		}
	}

    public void StopAndMarkAsCompletedSequence(string _sequenceID )
	{
        foreach (TaskSequence seq in m_activeSequences.ToArray())
        {
            if (string.Equals(seq.ID, _sequenceID))
            {
                if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(seq.ID))
                    GameDatas.current.currentPlayerSave.sequencesProgressions.Add(seq.ID, -1);
                GameDatas.current.currentPlayerSave.sequencesProgressions[seq.ID] = -1;
                m_activeSequences.Remove(seq);
                return;
			}
        }

        if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(_sequenceID))
            GameDatas.current.currentPlayerSave.sequencesProgressions.Add(_sequenceID, -1);
        else
            GameDatas.current.currentPlayerSave.sequencesProgressions[_sequenceID] = -1;
    }

	public void StartSequence(TaskSequence _sequence )
	{
        _sequence.Init();

        if (!m_activeSequences.Contains(_sequence))
            m_activeSequences.Add(_sequence);
    }

}
