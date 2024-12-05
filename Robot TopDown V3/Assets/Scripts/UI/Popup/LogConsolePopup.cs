using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LogConsolePopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_console;

	private List<LogConsole.LogEventType> m_visibleEventType = new();

	private void Awake ()
	{
		m_visibleEventType.Add(LogConsole.LogEventType.Main);
		m_visibleEventType.Add(LogConsole.LogEventType.InputPhase);
		m_visibleEventType.Add(LogConsole.LogEventType.PlayPhase);

		LogConsole.onLogAdded += OnLogAdded;
	}

	private void OnLogAdded ( LogConsole.Log _newLog )
	{
		m_console.text += _newLog.ToString();
	}
}
