using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

public abstract class ScriptableEnum<TEnum> : ScriptableObject where TEnum : Enum
{
    [ReadOnly]
    public TEnum enumID;

#if UNITY_EDITOR
    [Button]
    private void RefreshAllEnum ()
	{
        ScriptableEnumAutoGenerator.GenerateAllEnums();
    }
#endif
}

#if UNITY_EDITOR

[InitializeOnLoad]
public static class ScriptableEnumAutoGenerator
{
    static ScriptableEnumAutoGenerator ()
    {
        EditorApplication.delayCall += GenerateAllEnums;
    }

    public static void GenerateAllEnums ()
    {
        var scriptableEnumTypes = GetAllScriptableEnumTypes();

        foreach (var type in scriptableEnumTypes)
        {
            GenerateEnum(type);
        }
    }

    static List<Type> GetAllScriptableEnumTypes ()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t =>
                t.BaseType != null &&
                t.BaseType.IsGenericType &&
                t.BaseType.GetGenericTypeDefinition() == typeof(ScriptableEnum<>))
            .ToList();
    }

    static void GenerateEnum ( Type scriptableType )
    {
        Type enumType = scriptableType.BaseType.GetGenericArguments()[0];
        string enumName = enumType.Name;
        string fileName = enumType + ".cs";

        var assets = FindAssets(scriptableType);

        HashSet<string> names = new();
        List<string> entries = new();

        foreach (var asset in assets)
        {
            string cleanName = Regex.Replace(asset.name, "[^a-zA-Z0-9]", "");

            if (string.IsNullOrEmpty(cleanName))
                continue;

            if (names.Add(cleanName))
                entries.Add(cleanName);
        }

        entries.Sort();

        string enumContent =
$@"public enum {enumName}
{{
    Unknown = 0,
";

        int index = 1;
        foreach (var entry in entries)
        {
            enumContent += $"    {entry} = {index},\n";
            index++;
        }

        enumContent += "}";

        WriteEnumFileIfChanged(fileName, enumContent);
    }

    static List<ScriptableObject> FindAssets ( Type type )
    {
        List<ScriptableObject> assets = new();

        string[] guids = AssetDatabase.FindAssets("t:" + type.Name);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            ScriptableObject asset = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;

            if (asset != null)
                assets.Add(asset);
        }

        return assets;
    }

    static void WriteEnumFileIfChanged ( string fileName, string newContent )
    {
        string folder = "Assets/Generated";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, fileName);

        if (File.Exists(path))
        {
            string existing = File.ReadAllText(path);

            if (existing == newContent)
                return; // rien à changer -> évite recompilation
        }

        File.WriteAllText(path, newContent);
        AssetDatabase.Refresh();
    }

    [DidReloadScripts]
    static void ResolveEnums ()
    {
        try
        {
            GenerateAllEnums();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GenerateAllEnums failed: {e.Message}");
        }

        var scriptableEnumTypes = GetAllScriptableEnumTypes();

        foreach (var type in scriptableEnumTypes)
        {
            AssignEnumIDs(type);
        }

        AssetDatabase.SaveAssets();
    }

    static void AssignEnumIDs ( Type scriptableType )
    {
        Type enumType = scriptableType.BaseType.GetGenericArguments()[0];

        string[] guids = AssetDatabase.FindAssets("t:" + scriptableType.Name);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            ScriptableObject asset = AssetDatabase.LoadAssetAtPath(path, scriptableType) as ScriptableObject;

            if (asset == null)
                continue;

            string cleanName = Regex.Replace(asset.name, "[^a-zA-Z0-9]", "");

            if (string.IsNullOrEmpty(cleanName))
                continue;

            try
            {
                if (!Enum.GetNames(enumType).Contains(cleanName))
                {
                    SetEnumValue(asset, scriptableType, enumType, "Unknown");
                    continue;
                }

                SetEnumValue(asset, scriptableType, enumType, cleanName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Enum assign failed for asset '{asset.name}' : {e.Message}");
            }
        }
    }

    static void SetEnumValue ( ScriptableObject asset, Type scriptableType, Type enumType, string enumName )
    {
        object enumValue = Enum.Parse(enumType, enumName);

        FieldInfo field = scriptableType.BaseType.GetField(
            "enumID",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
        );

        if (field == null)
            return;

        field.SetValue(asset, enumValue);
        EditorUtility.SetDirty(asset);
    }
}

#endif