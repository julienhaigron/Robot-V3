using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReturnToHubPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private TextMeshProUGUI m_contentTMP;
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

	public void Init ( string _dayReport )
	{
		m_dayReport = _dayReport;
		RefreshContent();
	}

	private void RefreshContent ()
	{
		m_contentTMP.text = string.Format(LocalizationManager.Instance.Get(LocalizationKey.return_hub_content), m_dayReport);
	}

}
