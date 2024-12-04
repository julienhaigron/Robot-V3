using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogConsole : SingletonPersistant<LogConsole>
{

	private Dictionary<LogEventType, List<Log>> m_logs = new();
	public Dictionary<LogEventType, List<Log>> Logs => m_logs;

	public enum LogEventType { InputPhase, PlayPhase }

	public static void AddLog (string _message, LogEventType _eventType )
	{
		Log newLog = new Log(_message, _eventType);

		if (!Instance.Logs.ContainsKey(_eventType))
			Instance.Logs.Add(_eventType, new());
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
			return eventType.ToString() + ": [" + recordTime.ToString("en_US") + "]" + message;
		}
	}

}
