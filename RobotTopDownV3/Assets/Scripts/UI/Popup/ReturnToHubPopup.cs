using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReturnToHubPopup : AUIPopup
{
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private TextMeshProUGUI m_contentTMP;
	[SerializeField] private GameObject[] m_dayDisplays;
	[SerializeField] private TextMeshProUGUI m_cycleTMP;
	[SerializeField] private TextMeshProUGUI m_dayTMP;
	[SerializeField] private RecyclingComponentInfoDisplay[] m_recycledComponentsDisplays;
	private string m_dayReport;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		LocalizationManager.onLanguageChanged += RefreshContent;
	}

	private void OnDestroy ()
	{
		LocalizationManager.onLanguageChanged -= RefreshContent;
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

		for(int i = 0; i < m_recycledComponentsDisplays.Length; i++)
		{
			if (_recycledComponent.Count > i)
				m_recycledComponentsDisplays[i].Init(_recycledComponent[i]);
			else
				m_recycledComponentsDisplays[i].Hide();
		}
	}

	private void RefreshContent ()
	{
		string report = string.IsNullOrWhiteSpace(m_dayReport)
			? LocalizationManager.Instance.Get(LocalizationKey.return_hub_nothing)
			: m_dayReport;

		m_contentTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.return_hub_content), report);

		m_cycleTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.hub_cycle), GameDatas.current.currentPlayerSave.cycleCount + 1);
		m_dayTMP.text = "DAY " + (GameDatas.current.currentPlayerSave.dayCount + 1).ToString();

		for (int i = 0; i < m_dayDisplays.Length; i++)
		{
			m_dayDisplays[i].SetActive(GameDatas.current.currentPlayerSave.dayCount >= i);
		}
	}

}
