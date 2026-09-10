#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public class EditorIconBrowser : EditorWindow
{
    [MenuItem("Tools/Icon Browser")]
    static void Open ()
    {
        GetWindow<EditorIconBrowser>("Icon Browser");
    }

    Vector2 scroll;
    Texture2D[] icons;

    void OnEnable ()
    {
        // Force Unity à charger plein d'icônes connues
        EditorGUIUtility.IconContent("GameObject Icon");
        EditorGUIUtility.IconContent("Prefab Icon");
        EditorGUIUtility.IconContent("Grid.BoxTool");
        EditorGUIUtility.IconContent("EditCollider");

        // Récupère TOUTES les textures chargées
        icons = Resources
            .FindObjectsOfTypeAll<Texture2D>()
            .Where(t =>
                t.hideFlags == HideFlags.HideAndDontSave &&
                !string.IsNullOrEmpty(t.name) &&
                !t.name.StartsWith("d_") // optionnel
            )
            .OrderBy(t => t.name)
            .ToArray();
    }

    void OnGUI ()
    {
        if (icons == null)
            return;

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var tex in icons)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(tex, GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.Label(tex.name);
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif
