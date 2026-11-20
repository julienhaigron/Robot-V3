using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

public class MissionButton : BaseButton, IPointerExitHandler
{
	public static System.Action<MissionButton> onAnyMissionSelected;
	public static System.Action<MissionButton> onAnyMissionHovered;

	//[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_name;
	[SerializeField] private TextMeshProUGUI m_description;
	[SerializeField] private Image m_outlineImg;
	//[SerializeField] private ToolTipTrigger m_tooltipTrigger;

	private MissionData m_missionData;
	public MissionData MissionData => m_missionData;
	private GameDatas.ProceduralMatchData m_proceduralMatch;

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;

	public void Init( MissionDataEnumID _missionID )
	{
		m_proceduralMatch = null;
		Init(GameAssets.current.game.missions[_missionID]);
	}

	public void Init ( GameDatas.ProceduralMatchData _match )
	{
		m_proceduralMatch = _match;
		Init(_match.GetMissionData());
	}

	private void Init ( MissionData _missionData )
	{
		m_missionData = _missionData;
		string title = m_missionData.GetLocalizedName();
		string description = m_missionData.GetDescription();
		m_name.text = title;
		m_description.text = description;

		SetHasUnselected();
	}

	public void SetHasSelected ()
	{
		m_isSelected = true;
		m_outlineImg.color = Color.blue;
	}

	public void SetHasUnselected ()
	{
		m_isSelected = false;
		m_outlineImg.color = Color.white;
	}

	[Button]
	protected override void OnClick ()
	{
		if (m_isSelected)
			SetHasUnselected();
		else
			SetHasSelected();
		onAnyMissionSelected?.Invoke(this);

		base.OnClick();
	}

	public override void OnPointerEnter ( PointerEventData eventData )
	{
		base.OnPointerEnter(eventData);

		onAnyMissionHovered?.Invoke(this);
	}
	
	public override void OnPointerExit ( PointerEventData eventData )
	{
		base.OnPointerExit(eventData);

		onAnyMissionHovered?.Invoke(null);
	}


}
