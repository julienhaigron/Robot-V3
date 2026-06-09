using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TaskManager : SingletonPersistant<TaskManager>
{
    public event Action<Task> onTaskStarted;
    public event Action<Task> onTaskCompleted;
    public event Action onSequenceCompleted;

    private readonly TaskContext m_context;
    private readonly Queue<Task> m_tasks = new();
    private Task m_currentTask;

    public class TaskContext
    {
        public TurnManager TurnManager;
        public PlayerController Player;
        public UIManager UIManager;
        public GridManager GridManager;
        public DialogueManager DialogueManager;
    }

    public void StartTasks ( IEnumerable<Task> _tutorialTasks )
    {
        m_tasks.Clear();

        foreach (Task task in _tutorialTasks)
            m_tasks.Enqueue(task);

        StartNextTask();
    }

    private void StartNextTask ()
    {
        if (m_tasks.Count == 0)
        {
            m_currentTask = null;

            onSequenceCompleted?.Invoke();
            return;
        }

        m_currentTask = m_tasks.Dequeue();

        m_currentTask.onCompleted += OnCurrentTaskCompleted;

        onTaskStarted?.Invoke(m_currentTask);

        m_currentTask.Start(m_context);
    }

    private void OnCurrentTaskCompleted ( Task task )
    {
        task.onCompleted -= OnCurrentTaskCompleted;

        onTaskCompleted?.Invoke(task);

        StartNextTask();
    }
}
