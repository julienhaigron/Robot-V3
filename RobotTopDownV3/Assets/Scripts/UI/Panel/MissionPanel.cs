using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MissionPanel : AUIPanel
{
	[SerializeField] private BaseButton m_returnBtn;
	[SerializeField] private Transform m_missionBtnsParent;
	[SerializeField] private MissionButton m_baseMissionBtn;
	
	private List<MissionButton> m_levelBtns = new();

	private void Awake ()
	{
		m_returnBtn.onClick += OnClickReturn;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();

		RefreshMissionBtns();
	}

	private void RefreshMissionBtns ()
	{
		if(m_levelBtns.Count < GameDatas.current.currentPlayerSave.dayData.missionsIds.Count)
		{
			for(int i = m_levelBtns.Count; i < GameConfig.current.game.missionAmountInSoloPanel; i++)
			{
				m_levelBtns.Add(Instantiate(m_baseMissionBtn, m_missionBtnsParent));
			}
		}

		int missionCount = 0;
		foreach(MissionButton btn in m_levelBtns)
		{
			btn.Init(GameDatas.current.currentPlayerSave.dayData.missionsIds[missionCount++]);
		}
	}

	private void OnClickReturn ()
	{
		UIManager.Instance.OpenPanel<SoloHubPanel>();
	}
}
