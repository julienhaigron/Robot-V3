using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitRewardDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private Image m_mainIcon;
	[SerializeField] private Image m_subIcon;
	[SerializeField] private GameObject m_selectedGO;
	[SerializeField] private BaseButton m_btn;

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;
	private System.Action m_onSelected;
	private UnitPreset m_unitPreset;
	public UnitPreset UnitPreset => m_unitPreset;

	public void Init ( UnitPreset _unit, System.Action _onSelected, bool _isLockedOnSelected = false )
	{
		m_unitPreset = _unit;
		m_nameTMP.text = _unit.displayName;
		m_mainIcon.sprite = _unit.icon;
		m_subIcon.sprite = GameAssets.current.ui.corporationsIcons[_unit.GetSavedData().GetDominentFaction(out float percentage)];

		if (_isLockedOnSelected)
		{
			SetIsSelected(true);
			m_btn.SetInteractability(false);
		}
		else
		{
			SetIsSelected(false);
			m_onSelected = _onSelected;
			m_btn.onClick = OnClick;
		}
	}

	private void OnClick ()
	{
		SetIsSelected(!m_isSelected);
		m_onSelected?.Invoke();
	}

	public void SetIsSelected ( bool _isSelected )
	{
		m_isSelected = _isSelected;
		m_selectedGO.SetActive(m_isSelected);
	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		m_isSelected = false;
		gameObject.SetActive(false);
	}


}
