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
	[SerializeField] private BaseButton m_returnBtn;

	private void Awake ()
	{
		m_changeSquadBtn.onClick += OnClickChangeSquadBtn;
		m_openShopBtn.onClick += OnClickOpenShopBtn;
		m_recycleShopBtn.onClick += OnClickOpenRecycleBtn;
		m_repairBtn.onClick += OnClickOpenRepairBtn;
		m_missionBtn.onClick += OnClickMissionBtn;
		m_returnBtn.onClick += OnClickReturnBtn;
	}

	private void OnClickChangeSquadBtn ()
	{
		UIManager.Instance.OpenPanel<HangarPanel>();
	}
	
	private void OnClickOpenRecycleBtn ()
	{
		UIManager.Instance.OpenPanel<RecyclePanel>();
	}

	private void OnClickOpenShopBtn ()
	{
		UIManager.Instance.OpenPanel<ShopPanel>().Init();
	}

	private void OnClickOpenRepairBtn ()
	{
		UIManager.Instance.OpenPanel<RepairStationPanel>();
	}
	
	private void OnClickMissionBtn ()
	{
		LevelData rndLvl = GameManager.Instance.GetRandomLevel();
		GameManager.Instance.SetupLevel(rndLvl);
	}
	
	private void OnClickReturnBtn ()
	{
		Close();
		GameManager.Instance.GoToStartScreen();
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
