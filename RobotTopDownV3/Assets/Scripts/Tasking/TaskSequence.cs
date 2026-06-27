using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TaskSequence
{
	public System.Action<TaskSequence> onCompleted;

	private string m_id;
	public string ID => m_id;

	private List<Task> m_tasks = new();
	public List<Task> Tasks => m_tasks;

	public Task CurrentTask => m_tasks[CurrentTaskIndex];

	private int CurrentTaskIndex => GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(m_id) 
		? GameDatas.current.currentPlayerSave.sequencesProgressions[m_id] : 0;

	public bool IsPerforming => CurrentTask.IsPerforming;
	public bool IsCompleted => m_tasks[^1].IsCompleted;

	public TaskSequence ( string _id )
	{
		m_id = _id;
	}

	public Task Append ( Task _newTask )
	{
		m_tasks.Add(_newTask);
		return _newTask;
	}

	public void Init ()
	{
		if (!GameDatas.current.currentPlayerSave.sequencesProgressions.ContainsKey(m_id))
		{
			GameDatas.current.currentPlayerSave.sequencesProgressions.Add(m_id, 0);
		}

		for (int i = CurrentTaskIndex; i < m_tasks.Count; i++)
		{
			m_tasks[i].SequenceID = ID;
			m_tasks[i].IsLastTaskInSequence = i + 1 == m_tasks.Count;
		}
		//TaskManager.Instance.StartSequence(this);
	}
}
