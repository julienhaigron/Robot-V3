using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class InGameLogConsole : MonoBehaviour
{
	[SerializeField] private RectTransform m_consoleParent;
	//[SerializeField] private TextMeshProUGUI m_consoleTMP;
	[SerializeField] private ScrollRect m_scrollRect;
	[SerializeField] private BaseButton m_toggleDisplayConsoleBtn;
	[SerializeField] private float m_consoleExpandedXPos = 400f;
	[SerializeField] private float m_consoleCollapsedXPos = 80f;
	[SerializeField] private float m_duration = 0.3f;
	[SerializeField] private List<LogConsole.LogEventType> m_visibleEventType;
	
	[SerializeField] private PoolData m_logTmpPoolData;
	[SerializeField] private Transform m_content;
	[SerializeField] private int m_maxVisibleLogs = 150;

	private readonly Queue<LogPoolElement> m_visibleLogs = new();

	private bool m_isConsoleExpanded = true;
	private Tween m_currentToggleConsoleBtnTween;
	public Tween CurrentToggleConsoleBtnTween => m_currentToggleConsoleBtnTween;

	private void Awake ()
	{
		TurnManager.onEndLevel += OnEndLevel;
		LogConsole.onLogAdded += OnLogAdded;
		m_toggleDisplayConsoleBtn.onClick += OnClickToggleDisplayConsoleBtn;
	}

	private void OnDestroy ()
	{
		TurnManager.onEndLevel -= OnEndLevel;
		m_toggleDisplayConsoleBtn.onClick -= OnClickToggleDisplayConsoleBtn;
		LogConsole.onLogAdded -= OnLogAdded;
	}

	/*private void OnLogAdded ( LogConsole.Log _newLog )
	{
		//todo : use this
		//LogPoolElement logElem = ObjectsPooling.GetElement(m_logTmpPoolData) as LogPoolElement;
		//logElem.Init("content");
		
		if (m_visibleEventType.Contains(_newLog.eventType))
			m_consoleTMP.text += _newLog.ToString();
	}*/
	private void OnLogAdded ( LogConsole.Log _newLog )
	{
		if (!m_visibleEventType.Contains(_newLog.eventType))
			return;

		LogPoolElement elem = ObjectsPooling.GetElement(m_logTmpPoolData) as LogPoolElement;
		elem.transform.SetParent(m_content, false);
		elem.Init(_newLog.ToString());
		m_visibleLogs.Enqueue(elem);

		if (m_visibleLogs.Count > m_maxVisibleLogs)
		{
			LogPoolElement oldest = m_visibleLogs.Dequeue();
			oldest.Discard();
		}

		LayoutRebuilder.MarkLayoutForRebuild(m_content as RectTransform);
		m_scrollRect.verticalNormalizedPosition = 0f;
	}

	private void OnEndLevel ()
	{
		while (m_visibleLogs.Count > 0)
			m_visibleLogs.Dequeue().Discard();
	}

	public void SetConsoleVisibility ( bool _isVisible )
	{
		if (m_isConsoleExpanded == _isVisible)
			return;

		m_isConsoleExpanded = _isVisible;
		m_toggleDisplayConsoleBtn.SetVisible(!m_isConsoleExpanded || PlayerController.Instance.SelectedEntity != null, true);
		float targetXPos = m_isConsoleExpanded ? m_consoleExpandedXPos : m_consoleCollapsedXPos;

		m_currentToggleConsoleBtnTween?.Kill();
		m_currentToggleConsoleBtnTween = m_consoleParent.DOAnchorPos(new Vector2(targetXPos, m_consoleParent.anchoredPosition.y), m_duration)
			.SetEase(Ease.OutCubic);
	}

	private void OnClickToggleDisplayConsoleBtn ()
	{
		SetConsoleVisibility(!m_isConsoleExpanded);
	}
}
