using UnityEngine;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using System;

[CustomEditor(typeof(EntityActionData))]
public class EntityActionDataEditor : OdinEditor
{
    EntityActionData targetAction;


    protected override void OnEnable ()
    {
        base.OnEnable();
        Init();
    }

    private void Init ()
    {
        targetAction = (target as EntityActionData);
    }

    public override void OnInspectorGUI ()
    {
        if (targetAction == null)
        {
            Init();
        }

        base.OnInspectorGUI();

        Regex rgx = new Regex("[^a-zA-Z0-9]");
        targetAction.name = rgx.Replace(targetAction.name, "");
        if (GUILayout.Button("Compute currencies enum"))
        {
            ComputeEnum();
        }
    }

    private static List<string> actionsPathsToUpdate = new List<string>();
    public static void ComputeEnum ()
    {
        List<EntityActionData> actions = FindAssetsByType<EntityActionData>();
        HashSet<string> processedActions = new HashSet<string>();
        List<EntityActionData> ignoredActions = new List<EntityActionData>();
        string enumList = "";
        Regex rgx = new Regex("[^a-zA-Z0-9 -]");
        foreach (EntityActionData action in actions)
        {
            action.name = rgx.Replace(action.name, "");
            if (processedActions.Add(action.name))
            {
                enumList += "\t\t" + action.name + ",\n";
            }
            else
            {
                ignoredActions.Add(action);
            }
        }

        while (ignoredActions.Count > 0)
        {
            actions.Remove(ignoredActions[0]);
            ignoredActions.RemoveAt(0);
        }

        actionsPathsToUpdate.Clear();
        foreach (EntityActionData action in actions)
        {
            actionsPathsToUpdate.Add(AssetDatabase.GetAssetPath(action));
        }
        EditorPrefs.SetString("ActionsPath", string.Join("\n", actionsPathsToUpdate));
        ScriptGenerator.AppendContent("EntityActionEnumID.cs", "EntityActionEnumID", enumList, true);
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void UpdateTypesWhenReady ()
    {
        EditorApplication.delayCall -= UpdateTypesWhenReady;

        if (!EditorPrefs.HasKey("ActionsPath"))
            return;
        actionsPathsToUpdate = new List<string>(EditorPrefs.GetString("ActionsPath").Split("\n"));
        if (actionsPathsToUpdate.Count > 0)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += UpdateTypesWhenReady;
                return;
            }

            foreach (string actionPath in actionsPathsToUpdate)
            {
                EntityActionData action = AssetDatabase.LoadAssetAtPath<EntityActionData>(actionPath);
                if (action != null)
                {
                    action.enumID = Enum.Parse<EntityActionEnumID>(action.name);
                    if (!GameAssets.current.game.entityActionsData.ContainsKey(action.enumID))
                    {
                        GameAssets.current.game.entityActionsData.Add(action.enumID, action);
                    }
                    else
                    {
                        GameAssets.current.game.entityActionsData[action.enumID] = action;
                    }
                    EditorUtility.SetDirty(action);
                }
            }
            EditorUtility.SetDirty(GameAssets.current);
            actionsPathsToUpdate.Clear();
            EditorPrefs.DeleteKey("ActionsPath");
            AssetDatabase.SaveAssets();
        }
    }

    private static List<T> FindAssetsByType<T> () where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }
        return assets;
    }
}
