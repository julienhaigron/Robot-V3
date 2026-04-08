using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocalizationDatabase", menuName = "ScriptableObject/LocalizationDatabase")]
public class LocalizationDatabase : ScriptableObject
{
    public List<SystemLanguage> languages = new List<SystemLanguage>()
    {
        SystemLanguage.English,
        SystemLanguage.French
    };
    public List<LocalizationEntry> entries;

    public void Validate ()
    {
        foreach (var entry in entries)
        {
            while (entry.values.Count < languages.Count)
                entry.values.Add("");

            while (entry.values.Count > languages.Count)
                entry.values.RemoveAt(entry.values.Count - 1);
        }
    }

    private void OnValidate ()
    {
        Validate();
    }
}

[System.Serializable]
public class LocalizationEntry
{
    public string key;
    public List<string> values = new List<string>();
}
