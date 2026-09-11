using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogConsolePopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_console;
	[SerializeField] private ScrollRect m_scrollRect;

	//[SerializeField] private 

	[SerializeField] private BaseButton m_forceWin;
	[SerializeField] private BaseButton m_forceDraw;
	[SerializeField] private BaseButton m_forceLoose;

	private List<LogConsole.LogEventType> m_visibleEventType = new();

	private void Awake ()
	{
		m_visibleEventType.Add(LogConsole.LogEventType.PreGame);
		m_visibleEventType.Add(LogConsole.LogEventType.InputPhase);
		m_visibleEventType.Add(LogConsole.LogEventType.ActionConflict);
		m_visibleEventType.Add(LogConsole.LogEventType.DebugSys);

		LogConsole.onLogAdded += OnLogAdded;
		m_forceWin.onClick += OnClickForceWin;
		m_forceDraw.onClick += OnClickForceDraw;
		m_forceLoose.onClick += OnClickForceLoose;
	}

	protected override void OnShowFinished ()
	{
		base.OnShowFinished();
		m_console.text = "";
		m_scrollRect.verticalScrollbar.value = 0f;

		foreach(LogConsole.Log log in LogConsole.Instance.AllLogs)
		{
			m_console.text += log.ToString();
		}
	}

	private void OnLogAdded ( LogConsole.Log _newLog )
	{
		if(m_visibleEventType.Contains(_newLog.eventType))
			m_console.text += _newLog.ToString();
	}

	private void OnClickForceWin ()
	{
		TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Win);
	}

	private void OnClickForceDraw ()
	{
		TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Draw);
	}

	private void OnClickForceLoose ()
	{
		TurnManager.Instance.EndLevel(EndLevelPopup.GameResult.Loose);
	}
}
