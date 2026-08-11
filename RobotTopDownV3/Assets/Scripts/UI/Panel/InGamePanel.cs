using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;
using Sirenix.OdinInspector;

public sealed class InGamePanel : AUIPanel
{
	[Title("Actions")]
	[SerializeField] private EntityActionQueue m_actionQueue;
	public EntityActionQueue ActionQueue => m_actionQueue;

	[SerializeField] private SquadUnitDisplayList m_squadUnitDisplayList;
	[SerializeField] private UIEntityActionList m_entityActionList;
	public UIEntityActionList EntityActionList => m_entityActionList;

	[SerializeField] private BaseButton m_endPhaseButton;
	[SerializeField] private TextMeshProUGUI m_phaseTitleTmp;

	[Title("Console")]
	[SerializeField] private InGameLogConsole m_logConsole;

	//[SerializeField] private BaseButton m_validateTargetsBtn;
	[SerializeField] private TutoConsole m_tutoConsole;
	public TutoConsole TutoConsole => m_tutoConsole;

	[Title("Animations")]
	[SerializeField] private SerializableDictionary<RectTransform, AnchoredPositions> m_sectionPlacementsDictionary;
	[SerializeField] private float m_animationDuration = .5f;

	[Serializable]
	public class AnchoredPositions
	{
		public Vector2[] positions;
	}

	#region MonoBehaviour & Init

	private void Awake ()
	{
		TurnManager.onStartInputPhase += OnStartInputPhase;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		PlayerController.onEntitySelected += OnEntitySelected;
		m_endPhaseButton.onClick += OnClickEndPhaseBtn;
	}

	private void OnDestroy ()
	{
		TurnManager.onStartInputPhase = OnStartInputPhase;
		TurnManager.onEndInputPhase = OnEndInputPhase;
		PlayerController.onEntitySelected -= OnEntitySelected;
		m_endPhaseButton.onClick -= OnClickEndPhaseBtn;
	}

	public void Init ()
	{
		m_squadUnitDisplayList.Init();
		RefreshVisual(false, true);
		m_tutoConsole.Init();
		m_entityActionList.Init();
		m_actionQueue.Init();
	}

	public void RefreshVisual(bool _isEntitySelected, bool _isInstant )
	{
		foreach (RectTransform tfm in m_sectionPlacementsDictionary.Keys)
		{
			if (_isInstant)
				tfm.anchoredPosition = m_sectionPlacementsDictionary[tfm].positions[_isEntitySelected ? 0 : 1];
			else
				tfm.DOAnchorPos(m_sectionPlacementsDictionary[tfm].positions[_isEntitySelected ? 0 : 1], m_animationDuration).SetEase(Ease.OutExpo);
		}

		m_logConsole.SetConsoleVisibility(!_isEntitySelected);
	}

	#endregion

	#region animation

	protected override void OnHideStarted ()
	{
		if (m_logConsole.CurrentToggleConsoleBtnTween.IsActive())
			m_logConsole.CurrentToggleConsoleBtnTween.Kill();
		base.OnHideStarted();
	}

	#endregion

	#region Callbacks

	private void OnEntitySelected ( int? _entityID )
	{
		RefreshVisual(_entityID.HasValue, false);
	}

	private void OnStartInputPhase ()
	{
		m_phaseTitleTmp.text = "Input Phase";

		m_squadUnitDisplayList.Show(m_animationDuration);
		if(m_tutoConsole.AllDialogs.Count > 0)
			m_tutoConsole.Show(false);
		m_endPhaseButton.SetVisible(true, false);
	}

	private void OnEndInputPhase ()
	{
		m_phaseTitleTmp.text = "Play Phase";

		m_squadUnitDisplayList.Hide(m_animationDuration);
		m_tutoConsole.Hide(false);
		m_endPhaseButton.SetVisible(false, false);
	}

	private void OnClickEndPhaseBtn ()
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[0].Entities)
		{
			if (entity.Equipment.IsDead)
				continue;
			int remainingToken = TurnManager.Instance.RemainingActionToken[entity.ID];
			for (int i = 0; i < remainingToken; i++)
			{
				TurnManager.Instance.AddAction(entity.ID, EntityActionEnumID.Wait, Entity.EntityState.Patroling, null);
			}
		}

		TurnManager.onEndInputPhase?.Invoke();
		OnEntitySelected(null);

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
