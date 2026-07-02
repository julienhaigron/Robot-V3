using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_text;
    public TextMeshProUGUI Text => m_text;
    [SerializeField] private Image m_icon;
    public Image Icon => m_icon;

    public void Init(CurrencyType _type, string _text, bool _displaySuffix)
	{
        m_icon.sprite = GameAssets.current.currencies[_type].icon;
        m_text.text = _text + (_displaySuffix ? GameAssets.current.currencies[_type].suffix : "") ;
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
