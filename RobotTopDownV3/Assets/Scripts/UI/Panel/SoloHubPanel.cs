using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

public class SoloHubPanel : AUIPanel
{
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EntityFaction, BuildingBtn> m_openShopBtns;
	[SerializeField] private BuildingBtn m_hangarBtn;
	[SerializeField] private BuildingBtn m_recycleShopBtn;
	[SerializeField] private BuildingBtn m_repairBtn;
	[SerializeField] private BuildingBtn m_missionSelectionPanelBtn;
	[SerializeField] private BaseButton m_missionBtn;
	[SerializeField] private BaseButton m_skipDayBtn;
	[SerializeField] private TextMeshProUGUI m_missionBtnTMP;

	private void Awake ()
	{
		m_hangarBtn.Init(UIManager.Instance.GetPanel<HangarPanel>());
		foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BuildingBtn> shopBtn in m_openShopBtns)
			shopBtn.Value.Init(UIManager.Instance.GetPanel<ShopPanel>());
		m_recycleShopBtn.Init(UIManager.Instance.GetPanel<RecyclePanel>());
		m_missionSelectionPanelBtn.Init(UIManager.Instance.GetPanel<SelectMissionPanel>());
		m_repairBtn.Init(UIManager.Instance.GetPanel<RepairStationPanel>());
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
		m_repairBtn.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockRepareStation);
		m_recycleShopBtn.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockRecycler);
		foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BuildingBtn> shopBtn in m_openShopBtns)
			shopBtn.Value.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockShops);
		m_missionSelectionPanelBtn.SetInteractability(GameDatas.current.currentPlayerSave.cycleCount > 0);
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
