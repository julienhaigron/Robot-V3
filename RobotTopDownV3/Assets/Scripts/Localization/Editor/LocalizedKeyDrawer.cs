using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(LocalizedKeyAttribute))]
public class LocalizedKeyDrawer : PropertyDrawer
{
    private string[] keys;
    private bool initialized;

    private void Init ()
    {
        if (initialized) return;

        var db = GetDatabase();

        if (db != null)
        {
            keys = db.entries
                .Select(e => e.key)
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .ToArray();
        }
        else
        {
            keys = new string[0];
        }

        initialized = true;
    }

    public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
    {
        Init();

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [LocalizedKey] with string.");
            return;
        }

        float buttonWidth = 25f;

        Rect textRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
        Rect popupRect = new Rect(position.x + position.width * 0.52f, position.y, position.width * 0.38f, position.height);
        Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

        bool isValid = keys != null && keys.Contains(property.stringValue);

        if (!isValid)
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

        property.stringValue = EditorGUI.TextField(textRect, label, property.stringValue);

        GUI.backgroundColor = Color.white;

        if (keys != null && keys.Length > 0)
        {
            int currentIndex = Mathf.Max(0, System.Array.IndexOf(keys, property.stringValue));
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, keys);

            property.stringValue = keys[newIndex];
        }

        if (GUI.Button(buttonRect, "Ping"))
        {
            PingKey(property.stringValue);
        }

        if (GUI.changed)
        {
            initialized = false;
        }
    }

    private void PingKey ( string _key )
    {
        var db = GetDatabase();

        if (db == null)
        {
            Debug.LogWarning("LocalizationDatabase not found.");
            return;
        }

        // Sélectionne l'asset
        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);

        // Trouver l'index
        int index = db.entries.FindIndex(e => e.key == _key);

        if (index >= 0)
        {
            LocalizationDatabaseEditor.HighlightedIndex = index;
        }
    }

    private LocalizationDatabase GetDatabase ()
    {
        // Cherche automatiquement le database dans le projet
        string[] guids = AssetDatabase.FindAssets("t:LocalizationDatabase");

        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);

        return AssetDatabase.LoadAssetAtPath<LocalizationDatabase>(path);
    }
}