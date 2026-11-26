using UnityEngine;
using DG.Tweening;

public class ModActionButton : ActionButton
{
	[SerializeField] private ChargeGaugeGraphic m_chargeRing;
	[SerializeField] private float m_fullPulseAlpha = .4f;
	[SerializeField] private float m_fullPulseDuration = .8f;

	private SpecialAbilityState m_watchedAbility;
	private Tween m_fullPulse;

	private static Entity SelectedEntity => PlayerController.Instance == null ? null : PlayerController.Instance.SelectedEntity;

	private bool IsSpecialAbility => GameAssets.current.game.entityActionsData.TryGetValue(m_actionType, out EntityActionData data) && data.IsSpecialAbility;

	private void OnEnable ()
	{
		TurnManager.onSpecialAbilityPlanChanged += RefreshVisual;
		TurnManager.onActionTargetsChanged += RefreshVisual;
		EntityActionDisplay.onSelect += OnActionDisplaySelected;
	}

	private void OnDisable ()
	{
		TurnManager.onSpecialAbilityPlanChanged -= RefreshVisual;
		TurnManager.onActionTargetsChanged -= RefreshVisual;
		EntityActionDisplay.onSelect -= OnActionDisplaySelected;
		Watch(null);
		SetFullPulse(false);
	}

	private void SetFullPulse ( bool _isPulsing )
	{
		if (_isPulsing == m_fullPulse.IsActive())
			return;

		m_fullPulse?.Kill();
		m_fullPulse = null;
		m_chargeRing.canvasRenderer.SetAlpha(1f);
		if (_isPulsing)
			m_fullPulse = DOTween.To(() => m_chargeRing.canvasRenderer.GetAlpha(), _alpha => m_chargeRing.canvasRenderer.SetAlpha(_alpha), m_fullPulseAlpha, m_fullPulseDuration)
				.SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
	}

	private void Watch ( SpecialAbilityState _ability )
	{
		if (m_watchedAbility == _ability)
			return;

		if (m_watchedAbility != null)
			m_watchedAbility.onChanged -= RefreshVisual;
		m_watchedAbility = _ability;
		if (m_watchedAbility != null)
			m_watchedAbility.onChanged += RefreshVisual;
	}

	private void OnActionDisplaySelected ( EntityActionDisplay _display, bool _isModAction )
	{
		if (IsSpecialAbility)
			RefreshVisual();
	}

	protected override void Activate ()
	{
		if (!IsSpecialAbility)
		{
			base.Activate();
			return;
		}

		if (SelectedEntity != null)
			TurnManager.Instance.ToggleSpecialAbility(SelectedEntity);
	}

	protected override void SelectActionInTurnManager ()
	{
		TurnManager.Instance.SetCurrentModActionSelected(m_actionType, m_linkedEquipmentData, true);
	}

	protected override void RefreshVisual ()
	{
		Entity entity = IsSpecialAbility ? SelectedEntity : null;
		SpecialAbilityState ability = entity == null ? null : entity.SpecialAbility;
		Watch(ability);
		m_chargeRing.gameObject.SetActive(ability != null);
		SetFullPulse(ability != null && ability.IsFull);

		if (!IsSpecialAbility)
		{
			base.RefreshVisual();
			return;
		}

		if (ability == null || TurnManager.Instance == null)
			return;

		m_chargeRing.Show(ability, TurnManager.Instance.AbsoluteTick(TurnManager.currentTick));
		SetInteractability(TurnManager.Instance.CanPlanSpecialAbility(entity, out _));
		m_selectionOutline.SetActive(IsHighlighted(entity));
	}

	private static bool IsHighlighted ( Entity _entity )
	{
		EntityActionDisplay display = EntityActionDisplay.SelectedDisplay;
		return display != null
			? display.RecordedAction.performingEntityID == _entity.ID && display.RecordedAction.freeAction is SpecialAbilityAction
			: TurnManager.Instance.IsSpecialAbilityPending(_entity.ID);
	}

	protected override SpecialAbilityState TooltipAbility => IsSpecialAbility && SelectedEntity != null ? SelectedEntity.SpecialAbility : null;

	protected override string GetTooltipHint ( ActionTooltipContent _content )
	{
		if (!IsSpecialAbility || SelectedEntity == null)
			return base.GetTooltipHint(_content);

		TurnManager.Instance.CanPlanSpecialAbility(SelectedEntity, out LocalizationKey reason);
		return string.Format(LocalizationManager.Instance.Get(reason), _content.CarrierTypeLabel);
	}
}
