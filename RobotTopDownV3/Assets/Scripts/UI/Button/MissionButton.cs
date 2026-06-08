using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MissionButton : BaseButton
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;
	//[SerializeField] private ToolTipTrigger m_tooltipTrigger;

	private MissionData m_missionData;

	public void Init( MissionDataEnumID _missionID )
	{
		m_missionData = GameAssets.current.game.missions[_missionID];
		m_name.text = m_missionData.missionName;
		m_icon.sprite = m_missionData.icon;

		string title = "Title";
		string description = "Description";
		//m_tooltipTrigger.Init(title, description);
	}

	protected override void OnClick ()
	{
		if (m_missionData.preMissionDialogue != null)
			DialogueManager.Instance.PlayDialogue(m_missionData.preMissionDialogue, () => GameManager.Instance.SetupLevel(m_missionData));
		else
			GameManager.Instance.SetupLevel(m_missionData);
		
		base.OnClick();
	}


}
