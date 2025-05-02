using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public sealed class InGamePanel : AUIPanel
{

	[SerializeField] private UIEntityActionList m_entityActionList;
	public UIEntityActionList EntityActionList => m_entityActionList;

	[SerializeField] private BaseButton m_endPhaseButton;

	#region MonoBehaviour & Init

	private void Awake ()
	{
		m_endPhaseButton.onClick += OnClickEndPhaseBtn;
	}

	private void OnDestroy ()
	{
		m_endPhaseButton.onClick -= OnClickEndPhaseBtn;
	}

	public void Init () //add param
	{
		//Script Order : Awake - ShowPanelCR - l'Init
		//in case you need to get param from other UIPANEL
	}
	#endregion

	#region animation

	protected override IEnumerator ShowCR ( float _delay, bool _instant )
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
	}

	protected override IEnumerator HideCR ( float _delay, bool _instant )
	{
		CanClick = false;

		if (_delay != 0f)
			yield return new WaitForSecondsRealtime(_delay);

		//SetModalVisible(false, _instant ? 0f : m_modalFadeDuration);
		SetContainersVisible(false, _instant ? 0f : (m_overrideDurations ? m_hideDuration : null));

		if (!_instant)
			yield return m_hideDurationWFS;

		OnHideFinished();
	}

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}
	#endregion

	#region Callbacks

	private void OnClickEndPhaseBtn ()
	{
		if (GameManager.Instance.CurrentGameMode == GameManager.GameMode.Offline)
			TurnManager.Instance.EndInputPhase();
		else if (GameManager.Instance.CurrentGameMode == GameManager.GameMode.Online)
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
			/*TurnManager.RecordedAction[][] actionsToPlay = new TurnManager.RecordedAction[TurnManager.Instance.RecordedActions.Count][];
			int[] entitiesIDs = TurnManager.Instance.RecordedActions.Keys.ToArray();
			for (int i = 0; i < TurnManager.Instance.RecordedActions.Keys.Count; i++)
			{
				actionsToPlay[i] = TurnManager.Instance.RecordedActions[entitiesIDs[i]].ToArray();
			}*/
			OnlinePlayerInstance.Self.EndInputPhaseServerRPC(OnlinePlayerInstance.Self.OwnerClientId, actionsToSend.ToArray());
		}
	}

	#endregion
}
