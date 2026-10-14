using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

public class SoloHubPanel : AUIPanel
{
	[SerializeField] private BaseButton m_missionBtn;
	[SerializeField] private BaseButton m_skipDayBtn;
	[SerializeField] private TextMeshProUGUI m_missionBtnTMP;

	private void Awake ()
	{
		m_missionBtn.onClick += OnClickMissionBtn;
		m_skipDayBtn.onClick += OnClickSkipDay;
	}

	private void OnEnable ()
	{
		LocalizationManager.onLanguageChanged += RefreshMissionBtnLabel;
	}

	private void OnDisable ()
	{
		LocalizationManager.onLanguageChanged -= RefreshMissionBtnLabel;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
		RefreshVisual();
	}

	public void RefreshVisual ()
	{
		bool isSquadValid = GameManager.Instance.SquadValidityPredicate();

		RefreshMissionBtnLabel();

		m_missionBtn.SetInteractability(isSquadValid);
		//m_tournamentBtn.SetInteractability(isSquadValid);
	}

	private void RefreshMissionBtnLabel ()
	{
		bool isTournament = GameDatas.current.currentPlayerSave.IsTournamentDay;

		m_missionBtnTMP.text = LocalizationManager.Instance.Get(isTournament ? LocalizationKey.hub_tournament : LocalizationKey.hub_mission);
	}

	#region Callbacks

	private void OnClickMissionBtn ()
	{
		if (GameDatas.current.currentPlayerSave.IsTournamentDay)
			UIManager.Instance.OpenPanel<TournamentPanel>();
		else
			UIManager.Instance.OpenPanel<MissionPanel>();
	}

	private void OnClickSkipDay ()
	{
		UIManager.Instance.OpenPopup<SkipConfirmationPopup>().Init();
	}

	#endregion

}
