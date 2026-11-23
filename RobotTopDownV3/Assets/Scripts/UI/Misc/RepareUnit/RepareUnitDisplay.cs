using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RepareUnitDisplay : GestureDisplay
{
	public static System.Action<RepareUnitDisplay> onDisplayHovered;

	[SerializeField] private Image m_icon;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;
	[SerializeField] private GameObject m_damagedGO;

	private EntitySavedData m_savedData;
	public EntitySavedData SavedData => m_savedData;

	public override object Content => m_savedData;
	public override bool IsPlaceholder => m_savedData == null;

	public void Init ( EntitySavedData _unitSavedData )
	{
		m_savedData = _unitSavedData;
		if (_unitSavedData != null)
		{
			if (_unitSavedData.FrameData != null)
				m_icon.sprite = _unitSavedData.FrameData.icon;
			m_damagedGO.SetActive(_unitSavedData.IsDamaged());
		}
		else
			m_damagedGO.SetActive(false);

		m_corpIcon.gameObject.SetActive(true);
		m_descriptionTMP.gameObject.SetActive(true);
		m_descriptionTMP.text = LocalizationManager.Instance.Get(LocalizationKey.component_repair);
	}

	protected override void OnHoverChanged ( bool _isHovered )
	{
		if (_isHovered)
			onDisplayHovered?.Invoke(this);
	}
}
