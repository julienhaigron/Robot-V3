using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class CsvImporter
{
    public static async void ImportFromUrl<T> ( string _csvUrl, string _assetFolder ) where T : AParsableScriptableObject
    {
        string csv = await CsvDownloader.Download(_csvUrl);

        Dictionary<string, Dictionary<string, string>> objects = ParseColumnsAsObjects(csv);
        foreach (var kvp in objects)
        {
            string id = kvp.Key;
            Dictionary<string, string> data = kvp.Value;

            T asset = CreateOrLoadAsset<T>(id, _assetFolder);

            asset.spreadsheetId = _csvUrl;
            PopulateAsset(asset, new(data, asset.Id));

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static async void RefreshScriptable<T> ( T _parsableScriptable ) where T : AParsableScriptableObject
    {
        string csv = await CsvDownloader.Download(_parsableScriptable.GetUrl());
        Dictionary<string, Dictionary<string, string>> objects = ParseColumnsAsObjects(csv);

        if (objects.Keys.Contains(_parsableScriptable.Id))
		{
            PopulateAsset(_parsableScriptable, new(objects[_parsableScriptable.Id], _parsableScriptable.Id));

            EditorUtility.SetDirty(_parsableScriptable);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
            Debug.LogError("No entree found for Id " + _parsableScriptable.Id);
    }

    public static Dictionary<string, Dictionary<string, string>> ParseColumnsAsObjects ( string csv )
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        csv = csv.Replace("\r", "");

        string[] lines = csv.Split('\n');

        if (lines.Length == 0)
            return result;

        // IMPORTANT: CSV avec virgules
        string[] headers = lines[0]
            .Replace("\uFEFF", "")
            .Split(',');

        headers = headers
            .Select(h => h.Trim())
            .ToArray();

        // créer les objets (colonnes)
        for (int col = 1; col < headers.Length; col++)
        {
            string id = headers[col];
            result[id] = new Dictionary<string, string>();
        }

        // remplir les champs
        for (int row = 1; row < lines.Length; row++)
        {
            if (string.IsNullOrWhiteSpace(lines[row]))
                continue;

            string[] values = SplitCsvLine(lines[row]);

            string fieldName = values[0].Trim();

            for (int col = 1; col < headers.Length; col++)
            {
                string id = headers[col];

                string value = col < values.Length ? values[col].Trim() : "";

                result[id][fieldName] = value;
            }
        }

        return result;
    }

    public static string[] SplitCsvLine ( string line )
    {
        List<string> result = new();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);

        return result.ToArray();
    }

    private static void PopulateAsset ( AParsableScriptableObject _asset, ImportedData _data )
    {
        var fields = _asset.GetType().GetFields(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ParsingAttribute>();
            if (attr == null)
                continue;

            if (!_data.data.TryGetValue(attr.ColumnName, out string value))
			{
                Debug.Log("No value for var " + attr.ColumnName + " found for asset " + _asset.name);
                continue;
			}

            object converted = CsvTypeConverter.Convert(value, field.FieldType);

            field.SetValue(_asset, converted);
        }

        _asset.OnParse(_data);
    }

    private static T CreateOrLoadAsset<T> ( string _id, string _folder ) where T : AParsableScriptableObject
    {
        string path = $"{_folder}/{_id}.asset";

        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
            return asset;

        string typeName = typeof(T).Name;
        if (GameConfig.current.parsing.baseParsableScriptablePerType.ContainsKey(typeName))
            asset = AParsableScriptableObject.Instantiate(GameConfig.current.parsing.baseParsableScriptablePerType[typeName]) as T;
        else
            asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);

        return asset;
    }


}