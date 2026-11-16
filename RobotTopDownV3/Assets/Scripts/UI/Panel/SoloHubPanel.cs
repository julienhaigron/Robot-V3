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
	[SerializeField] private BaseButton m_classicMatchBtn;
	[SerializeField] private TextMeshProUGUI m_classicMatchBtnTMP;

	private static bool CanChooseClassicMatch => GameDatas.current.currentPlayerSave.CanJoinTournament && !GameDatas.current.currentPlayerSave.HasScriptedTournament;

	private void Awake ()
	{
		m_missionBtn.onClick += OnClickMissionBtn;
		m_skipDayBtn.onClick += OnClickSkipDay;
		if (m_classicMatchBtn != null)
			m_classicMatchBtn.onClick += OnClickClassicMatchBtn;
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

		if (m_classicMatchBtn != null)
		{
			m_classicMatchBtn.gameObject.SetActive(CanChooseClassicMatch);
			m_classicMatchBtn.SetInteractability(isSquadValid);
		}
	}

	private void RefreshMissionBtnLabel ()
	{
		bool isTournament = GameDatas.current.currentPlayerSave.IsTournamentAvailable;

		m_missionBtnTMP.text = LocalizationManager.Instance.Get(isTournament ? LocalizationKey.hub_tournament : LocalizationKey.hub_mission);
		if (m_classicMatchBtnTMP != null)
			m_classicMatchBtnTMP.text = LocalizationManager.Instance.Get(LocalizationKey.hub_mission);
	}

	#region Callbacks

	private void OnClickMissionBtn ()
	{
		if (GameDatas.current.currentPlayerSave.IsTournamentAvailable)
			UIManager.Instance.OpenPanel<TournamentPanel>();
		else
			UIManager.Instance.OpenPanel<MissionPanel>();
	}

	private void OnClickClassicMatchBtn ()
	{
		UIManager.Instance.OpenPanel<MissionPanel>();
	}

	private void OnClickSkipDay ()
	{
		UIManager.Instance.OpenPopup<SkipConfirmationPopup>().Init();
	}

	#endregion

}
