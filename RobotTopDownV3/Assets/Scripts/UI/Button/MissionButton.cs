using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class MissionButton : BaseButton
{
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_description;
	//[SerializeField] private ToolTipTrigger m_tooltipTrigger;

	private MissionData m_missionData;

	public void Init( MissionDataEnumID _missionID )
	{
		m_missionData = GameAssets.current.game.missions[_missionID];
		string title = m_missionData.missionName;
		string description = m_missionData.GetDescription();
		//m_tooltipTrigger.Init(title, description);

		m_icon.sprite = m_missionData.icon;
		m_name.text = title;
		m_description.text = description;
	}

	[Button]
	protected override void OnClick ()
	{
		if (m_missionData.preMissionDialogue != null)
			DialogueManager.Instance.PlayDialogue(m_missionData.preMissionDialogue, () => GameManager.Instance.SetupLevel(m_missionData));
		else
			GameManager.Instance.SetupLevel(m_missionData);
		
		base.OnClick();
	}


}
