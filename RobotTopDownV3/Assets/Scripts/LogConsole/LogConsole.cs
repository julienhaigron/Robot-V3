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

	private Dictionary<string, LogDetails> m_logsDetails = new();
	public Dictionary<string, LogDetails> LogsDetails => m_logsDetails;

	public enum LogEventType { PreGame, InputPhase, AICheck, AttackResolution, ActionConflict, DebugSys }

	[System.Serializable]
	public class LogDetails
	{
		public string ID;
		public string title;
		public string description;

		public LogDetails(string _ID, string _title, string _description )
		{
			ID = _ID;
			title = _title;
			description = _description;
		}

		public struct EntityStatAndRollInfo
		{
			public float baseDef;
			//public int
		}
	}

	public static void AddLog (string _message, LogEventType _eventType, LogDetails _details = null )
	{
		Log newLog = new Log(_message, _eventType, _details);

		if (!Instance.Logs.ContainsKey(_eventType))
			Instance.Logs.Add(_eventType, new());

		Instance.Logs[_eventType].Add(newLog);
		Instance.AllLogs.Add(newLog);
		if (_details != null)
			Instance.LogsDetails.Add(_details.ID, _details);

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
			switch (eventType)
			{
				case LogEventType.AttackResolution:
					return "<link=" + details.ID + ">" + "<color=#" + ColorUtility.ToHtmlStringRGB(GameConfig.current.meta.colorsPerType[eventType]) + ">" + message + "</color></link>\n";
				default:
					return eventType.ToString() + " [" + recordTime.ToString("HH:mm") + "]: " + "<color=#" + ColorUtility.ToHtmlStringRGB(GameConfig.current.meta.colorsPerType[eventType]) + ">" + message + "</color>\n";
			}

		}
	}

}
