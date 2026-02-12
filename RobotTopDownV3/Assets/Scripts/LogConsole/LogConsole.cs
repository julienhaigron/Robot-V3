using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogConsole : SingletonPersistant<LogConsole>
{
	public static System.Action<Log> onLogAdded;

	private List<Log> m_allLogs = new();
	public List<Log> AllLogs => m_allLogs;

	private Dictionary<LogEventType, List<Log>> m_logs = new();
	public Dictionary<LogEventType, List<Log>> Logs => m_logs;

	public enum LogEventType { PreGame, Main, InputPhase, PlayPhase }

	public static void AddLog (string _message, LogEventType _eventType )
	{
		Log newLog = new Log(_message, _eventType);

		if (!Instance.Logs.ContainsKey(_eventType))
			Instance.Logs.Add(_eventType, new());

		Instance.Logs[_eventType].Add(newLog);
		Instance.AllLogs.Add(newLog);

		onLogAdded?.Invoke(newLog);
	}

	public class Log
	{
		public string message;
		public LogEventType eventType;
		public System.DateTime recordTime;

		public Log(string _message, LogEventType _eventType )
		{
			message = _message;
			eventType = _eventType;
			recordTime = System.DateTime.Now;
		}

		public override string ToString ()
		{
			return eventType.ToString() + " [" + recordTime.ToString("HH:mm") + "]: " + "<color=#" + ColorUtility.ToHtmlStringRGB(GameConfig.current.meta.colorsPerType[eventType]) + ">" + message + "</color>\n";
		}
	}

}
