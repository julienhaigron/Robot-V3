using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SoloHubPanel : AUIPanel
{
	[SerializeField] private BaseButton m_hangarBtn;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EntityFaction, BaseButton> m_openShopBtns;
	[SerializeField] private BaseButton m_recycleShopBtn;
	[SerializeField] private BaseButton m_repairBtn;
	[SerializeField] private BaseButton m_missionBtn;
	[SerializeField] private BaseButton m_tournamentBtn;

	private void Awake ()
	{
		m_hangarBtn.onClick += OnClickHangarBtn;
		foreach(KeyValuePair< EntityEquipmentData.EntityFaction, BaseButton> shopBtn in m_openShopBtns)
			shopBtn.Value.onClick += () => OnClickOpenShopBtn(shopBtn.Key);
		m_recycleShopBtn.onClick += OnClickOpenRecycleBtn;
		m_repairBtn.onClick += OnClickOpenRepairBtn;
		m_missionBtn.onClick += OnClickMissionBtn;
		m_tournamentBtn.onClick += OnClickTournamentBtn;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
		RefreshVisual();
	}

	private void RefreshVisual ()
	{
		bool isSquadValid = /*GameManager.Instance.SquadValidityPredicate();*/true;
		m_missionBtn.SetInteractability(isSquadValid);
		m_tournamentBtn.SetInteractability(isSquadValid);
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

	private void OnClickOpenShopBtn ( EntityEquipmentData.EntityFaction _faction)
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
			return;

		UIManager.Instance.OpenPanel<MissionPanel>();
	}

	private void OnClickTournamentBtn ()
	{
		if (GameDatas.current.currentPlayerSave.dayCount <= 3)
			return;

		UIManager.Instance.OpenPanel<TournamentPanel>();
	}

	#endregion

}
