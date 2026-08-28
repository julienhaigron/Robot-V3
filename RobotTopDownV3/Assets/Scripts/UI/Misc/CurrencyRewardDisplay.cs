using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrencyRewardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_valueTMP;
    [SerializeField] private Image m_icon;
	[SerializeField] private GameObject m_selectedGO;
	[SerializeField] private BaseButton m_btn;

	private bool m_isSelected = false;
	public bool IsSelected => m_isSelected;
	private System.Action m_onSelected;
	private CurrencyType m_currencyType;
	public CurrencyType CurrencyType => m_currencyType;
	private ulong m_value;
	public ulong Value => m_value;

	public void Init(CurrencyType _type, ulong _value, bool _displaySuffix, System.Action _onSelected, bool _isLockedOnSelected = false )
	{
		m_currencyType = _type;
		m_value = _value;
        m_valueTMP.text = _value.ToString() + (_displaySuffix ? GameAssets.current.currencies[_type].suffix : "");
        m_icon.sprite = GameAssets.current.currencies[_type].icon;

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

	public void SetIsSelected(bool _isSelected )
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
