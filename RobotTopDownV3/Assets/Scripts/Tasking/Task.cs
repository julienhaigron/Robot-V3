using UnityEngine;
using System;

public abstract class Task
{
	public Action<Task> onStarted;
	public Action<Task> onCompleted;

	public string Description { get; }
	public string SequenceID { get; set; }
	public bool IsLastTaskInSequence { get; set; }

	public bool IsCompleted { get; private set; }

	private Func<TaskManager.TaskContext, bool> m_startPredicate;
	private Func<TaskManager.TaskContext, bool> m_skipPredicate;

	private bool m_isPerforming = false;
	public bool IsPerforming => m_isPerforming;

	protected Task ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate )
	{
		Description = _description;
		m_startPredicate = _startPredicate;
		IsCompleted = false;
	}

	public Task SetSkipPredicate ( Func<TaskManager.TaskContext, bool> _skipPredicate )
	{
		m_skipPredicate = _skipPredicate;
		return this;
	}

	public bool TryStart ( TaskManager.TaskContext _context )
	{
		if (m_skipPredicate != null && m_skipPredicate(_context))
		{
			Skip();
			return false;
		}
		else if (m_startPredicate == null || m_startPredicate(_context))
		{
			Start(_context);
			return true;
		}
		else
			return false;
	}

	public void Start ( TaskManager.TaskContext _context )
	{
		m_isPerforming = true;

		OnStart(_context);
	}

	protected virtual void OnStart ( TaskManager.TaskContext _context )
	{
		//Debug.Log("Start task " + Description);
		onStarted?.Invoke(this);
	}

	protected void Skip ()
	{
		Complete();
	}

	protected void Complete ()
	{
		if (IsCompleted)
			return;

		m_isPerforming = false;
		IsCompleted = true;

		OnComplete();
	}

	protected virtual void OnComplete ()
	{
		if (!string.IsNullOrEmpty(SequenceID))
		{
			if (IsLastTaskInSequence)
				GameDatas.current.currentPlayerSave.sequencesProgressions[SequenceID] = -1;
			else
				GameDatas.current.currentPlayerSave.sequencesProgressions[SequenceID]++;
		}

		onCompleted?.Invoke(this);
	}
}