using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LocalizationManager : Singleton<LocalizationManager>
{
    public static event System.Action OnLanguageChanged;

    public SystemLanguage CurrentLanguage;

    [SerializeField] private LocalizationDatabase database;

    private Dictionary<string, string> localizedDict;

    public override void Awake ()
    {
        base.Awake();
        BuildDictionary();
    }

    public string Get ( LocalizationKey key )
    {
        return Get(key.ToString().Replace("_", "/"));
    }

    public string Get ( string key )
    {
        return localizedDict.TryGetValue(key, out var value) ? value : key;
    }

    public void SetLanguage ( SystemLanguage language )
    {
        CurrentLanguage = language;
        BuildDictionary();
        OnLanguageChanged?.Invoke();
    }

    private void BuildDictionary ()
    {
        localizedDict = new Dictionary<string, string>();

        int langIndex = database.languages.IndexOf(CurrentLanguage);
        int fallbackIndex = database.languages.IndexOf(SystemLanguage.English);

        foreach (var entry in database.entries)
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

            localizedDict[entry.key] = value;
        }
    }


}
