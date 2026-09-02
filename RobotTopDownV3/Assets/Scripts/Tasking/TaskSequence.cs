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
	public bool IsCompleted => /*m_tasks[^1].IsCompleted || */CurrentTaskIndex == -1;

	private System.Func<TaskManager.TaskContext, bool> m_skipPredicate;
	public System.Func<TaskManager.TaskContext, bool> SkipPredicate => m_skipPredicate;

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

		for (int i = Mathf.Max(0, CurrentTaskIndex); i < m_tasks.Count; i++)
		{
			m_tasks[i].SequenceID = ID;
			m_tasks[i].IsLastTaskInSequence = i + 1 == m_tasks.Count;

			if (i + 1 == m_tasks.Count)
			{
				m_tasks[i].onCompleted -= OnComplete;
				m_tasks[i].onCompleted += OnComplete;
			}
		}
		//TaskManager.Instance.StartSequence(this);
	}

	public bool TryStart (TaskManager.TaskContext _context)
	{
		return CurrentTask.TryStart(_context)
;	}

	public void Complete ()
	{
		GameDatas.current.currentPlayerSave.sequencesProgressions[m_id] = -1;
		onCompleted?.Invoke(this);
	}

	private void OnComplete (Task _task)
	{
		Complete();
	}

	public void SetSkipPredicate(System.Func<TaskManager.TaskContext, bool> _skipPredicate )
	{
		m_skipPredicate = _skipPredicate;
	}
}
