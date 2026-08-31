using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FTUESequence
{
	private string m_id;
	public string ID => m_id;

	private List<TaskSequence> m_sequences = new();
	public List<TaskSequence> Sequences => m_sequences;

	public TaskSequence CurrentSequence => m_sequences[CurrentSequenceIndex];


	private int CurrentSequenceIndex => GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(m_id)
		? GameDatas.current.currentPlayerSave.sequencesProgressions[m_id] : 0;

	public bool IsPerforming => CurrentSequence.IsPerforming;
	public bool IsCompleted => m_sequences[^1].IsCompleted || CurrentSequenceIndex == -1;

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
			GameDatas.current.currentPlayerSave.sequencesProgressions.Add(ID, 0);
		}

		for (int i = CurrentSequenceIndex; i < Sequences.Count; i++)
		{
			if(Sequences[i].SkipPredicate != null && Sequences[i].SkipPredicate(TaskManager.Instance.Context))
			{
				OnEndFTUESingleSequence(Sequences[i]);
			}
			else if (i + 1 == Sequences.Count)
				Sequences[i].onCompleted += OnEndFTUE;
			else
				Sequences[i].onCompleted += OnEndFTUESingleSequence;
		}

		TaskManager.Instance.StartSequence(Sequences[CurrentSequenceIndex]);
	}

	private void OnEndFTUESingleSequence ( TaskSequence _sequence )
	{
		if(GameDatas.current.currentPlayerSave.sequencesProgressions[m_id] != -1)
			GameDatas.current.currentPlayerSave.sequencesProgressions[m_id]++;

		if(!IsCompleted)
			TaskManager.Instance.StartSequence(Sequences[CurrentSequenceIndex]);
	}

	private void OnEndFTUE ( TaskSequence _sequence )
	{
		GameDatas.current.currentPlayerSave.sequencesProgressions[m_id] = -1;
	}
}
