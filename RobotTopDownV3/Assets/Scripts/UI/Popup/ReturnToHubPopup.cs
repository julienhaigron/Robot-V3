using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ReturnToHubPopup : AUIPopup
{
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private TextMeshProUGUI m_contentTMP;
	[SerializeField] private GameObject[] m_dayDisplays;
	[SerializeField] private TextMeshProUGUI m_cycleTMP;
	[SerializeField] private TextMeshProUGUI m_dayTMP;
	[SerializeField] private RecyclingComponentInfoDisplay[] m_recycledComponentsDisplays;

	[Header("Opening")]
	[SerializeField] private RectTransform m_frame;
	[SerializeField] private CanvasGroup m_frameGroup;
	[SerializeField] private CanvasGroup m_headerGroup;
	[SerializeField] private CanvasGroup m_reportGroup;
	[SerializeField] private CanvasGroup m_recyclingGroup;
	[SerializeField] private float m_frameStartScale = .03f;
	[SerializeField] private float m_frameDuration = .28f;
	[SerializeField] private float m_sectionDuration = .25f;
	[SerializeField] private float m_sectionStagger = .06f;
	[SerializeField] private float m_rowStagger = .08f;
	[SerializeField] private float m_slideDistance = 24f;

	private string m_dayReport;
	private Sequence m_opening;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		LocalizationManager.onLanguageChanged += RefreshContent;
	}

	private void OnDestroy ()
	{
		LocalizationManager.onLanguageChanged -= RefreshContent;
	}

	protected override void OnHideStarted ()
	{
		m_opening?.Kill(true);
		base.OnHideStarted();
	}

	private void OnClickClose ()
	{
		UIManager.Instance.GetPanel<SoloHubPanel>().RefreshVisual();
		Close();

		if (!GameDatas.current.currentPlayerSave.cycleData.didSelectMissions)
			UIManager.Instance.OpenPanel<SelectMissionPanel>();
	}

	public void Init ( string _dayReport, List<GameDatas.PlayerSave.DayData.RecyclingComponentData> _recycledComponent )
	{
		m_dayReport = _dayReport;
		RefreshContent();

		for (int i = 0; i < m_recycledComponentsDisplays.Length; i++)
		{
			if (_recycledComponent.Count > i)
				m_recycledComponentsDisplays[i].Init(_recycledComponent[i]);
			else
				m_recycledComponentsDisplays[i].Hide();
		}

		PlayOpening();
	}

	private void RefreshContent ()
	{
		LocalizationManager localization = LocalizationManager.Instance;
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;

		m_contentTMP.text = string.IsNullOrWhiteSpace(m_dayReport) ? localization.Get(LocalizationKey.return_hub_nothing) : m_dayReport;
		m_cycleTMP.text = string.Format(localization.Get(LocalizationKey.hub_cycle), save.cycleCount + 1);
		m_dayTMP.text = string.Format(localization.Get(LocalizationKey.hub_day), save.dayCount + 1);

		for (int i = 0; i < m_dayDisplays.Length; i++)
			m_dayDisplays[i].SetActive(save.dayCount >= i);
	}

	private void PlayOpening ()
	{
		m_opening?.Kill(true);
		m_frame.localScale = new Vector3(1f, m_frameStartScale, 1f);
		m_frameGroup.alpha = 0f;

		m_opening = DOTween.Sequence().SetLink(gameObject).SetUpdate(true)
			.Append(m_frame.DOScaleY(1f, m_frameDuration).SetEase(Ease.OutCubic))
			.Join(m_frameGroup.DOFade(1f, m_frameDuration * .5f));

		float sectionsStart = m_frameDuration * .7f;
		m_opening.Insert(sectionsStart, RevealSection(m_headerGroup, Vector2.up));
		m_opening.Insert(sectionsStart + m_sectionStagger, RevealSection(m_reportGroup, Vector2.left));
		m_opening.Insert(sectionsStart + m_sectionStagger * 2f, RevealSection(m_recyclingGroup, Vector2.right));

		float rowsStart = sectionsStart + m_sectionStagger * 3f;
		int revealedRows = 0;
		foreach (RecyclingComponentInfoDisplay row in m_recycledComponentsDisplays)
		{
			if (row.gameObject.activeSelf)
				m_opening.Insert(rowsStart + m_rowStagger * revealedRows++, row.Reveal(m_sectionDuration, m_slideDistance));
		}

		int today = GameDatas.current.currentPlayerSave.dayCount;
		if (today >= 0 && today < m_dayDisplays.Length)
		{
			Transform todayFill = m_dayDisplays[today].transform;
			todayFill.localScale = new Vector3(0f, 1f, 1f);
			m_opening.Insert(rowsStart, todayFill.DOScaleX(1f, m_sectionDuration).SetEase(Ease.OutBack));
		}
	}

	private Tween RevealSection ( CanvasGroup _section, Vector2 _from )
	{
		RectTransform rectTransform = (RectTransform)_section.transform;
		_section.alpha = 0f;
		rectTransform.anchoredPosition = _from * m_slideDistance;

		return DOTween.Sequence()
			.Append(_section.DOFade(1f, m_sectionDuration))
			.Join(rectTransform.DOAnchorPos(Vector2.zero, m_sectionDuration).SetEase(Ease.OutCubic));
	}
}
