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

	[Header("Selection feedback")]
	[SerializeField] private Graphic m_glow;
	[SerializeField] private Graphic m_fill;
	[SerializeField] private Color m_selectedColor = new(.18f, .9f, 1f);
	[SerializeField] private Color m_unselectedColor = Color.white;
	[SerializeField] private float m_glowAlpha = 1f;
	[SerializeField] private float m_fillAlpha = .22f;
	[SerializeField] private float m_fadeDuration = .1f;
	[SerializeField] private float m_popStrength = .04f;
	[SerializeField] private float m_popDuration = .15f;

	private MissionData m_missionData;
	public MissionData MissionData => m_missionData;
	private GameDatas.ProceduralMatchData m_proceduralMatch;

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;
	private Sequence m_selectionTween;
	private Tween m_popTween;

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
		SetSelected(true);
	}

	public void SetHasUnselected ()
	{
		SetSelected(false);
	}

	private void SetSelected ( bool _isSelected )
	{
		m_isSelected = _isSelected;
		Color color = _isSelected ? m_selectedColor : m_unselectedColor;
		float glow = _isSelected ? m_glowAlpha : 0f;
		float fill = _isSelected ? m_fillAlpha : 0f;

		m_selectionTween?.Kill();
		if (!isActiveAndEnabled)
		{
			m_outlineImg.color = color;
			m_name.color = color;
			SetAlpha(m_glow, glow);
			SetAlpha(m_fill, fill);
			return;
		}

		m_selectionTween = DOTween.Sequence().SetUpdate(true)
			.Join(m_outlineImg.DOColor(color, m_fadeDuration))
			.Join(m_name.DOColor(color, m_fadeDuration))
			.Join(m_glow.DOFade(glow, m_fadeDuration))
			.Join(m_fill.DOFade(fill, m_fadeDuration));
	}

	private static void SetAlpha ( Graphic _graphic, float _alpha )
	{
		Color color = _graphic.color;
		color.a = _alpha;
		_graphic.color = color;
	}

	private void OnDisable ()
	{
		m_selectionTween?.Complete();
		m_popTween?.Complete();
	}

	[Button]
	protected override void OnClick ()
	{
		SetSelected(!m_isSelected);
		m_popTween?.Complete();
		m_popTween = transform.DOPunchScale(transform.localScale * m_popStrength, m_popDuration, 1, 0f).SetUpdate(true);
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
