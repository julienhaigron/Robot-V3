using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogTooltip : TooltipCard
{
	[Serializable]
	private struct KindStyle
	{
		public LogCard.Kind kind;
		public Sprite icon;
		public Color color;
	}

	public event Action<LogTooltip> onCloseRequested;

	[SerializeField] private Vector2 m_cursorOffset = new(15f, -15f);

	[Header("Head")]
	[SerializeField] private Image m_iconFill;
	[SerializeField] private Image m_iconFrame;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private GameObject m_subtitle;
	[SerializeField] private TextMeshProUGUI m_actorTMP;
	[SerializeField] private GameObject m_targetArrow;
	[SerializeField] private TextMeshProUGUI m_targetTMP;
	[SerializeField] private GameObject m_verdict;
	[SerializeField] private Image m_verdictFill;
	[SerializeField] private Image m_verdictFrame;
	[SerializeField] private TextMeshProUGUI m_verdictTMP;
	[SerializeField] private TextMeshProUGUI m_verdictValueTMP;
	[SerializeField] private GameObject m_pin;
	[SerializeField] private Button m_closeButton;

	[Header("Steps")]
	[SerializeField] private LogStepView[] m_steps;
	[SerializeField] private TextMeshProUGUI m_moreTMP;

	[Header("Style")]
	[SerializeField] private KindStyle[] m_kinds;
	[SerializeField] private LogCardStyle m_style = new();

	private bool m_isFollowingCursor = true;

	private void Awake ()
	{
		m_closeButton.onClick.AddListener(() => onCloseRequested?.Invoke(this));
	}

	public void Show ( LogCard _card )
	{
		Open();
		BindHead(_card);
		BindSteps(_card);

		LayoutRebuilder.ForceRebuildLayoutImmediate(m_card);
		if (m_isFollowingCursor)
			PlaceAtCursor(m_cursorOffset);
	}

	public void SetPinned ( bool _isPinned )
	{
		m_isFollowingCursor = !_isPinned;
		m_pin.SetActive(_isPinned);
		m_canvasGroup.blocksRaycasts = _isPinned;
	}

	public void CopyPlacementOf ( LogTooltip _other )
	{
		m_card.pivot = _other.m_card.pivot;
		m_card.localPosition = _other.m_card.localPosition;
	}

	protected override void OnShownUpdate ()
	{
		if (m_isFollowingCursor)
			PlaceAtCursor(m_cursorOffset);
	}

	private void BindHead ( LogCard _card )
	{
		KindStyle style = StyleOf(_card.kind);
		Color accent = _card.accent ?? (_card.verdictDamageType.HasValue ? m_style.Damage(_card.verdictDamageType.Value) : style.color);
		m_style.Paint(m_iconFill, m_iconFrame, accent);
		m_icon.sprite = _card.icon != null ? _card.icon : style.icon;
		m_icon.color = accent;
		m_titleTMP.text = _card.title;

		m_subtitle.SetActive(!string.IsNullOrEmpty(_card.actor));
		m_actorTMP.text = _card.actor;
		bool hasTarget = !string.IsNullOrEmpty(_card.target);
		m_targetArrow.SetActive(hasTarget);
		m_targetTMP.gameObject.SetActive(hasTarget);
		m_targetTMP.text = _card.target;

		bool hasVerdict = !string.IsNullOrEmpty(_card.verdict);
		m_verdict.SetActive(hasVerdict);
		if (!hasVerdict)
			return;

		Color color = m_style.Verdict(_card);
		m_style.Paint(m_verdictFill, m_verdictFrame, color);
		m_verdictTMP.gameObject.SetActive(!_card.isVerdictValue);
		m_verdictValueTMP.gameObject.SetActive(_card.isVerdictValue);
		TextMeshProUGUI verdict = _card.isVerdictValue ? m_verdictValueTMP : m_verdictTMP;
		verdict.text = _card.verdict;
		verdict.color = color;
	}

	private void BindSteps ( LogCard _card )
	{
		bool isFused = _card.steps.Count > 0;
		int shown = isFused ? Mathf.Min(_card.steps.Count, m_steps.Length) : _card.HasBody ? 1 : 0;
		for (int i = 0; i < m_steps.Length; i++)
		{
			bool isUsed = i < shown;
			m_steps[i].gameObject.SetActive(isUsed);
			if (isUsed)
				m_steps[i].Bind(isFused ? _card.steps[i] : _card, isFused ? _card : null, m_style);
		}

		int hidden = _card.steps.Count - shown;
		m_moreTMP.gameObject.SetActive(hidden > 0);
		if (hidden > 0)
			m_moreTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.log_card_more), hidden);
	}

	private KindStyle StyleOf ( LogCard.Kind _kind )
	{
		foreach (KindStyle style in m_kinds)
			if (style.kind == _kind)
				return style;
		return new KindStyle { kind = _kind, color = m_style.neutralColor };
	}
}
