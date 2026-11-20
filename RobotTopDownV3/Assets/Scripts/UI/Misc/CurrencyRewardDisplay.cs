using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrencyRewardDisplay : ARewardDisplay
{
    [SerializeField] private TextMeshProUGUI m_valueTMP;
    [SerializeField] private Image m_icon;

	private CurrencyType m_currencyType;
	public CurrencyType CurrencyType => m_currencyType;
	private ulong m_value;
	public ulong Value => m_value;

	public void Init ( CurrencyType _type, ulong _value, bool _displaySuffix, System.Action _onSelected, bool _isLockedOnSelected = false )
	{
		if (!GameAssets.current.currencies.ContainsKey(_type))
			return;

		m_currencyType = _type;
		m_value = _value;
        m_valueTMP.text = _value.ToString() + (_displaySuffix ? GameAssets.current.currencies[_type].suffix : "");
        m_icon.sprite = GameAssets.current.currencies[_type].icon;
		InitSelection(_onSelected, _isLockedOnSelected);
	}
}
