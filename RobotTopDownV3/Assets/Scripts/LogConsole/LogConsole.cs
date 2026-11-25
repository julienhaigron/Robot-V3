using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogConsole : SingletonPersistant<LogConsole>
{
	public static System.Action<Log> onLogAdded;
	public static System.Action onCleared;

	private List<Log> m_allLogs = new();
	public List<Log> AllLogs => m_allLogs;

	private Dictionary<LogEventType, List<Log>> m_logs = new();
	public Dictionary<LogEventType, List<Log>> Logs => m_logs;

	private Dictionary<string, LogDetails> m_logsDetails = new();
	public Dictionary<string, LogDetails> LogsDetails => m_logsDetails;

	private readonly HashSet<LogCard.Kind> m_loggedCardKinds = new();
	public bool HasLoggedCard ( LogCard.Kind _kind ) => m_loggedCardKinds.Contains(_kind);

	private int m_counter = 0;
	public int Counter
	{
		get
		{
			return m_counter;
		}
		set
		{
			m_counter = value;
		}
	}

	public enum LogEventType { PreGame, InputPhase, AICheck, ActionResolution, ActionConflict, DebugSys, AttackRoll, Damage, Status, UseWeapon, UseTool, Death, Round }

	public class LogDetails
	{
		public readonly string ID;
		public readonly LogCard card;

		public LogDetails ( LogCard _card )
		{
			ID = "log_" + Instance.Counter++;
			card = _card;
		}
	}

	public static void Clear ()
	{
		Instance.m_allLogs.Clear();
		Instance.m_logs.Clear();
		Instance.m_logsDetails.Clear();
		Instance.m_loggedCardKinds.Clear();
		onCleared?.Invoke();
	}

	public static void AddLog (string _message, LogEventType _eventType, LogDetails _details = null )
	{
		Log newLog = new Log(_message, _eventType, _details);
		Instance.Counter++;

		if (!Instance.Logs.ContainsKey(_eventType))
			Instance.Logs.Add(_eventType, new());

		Instance.Logs[_eventType].Add(newLog);
		Instance.AllLogs.Add(newLog);
		if (_details != null)
		{
			if (Instance.LogsDetails.ContainsKey(_details.ID))
				Instance.LogsDetails[_details.ID] = _details;
			else
				Instance.LogsDetails.Add(_details.ID, _details);

			Instance.m_loggedCardKinds.Add(_details.card.kind);
			foreach (LogCard step in _details.card.steps)
				Instance.m_loggedCardKinds.Add(step.kind);
		}

		onLogAdded?.Invoke(newLog);
	}

	[System.Serializable]
	public class Log
	{
		public string message;
		public LogEventType eventType;
		public System.DateTime recordTime;
		public LogDetails details;

		public Log(string _message, LogEventType _eventType, LogDetails _details = null )
		{
			message = _message;
			eventType = _eventType;
			recordTime = System.DateTime.Now;
			details = _details;
		}

		public override string ToString ()
		{
			return Format(true);
		}

		public string Format ( bool _withDetailLink )
		{
			string colored = "<color=#" + ColorUtility.ToHtmlStringRGB(GameConfig.current.meta.colorsPerType[eventType]) + ">" + message + "</color>";
			if (eventType == LogEventType.Round)
				return "<align=center>" + colored + "</align>\n";
			if (details == null)
				return "- " + eventType + ": " + colored + "\n";

			return _withDetailLink ? "- <link=" + details.ID + ">" + colored + "</link>\n" : "- " + colored + "\n";
		}
	}

}
