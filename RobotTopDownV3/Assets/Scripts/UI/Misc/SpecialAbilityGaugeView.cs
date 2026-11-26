using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SpecialAbilityGaugeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private ChargeGaugeGraphic m_gauge;
	[SerializeField] private TextMeshProUGUI m_valueTMP;
	[SerializeField] private Color m_chargedColor = new(.91f, .7f, .23f, 1f);
	[SerializeField] private Color m_emptyColor = new(.6f, .64f, .75f, 1f);
	[SerializeField] private Color m_activeColor = new(.18f, .9f, 1f, 1f);

	private Entity m_entity;
	private SpecialAbilityState m_ability;

	private void OnEnable ()
	{
		TurnManager.onNewRoundStart += Refresh;
		TurnManager.onStartInputPhase += Refresh;
		LocalizationManager.onLanguageChanged += Refresh;
	}

	private void OnDisable ()
	{
		TurnManager.onNewRoundStart -= Refresh;
		TurnManager.onStartInputPhase -= Refresh;
		LocalizationManager.onLanguageChanged -= Refresh;
	}

	private void OnDestroy ()
	{
		Watch(null);
	}

	public void Bind ( Entity _entity, bool _isReadable )
	{
		m_entity = _entity;
		Watch(_isReadable && _entity != null ? _entity.SpecialAbility : null);
		gameObject.SetActive(m_ability != null);
		Refresh();
	}

	private void Watch ( SpecialAbilityState _ability )
	{
		if (m_ability == _ability)
			return;

		if (m_ability != null)
			m_ability.onChanged -= Refresh;
		m_ability = _ability;
		if (m_ability != null)
			m_ability.onChanged += Refresh;
	}

	private void Refresh ()
	{
		if (m_ability == null || TurnManager.Instance == null)
			return;

		int absoluteTick = TurnManager.Instance.AbsoluteTick(TurnManager.currentTick);
		int remaining = m_ability.GetRemainingActiveTicks(absoluteTick);
		Color tone = remaining > 0 ? m_activeColor : m_ability.Charges > 0 ? m_chargedColor : m_emptyColor;

		m_icon.sprite = m_ability.data.icon;
		m_icon.color = tone;
		m_nameTMP.text = m_ability.data.GetLocalizedName();
		m_gauge.Show(m_ability, absoluteTick);
		m_valueTMP.color = tone;
		m_valueTMP.text = remaining > 0
			? string.Format(LocalizationManager.Instance.Get(remaining > 1 ? LocalizationKey.tooltip_duration_ticks : LocalizationKey.tooltip_duration_tick), remaining)
			: m_ability.Charges + " / " + m_ability.data.abilityChargeMax;
	}

	public void OnPointerEnter ( PointerEventData _eventData )
	{
		if (m_ability != null && m_entity != null)
			ToolTipManager.Instance.ShowAction(new ActionTooltipContent(m_ability.data, m_entity.Data, m_ability), gameObject, null);
	}

	public void OnPointerExit ( PointerEventData _eventData )
	{
		ToolTipManager.Instance.Hide();
	}
}
