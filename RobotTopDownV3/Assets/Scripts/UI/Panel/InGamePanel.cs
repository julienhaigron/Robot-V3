using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public sealed class InGamePanel : AUIPanel
{

	[SerializeField] private UIEntityActionList m_entityActionList;
	public UIEntityActionList EntityActionList => m_entityActionList;

	[SerializeField] private BaseButton m_endPhaseButton;
	[SerializeField] private TextMeshProUGUI m_phaseTitleTmp;

	#region MonoBehaviour & Init

	private void Awake ()
	{
		TurnManager.onStartInputPhase += OnStartInputPhase;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		m_endPhaseButton.onClick += OnClickEndPhaseBtn;
	}

	private void OnDestroy ()
	{
		TurnManager.onStartInputPhase = OnStartInputPhase;
		TurnManager.onEndInputPhase = OnEndInputPhase;
		m_endPhaseButton.onClick -= OnClickEndPhaseBtn;
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
	#endregion

	#region Callbacks

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
				TurnManager.Instance.AddAction(entity.ID, EntityActionEnumID.Wait, Entity.EntityState.Guarding);
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
