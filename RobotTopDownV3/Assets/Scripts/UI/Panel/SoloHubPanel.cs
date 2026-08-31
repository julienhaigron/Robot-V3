using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

public class SoloHubPanel : AUIPanel
{
	[SerializeField] private BaseButton m_hangarBtn;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EntityFaction, BaseButton> m_openShopBtns;
	[SerializeField] private BaseButton m_recycleShopBtn;
	[SerializeField] private BaseButton m_repairBtn;
	[SerializeField] private BaseButton m_missionBtn;
	[SerializeField] private BaseButton m_skipDayBtn;
	[SerializeField] private TextMeshProUGUI m_missionBtnTMP;

	private void Awake ()
	{
		m_hangarBtn.onClick += OnClickHangarBtn;
		foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BaseButton> shopBtn in m_openShopBtns)
			shopBtn.Value.onClick += () => OnClickOpenShopBtn(shopBtn.Key);
		m_recycleShopBtn.onClick += OnClickOpenRecycleBtn;
		m_repairBtn.onClick += OnClickOpenRepairBtn;
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
		bool isSquadValid = /*GameManager.Instance.SquadValidityPredicate()*/true;

		RefreshMissionBtnLabel();

		m_missionBtn.SetInteractability(isSquadValid);
		m_repairBtn.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockRepareStation);
		m_recycleShopBtn.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockRecycler);
		/*foreach (KeyValuePair<EntityEquipmentData.EntityFaction, BaseButton> shopBtn in m_openShopBtns)
			shopBtn.Value.SetInteractability(GameDatas.current.currentPlayerSave.didUnlockShops);*/
		//m_tournamentBtn.SetInteractability(isSquadValid);
	}

	private void RefreshMissionBtnLabel ()
	{
		bool isTournament = GameDatas.current.currentPlayerSave.dayCount > 3;

		m_missionBtnTMP.text = LocalizationManager.Instance.Get(isTournament ? LocalizationKey.hub_tournament : LocalizationKey.hub_mission);
	}

	#region Callbacks

	private void OnClickHangarBtn ()
	{
		UIManager.Instance.OpenPanel<HangarPanel>();
	}

	private void OnClickOpenRecycleBtn ()
	{
		UIManager.Instance.OpenPanel<RecyclePanel>().Init();
	}

	private void OnClickOpenShopBtn ( EntityEquipmentData.EntityFaction _faction )
	{
		UIManager.Instance.OpenPanel<ShopPanel>().Init(_faction);
	}

	private void OnClickOpenRepairBtn ()
	{
		UIManager.Instance.OpenPanel<RepairStationPanel>().Init();
	}

	private void OnClickMissionBtn ()
	{
		if (GameDatas.current.currentPlayerSave.dayCount > 3)
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
