using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LocalizationManager : Singleton<LocalizationManager>
{
    public static event System.Action onLanguageChanged;

    [SerializeField] private LocalizationDatabase m_database;

    public SystemLanguage CurrentLanguage;

    private Dictionary<string, string> m_localizedDict;

    public override void Awake ()
    {
        base.Awake();
        BuildDictionary();
    }

    public string Get ( LocalizationKey _key )
    {
        return Get(_key.ToString().Replace("_", "/"));
    }

    public string Get ( string _key )
    {
        return m_localizedDict.TryGetValue(_key, out var value) ? value : _key;
    }

    public void SetLanguage ( SystemLanguage _language )
    {
        CurrentLanguage = _language;
        BuildDictionary();
        onLanguageChanged?.Invoke();
    }

    private void BuildDictionary ()
    {
        m_localizedDict = new Dictionary<string, string>();

        int langIndex = m_database.languages.IndexOf(CurrentLanguage);
        int fallbackIndex = m_database.languages.IndexOf(SystemLanguage.English);

        foreach (var entry in m_database.entries)
        {
            string value = null;

            if (langIndex >= 0 && entry.values.Count > langIndex)
                value = entry.values[langIndex];

            // fallback anglais
            if (string.IsNullOrEmpty(value) && fallbackIndex >= 0 && entry.values.Count > fallbackIndex)
                value = entry.values[fallbackIndex];

            // fallback ultime = key
            if (string.IsNullOrEmpty(value))
                value = entry.key;

            m_localizedDict[entry.key] = value;
        }
    }


}
