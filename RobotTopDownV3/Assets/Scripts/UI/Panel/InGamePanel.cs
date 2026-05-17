using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public sealed class InGamePanel : AUIPanel
{

	[SerializeField] private UIEntityActionList m_entityActionList;
	public UIEntityActionList EntityActionList => m_entityActionList;

	[SerializeField] private BaseButton m_endPhaseButton;
	[SerializeField] private TextMeshProUGUI m_phaseTitleTmp;

	[SerializeField] private RectTransform m_consoleParent;
	[SerializeField] private TextMeshProUGUI m_consoleTMP;
	[SerializeField] private ScrollRect m_scrollRect;
	[SerializeField] private BaseButton m_toggleDisplayConsoleBtn;
	[SerializeField] private float m_consoleExpandedHeight = 400f;
	[SerializeField] private float m_consoleCollapsedHeight = 80f;
	[SerializeField] private float m_duration = 0.3f;
	[SerializeField] private List<LogConsole.LogEventType> m_visibleEventType;

	private bool m_isConsoleExpanded = true;
	private Tween m_currentToggleConsoleBtnTween;

	#region MonoBehaviour & Init

	private void Awake ()
	{
		TurnManager.onStartInputPhase += OnStartInputPhase;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		TurnManager.onEndLevel += OnEndLevel;
		m_endPhaseButton.onClick += OnClickEndPhaseBtn;

		LogConsole.onLogAdded += OnLogAdded;
		m_toggleDisplayConsoleBtn.onClick += OnClickToggleDisplayConsoleBtn;
	}

	private void OnDestroy ()
	{
		TurnManager.onStartInputPhase = OnStartInputPhase;
		TurnManager.onEndInputPhase = OnEndInputPhase;
		m_endPhaseButton.onClick -= OnClickEndPhaseBtn;
		TurnManager.onEndLevel -= OnEndLevel;
		m_toggleDisplayConsoleBtn.onClick -= OnClickToggleDisplayConsoleBtn;
		LogConsole.onLogAdded -= OnLogAdded;
	}

	public void Init () //add param
	{
		//Script Order : Awake - ShowPanelCR - l'Init
		//in case you need to get param from other UIPANEL
	}
	#endregion

	#region animation

	/*protected override IEnumerator ShowCR ( float _delay, bool _instant )
	{
		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		//SetModalVisible(true, _instant ? 0f : m_modalFadeDuration);
		SetContainersVisible(true, _instant ? 0f : (m_overrideDurations ? m_showDuration : null));

		if (!_instant)
			yield return m_showDurationWFS;

		OnShowFinished();
	}

	protected override void OnShowFinished ()
	{
		CanClick = true;
	}*/

	/*protected override IEnumerator HideCR ( float _delay, bool _instant )
	{
		CanClick = false;

		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		//SetModalVisible(false, _instant ? 0f : m_modalFadeDuration);
		SetContainersVisible(false, _instant ? 0f : (m_overrideDurations ? m_hideDuration : null));

		if (!_instant)
			yield return m_hideDurationWFS;

		OnHideFinished();
	}*/

	/*protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}*/

	protected override void OnHideStarted ()
	{
		if (m_currentToggleConsoleBtnTween.IsActive())
			m_currentToggleConsoleBtnTween.Kill();
		base.OnHideStarted();
	}

	#endregion

	#region Callbacks

	private void OnLogAdded ( LogConsole.Log _newLog )
	{
		if(m_visibleEventType.Contains(_newLog.eventType))
			m_consoleTMP.text += _newLog.ToString();
	}

	private void OnEndLevel ()
	{
		m_consoleTMP.text = "";
	}

	private void OnClickToggleDisplayConsoleBtn ()
	{
		m_isConsoleExpanded = !m_isConsoleExpanded;
		float targetHeight = m_isConsoleExpanded
			? m_consoleExpandedHeight
			: m_consoleCollapsedHeight;

		m_currentToggleConsoleBtnTween?.Kill();

		m_currentToggleConsoleBtnTween = m_consoleParent.DOSizeDelta(new Vector2(m_consoleParent.sizeDelta.x, targetHeight), m_duration)
			.SetEase(Ease.OutCubic);
	}

	private void OnStartInputPhase ()
	{
		m_phaseTitleTmp.text = "Input Phase";
	}

	private void OnEndInputPhase ()
	{
		m_phaseTitleTmp.text = "Play Phase";
	}

	private void OnClickEndPhaseBtn ()
	{

		foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[0].Entities)
		{
			if (entity.Equipment.IsDead)
				continue;

			for (int i = TurnManager.Instance.RemainingActionToken[entity.ID]; i < GameConfig.current.game.actionTokenPerRound; i++)
			{
				TurnManager.Instance.AddAction(entity.ID, EntityActionEnumID.Wait, Entity.EntityState.Guarding, null);
			}
		}

		TurnManager.onEndInputPhase?.Invoke();

		if (!GameManager.Instance.IsOnline)
			TurnManager.Instance.EndInputPhase();
		else
		{
			List<TurnManager.RecordedEntityActionsContainer> actionsToSend = new();

			foreach (var kvp in TurnManager.Instance.RecordedActions)
			{
				actionsToSend.Add(new TurnManager.RecordedEntityActionsContainer
				{
					entityId = kvp.Key,
					actions = kvp.Value.ToArray()
				});
			}

			OnlinePlayerInstance.Self.EndInputPhaseServerRPC(OnlinePlayerInstance.Self.OwnerClientId, actionsToSend.ToArray());
		}
	}

	#endregion
}
