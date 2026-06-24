using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SoloHubPanel : AUIPanel
{
	[SerializeField] private BaseButton m_changeSquadBtn;
	[SerializeField] private BaseButton m_openShopBtn;
	[SerializeField] private BaseButton m_recycleShopBtn;
	[SerializeField] private BaseButton m_repairBtn;
	[SerializeField] private BaseButton m_missionBtn;

	private void Awake ()
	{
		m_changeSquadBtn.onClick += OnClickChangeSquadBtn;
		m_openShopBtn.onClick += OnClickOpenShopBtn;
		m_recycleShopBtn.onClick += OnClickOpenRecycleBtn;
		m_repairBtn.onClick += OnClickOpenRepairBtn;
		m_missionBtn.onClick += OnClickMissionBtn;
	}

	private void OnClickChangeSquadBtn ()
	{
		UIManager.Instance.OpenPanel<HangarPanel>();
	}
	
	private void OnClickOpenRecycleBtn ()
	{
		UIManager.Instance.OpenPanel<RecyclePanel>().Init();
	}

	private void OnClickOpenShopBtn ()
	{
		UIManager.Instance.OpenPanel<ShopPanel>().Init(EntityEquipmentData.EntityFaction.Psy);
	}

	private void OnClickOpenRepairBtn ()
	{
		UIManager.Instance.OpenPanel<RepairStationPanel>().Init();
	}
	
	private void OnClickMissionBtn ()
	{
		if(GameDatas.current.currentPlayerSave.dayCount <= 3)
			UIManager.Instance.OpenPanel<MissionPanel>();
		else
			UIManager.Instance.OpenPanel<TournamentPanel>();
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
		RefreshVisual();
	}

	private void RefreshVisual ()
	{
		m_missionBtn.SetInteractability(GameManager.Instance.SquadValidityPredicate());
	}

}
