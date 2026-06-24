using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class MissionButton : BaseButton
{
	public static System.Action<MissionButton> onAnyMissionSelected;

	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_description;
	//[SerializeField] private ToolTipTrigger m_tooltipTrigger;

	private MissionData m_missionData;
	public MissionData MissionData => m_missionData;

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

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}

	[Button]
	protected override void OnClick ()
	{
		onAnyMissionSelected?.Invoke(this);

		base.OnClick();
	}


}
