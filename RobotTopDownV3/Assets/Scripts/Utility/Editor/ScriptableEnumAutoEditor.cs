using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

[CustomEditor(typeof(ScriptableEnum<>))]
public class ScriptableEnumAutoEditor : OdinEditor
{
    ScriptableObject targetAction;


    protected override void OnEnable ()
    {
        base.OnEnable();
        Init();
    }

    private void Init ()
    {
        targetAction = (target as ScriptableObject);
    }
    public override void OnInspectorGUI ()
    {
        OnInspectorGUI(targetAction);
    }
    private void OnInspectorGUI ( ScriptableObject target )
    {
        if (target == null)
        {
            Init();
        }

        //if (!string.Equals(target.name, target.enumID.ToString()))
        //{
        Regex rgx = new Regex("[^a-zA-Z0-9]");
        target.name = rgx.Replace(target.name, "");
        if (GUILayout.Button("Compute enum"))
        {
            GenerateAllEnums();
        }
        //}

        GUILayout.Space(5f);

        base.OnInspectorGUI();
    }
    /*static ScriptableEnumAutoEditor ()
    {
        EditorApplication.delayCall += GenerateAllEnums;
    }*/

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

        List<ScriptableObject> assets = FindAssets(scriptableType);
        HashSet<string> processedAssets = new HashSet<string>();
        List<ScriptableObject> ignoredAssets = new List<ScriptableObject>();

        string enumList = "";
        Regex rgx = new Regex("[^a-zA-Z0-9 -]");
        foreach (ScriptableObject scriptable in assets)
        {
            scriptable.name = rgx.Replace(scriptable.name, "");
            if (processedAssets.Add(scriptable.name))
            {
                enumList += "\t" + scriptable.name + ",\n";
            }
            else
            {
                ignoredAssets.Add(scriptable);
            }
        }

        while (ignoredAssets.Count > 0)
        {
            assets.Remove(ignoredAssets[0]);
            ignoredAssets.RemoveAt(0);
        }

        ScriptGenerator.AppendContent(fileName, enumName, enumList, true);

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

    /*static void WriteEnumFileIfChanged ( string fileName, string newContent )
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
    }*/

    /* [DidReloadScripts]
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
     }*/

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