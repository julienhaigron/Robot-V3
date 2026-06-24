using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrencyRewardDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_valueTMP;
    [SerializeField] private Image m_icon;

    public void Init(CurrencyType _type, ulong _value )
	{
        m_valueTMP.text = _value.ToString();
        m_icon.sprite = GameAssets.current.currencies[_type].icon;

    }

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}
}
