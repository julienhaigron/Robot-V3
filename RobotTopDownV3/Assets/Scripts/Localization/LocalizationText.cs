using UnityEngine;
using TMPro;
using System.Linq;

[RequireComponent(typeof(TMP_Text))]
public class LocalizationText : MonoBehaviour
{
    public string key;

    private TMP_Text text;
    private string[] m_formatArgKeys;

    private void Awake ()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable ()
    {
        UpdateText();
        LocalizationManager.onLanguageChanged += UpdateText;
    }

    private void OnDisable ()
    {
        LocalizationManager.onLanguageChanged -= UpdateText;
    }

    public void SetKey ( string _key, params string[] _formatArgKeys )
    {
        key = _key;
        m_formatArgKeys = _formatArgKeys;
        if (text != null && isActiveAndEnabled)
            UpdateText();
    }

    private void UpdateText ()
    {
        string value = LocalizationManager.Instance.Get(key);
        if (m_formatArgKeys != null && m_formatArgKeys.Length > 0)
            value = string.Format(value, m_formatArgKeys.Select(argKey => (object)LocalizationManager.Instance.Get(argKey)).ToArray());

        text.text = value;
    }
}
