using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

public class MissionButton : BaseButton, IPointerEnterHandler, IPointerExitHandler
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

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;

	public void Init( MissionDataEnumID _missionID )
	{
		m_missionData = GameAssets.current.game.missions[_missionID];
		string title = m_missionData.missionName;
		string description = m_missionData.GetDescription();
		//m_tooltipTrigger.Init(title, description);

		//m_icon.sprite = m_missionData.icon;
		m_name.text = title;
		m_description.text = description;
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
		onAnyMissionSelected?.Invoke(this);

		base.OnClick();
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		onAnyMissionHovered?.Invoke(this);
	}
	
	public void OnPointerExit ( PointerEventData eventData )
	{
		onAnyMissionHovered?.Invoke(null);
	}


}
