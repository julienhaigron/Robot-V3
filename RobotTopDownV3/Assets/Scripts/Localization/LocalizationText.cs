using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizationText : MonoBehaviour
{
    public string key;

    private TMP_Text text;

    private void Awake ()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable ()
    {
        UpdateText();
        LocalizationManager.OnLanguageChanged += UpdateText;
    }

    private void OnDisable ()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText ()
    {
        text.text = LocalizationManager.Instance.Get(key);
    }
}
