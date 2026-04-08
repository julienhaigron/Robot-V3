using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(LocalizationDatabase))]
public class LocalizationDatabaseEditor : Editor
{
    public static int HighlightedIndex = -1;

    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
    private LocalizationDatabase db;

    private string search;
    private Vector2 scroll;
    private string newEntryPath = "menu/audio";
    private Dictionary<string, bool> treeFoldouts = new Dictionary<string, bool>();

    private class TreeNode
    {
        public string name;
        public Dictionary<string, TreeNode> children = new Dictionary<string, TreeNode>();
        public List<int> entryIndices = new List<int>();
    }

    private TreeNode BuildTree ()
    {
        TreeNode root = new TreeNode { name = "Root" };

        for (int i = 0; i < db.entries.Count; i++)
        {
            string key = db.entries[i].key;

            if (string.IsNullOrEmpty(key))
                key = "Uncategorized";

            string[] parts = key.Split('/');

            TreeNode current = root;

            for (int j = 0; j < parts.Length; j++)
            {
                string part = parts[j];

                if (!current.children.ContainsKey(part))
                {
                    current.children[part] = new TreeNode { name = part };
                }

                current = current.children[part];
            }

            current.entryIndices.Add(i);
        }

        return root;
    }

    private void OnEnable ()
    {
        db = (LocalizationDatabase)target;
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();

        search = EditorGUILayout.TextField("Search", search);

        DrawLanguages();
        EditorGUILayout.Space();
        DrawAddEntry();
        EditorGUILayout.Space();
        DrawTable();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(db);
        }

        if (GUILayout.Button("Validate Database"))
        {
            ValidateDatabase();
        }

    }

    private Dictionary<string, List<int>> GetGroupedEntries ()
    {
        var groups = new Dictionary<string, List<int>>();

        for (int i = 0; i < db.entries.Count; i++)
        {
            string key = db.entries[i].key;

            if (string.IsNullOrEmpty(key))
                key = "Uncategorized";

            string category = key.Contains("/")
                ? key.Split('/')[0]
                : "Uncategorized";

            if (!groups.ContainsKey(category))
                groups[category] = new List<int>();

            groups[category].Add(i);
        }

        return groups;
    }

    private void DrawLanguages ()
    {
        EditorGUILayout.LabelField("Languages", EditorStyles.boldLabel);

        for (int i = 0; i < db.languages.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            db.languages[i] = (SystemLanguage)EditorGUILayout.EnumPopup(db.languages[i]);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RemoveLanguage(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Language"))
        {
            AddLanguage();
        }
    }

    private void AddLanguage ()
    {
        db.languages.Add(SystemLanguage.French);

        foreach (var entry in db.entries)
        {
            entry.values.Add("");
        }
    }

    private void RemoveLanguage ( int index )
    {
        db.languages.RemoveAt(index);

        foreach (var entry in db.entries)
        {
            if (entry.values.Count > index)
                entry.values.RemoveAt(index);
        }
    }

    private void DrawNode ( TreeNode node, int indent = 0, string path = "" )
    {
        foreach (var child in node.children)
        {
            string currentPath = string.IsNullOrEmpty(path)
                ? child.Key
                : path + "/" + child.Key;

            if (!treeFoldouts.ContainsKey(currentPath))
                treeFoldouts[currentPath] = true;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 15);

            treeFoldouts[currentPath] = EditorGUILayout.Foldout(treeFoldouts[currentPath], child.Key, true);
            EditorGUILayout.EndHorizontal();

            if (treeFoldouts[currentPath])
            {
                // Dessiner les entrées (feuilles)
                foreach (int index in child.Value.entryIndices)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space((indent + 1) * 15);

                    DrawRow(index);

                    EditorGUILayout.EndHorizontal();
                }

                DrawNode(child.Value, indent + 1, currentPath);
            }
        }
    }

    private void DrawAddEntry ()
    {
        EditorGUILayout.BeginHorizontal();

        newEntryPath = EditorGUILayout.TextField(newEntryPath);

        if (GUILayout.Button("Add Entry"))
        {
            AddEntry(newEntryPath);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void AddEntry ( string path )
    {
        var newEntry = new LocalizationEntry();

        newEntry.key = path;

        for (int i = 0; i < db.languages.Count; i++)
        {
            newEntry.values.Add("");
        }

        db.entries.Add(newEntry);

        LocalizationEnumGenerator.Generate();
    }

    private void DrawTable ()
    {
        EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (HighlightedIndex >= 0)
        {
            scroll.y = HighlightedIndex * 20f; // approx hauteur ligne
        }

        DrawCustomHeader();
        var groups = GetGroupedEntries();

        foreach (var group in groups)
        {
            /*if (!string.IsNullOrEmpty(search) && !db.entries[i].key.ToLower().Contains(search.ToLower()))
                continue;*/
            string category = group.Key;

            if (!foldouts.ContainsKey(category))
                foldouts[category] = true;

            // Foldout
            foldouts[category] = EditorGUILayout.Foldout(foldouts[category], category, true);

            if (foldouts[category])
            {
                foreach (int index in group.Value)
                {
                    DrawRow(index);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCustomHeader ()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Key", GUILayout.Width(150));

        foreach (var lang in db.languages)
        {
            GUILayout.Label(lang.ToString(), GUILayout.Width(150));
        }

        GUILayout.Label("", GUILayout.Width(30));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRow ( int index )
    {
        var entry = db.entries[index];

        bool isHighlighted = index == HighlightedIndex;

        if (isHighlighted)
        {
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // bleu
        }

        EditorGUILayout.BeginHorizontal();
        bool isDuplicate = IsDuplicateKey(entry.key, index);

        if (isDuplicate)
            GUI.backgroundColor = Color.yellow;

        entry.key = EditorGUILayout.TextField(entry.key, GUILayout.Width(150));

        GUI.backgroundColor = Color.white;

        while (entry.values.Count < db.languages.Count)
            entry.values.Add("");

        for (int i = 0; i < db.languages.Count; i++)
        {
            bool isMissing = string.IsNullOrEmpty(entry.values[i]);

            if (isMissing)
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // rouge

            entry.values[i] = EditorGUILayout.TextField(entry.values[i], GUILayout.Width(150));

            GUI.backgroundColor = Color.white;
        }

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            db.entries.RemoveAt(index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private bool IsDuplicateKey ( string key, int currentIndex )
    {
        for (int i = 0; i < db.entries.Count; i++)
        {
            if (i == currentIndex) continue;

            if (db.entries[i].key == key)
                return true;
        }

        return false;
    }

    private void ValidateDatabase ()
    {
        HashSet<string> keys = new HashSet<string>();

        foreach (var entry in db.entries)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning("Empty key found!");
            }

            if (!keys.Add(entry.key))
            {
                Debug.LogWarning($"Duplicate key: {entry.key}");
            }

            for (int i = 0; i < entry.values.Count; i++)
            {
                if (string.IsNullOrEmpty(entry.values[i]))
                {
                    Debug.LogWarning($"Missing translation: {entry.key} ({db.languages[i]})");
                }
            }
        }

        Debug.Log("Localization validation complete.");
    }
}
