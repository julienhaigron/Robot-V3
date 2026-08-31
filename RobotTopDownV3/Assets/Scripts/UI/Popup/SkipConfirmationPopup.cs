using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class SkipConfirmationPopup : AUIPopup
{

	[Header("General")]
	[SerializeField] private BaseButton m_cancelBtn;
	[SerializeField] private BaseButton m_confirmBtn;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;

	private void Awake ()
	{
		m_cancelBtn.onClick += OnClickClose;
		m_confirmBtn.onClick += OnClickConfirm;
	}

	private void OnDestroy ()
	{
		m_cancelBtn.onClick -= OnClickClose;
		m_confirmBtn.onClick -= OnClickConfirm;
	}

	public void Init ()
	{
		int daysAmountSkip = 0; //base 1
		string description = "";
		if (GameDatas.current.currentPlayerSave.cycleCount < 1)
		{
			//skip FTUE and give all FTUE missions rewards
			daysAmountSkip = 7 - (GameDatas.current.currentPlayerSave.dayCount);
			description = "Do you want to skip all main tutos?\n This will make you skip " + daysAmountSkip + " days. You will get all rewards given by skipped missions";
		}
		else if (GameDatas.current.currentPlayerSave.dayCount < 4)
		{
			//skip days to tournament (day 4)
			daysAmountSkip = 4 - (GameDatas.current.currentPlayerSave.dayCount);
			description = "Do you want to skip all days until tournament?\nThis will make you skip " + daysAmountSkip + " days.";
		}
		else
		{
			//skip tournament
			daysAmountSkip = 7 - (GameDatas.current.currentPlayerSave.dayCount);
			description = "Do you want to skip this cycle's tournament and directly start next cycle?\nThis will make you skip " + daysAmountSkip + " days.";
		}
		m_descriptionTMP.text = description;
	}

	private void OnClickConfirm ()
	{
		int daysAmountSkip = 0; //base 1
		bool doGiveReward = false;
		int currentDay = GameDatas.current.currentPlayerSave.dayCount;
		if (GameDatas.current.currentPlayerSave.cycleCount < 1)
		{
			//skip FTUE and give all FTUE missions rewards
			daysAmountSkip = 7 - (GameDatas.current.currentPlayerSave.dayCount);
			doGiveReward = true;
			FTUEManager.Instance.ForceFinishFTUE();
		}
		else if(GameDatas.current.currentPlayerSave.dayCount < 4)
		{
			//skip days to tournament (day 4)
			daysAmountSkip = 4 - (GameDatas.current.currentPlayerSave.dayCount);
		}
		else
		{
			//skip tournament
			daysAmountSkip = 7 - (GameDatas.current.currentPlayerSave.dayCount);
		}
		for(int i = 0; i < daysAmountSkip; i++)
		{
			if (doGiveReward)
			{
				if(currentDay + i == -1)
					FTUEManager.Instance.Day0MissionData.GiveAllRewards();
				else if(currentDay + i < FTUEManager.Instance.Cycle1MatchMissions.Length)
					FTUEManager.Instance.Cycle1MatchMissions[i].GiveAllRewards();
				else
					FTUEManager.Instance.Cycle1TournamentMissions[i - FTUEManager.Instance.Cycle1MatchMissions.Length].GiveAllRewards();
			}
			GameDatas.current.currentPlayerSave.NewDay();
		}
		
		Close(_instant: true);
		
		//TODO : display all days past changes, not only previous day
		UIManager.Instance.OpenPopup<ReturnToHubPopup>().Init();
	}

	private void OnClickClose ()
	{
		Close();
	}
}
