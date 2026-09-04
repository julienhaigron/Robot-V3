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

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
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
		m_contentTMP.text = "content:\n" + _dayReport;
	}

}
