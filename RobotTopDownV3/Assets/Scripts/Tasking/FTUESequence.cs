using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FTUESequence
{
	private string m_id;
	public string ID => m_id;

	private List<TaskSequence> m_sequences = new();
	public List<TaskSequence> Sequences => m_sequences;

	public TaskSequence CurrentSequence => m_sequences[m_currentSequenceIndex];

	private int m_currentSequenceIndex = 0;
	public int CurrentSequenceIndex => m_currentSequenceIndex;

	public bool IsPerforming => CurrentSequence.IsPerforming;
	public bool IsCompleted => m_sequences[^1].IsCompleted;

	public FTUESequence ( string _id )
	{
		m_id = _id;
	}

	public TaskSequence Append ( TaskSequence _newSequence )
	{
		m_sequences.Add(_newSequence);
		return _newSequence;
	}

	public void Start ()
	{
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(ID))
		{
			m_currentSequenceIndex = 0;
			GameDatas.current.currentPlayerSave.sequencesProgressions.Add(ID, m_currentSequenceIndex);
		}
		else
			m_currentSequenceIndex = GameDatas.current.currentPlayerSave.sequencesProgressions[ID];

		for (int i = m_currentSequenceIndex; i < Sequences.Count; i++)
		{
			if (i + 1 == Sequences.Count)
				Sequences[i].onCompleted += OnEndFTUE;
			else
				Sequences[i].onCompleted += OnEndFTUESingleSequence;
		}

		TaskManager.Instance.StartSequence(Sequences[m_currentSequenceIndex]);
	}

	private void OnEndFTUESingleSequence ( TaskSequence _sequence )
	{
		GameDatas.current.currentPlayerSave.sequencesProgressions[_sequence.ID]++;
	}

	private void OnEndFTUE ( TaskSequence _sequence )
	{
		GameDatas.current.currentPlayerSave.sequencesProgressions[_sequence.ID] = -1;
	}
}
