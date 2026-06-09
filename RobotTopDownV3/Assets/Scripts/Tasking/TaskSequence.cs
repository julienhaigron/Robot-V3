using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TaskSequence
{
    private List<Task> m_tasks = new();
    public List<Task> Tasks => m_tasks;

    public Task Append (Task _newTask)
	{
        m_tasks.Add(_newTask);
        return _newTask;
    }

    public void StartSequence ()
	{
        TaskManager.Instance.StartTasks(m_tasks);
    }
}
