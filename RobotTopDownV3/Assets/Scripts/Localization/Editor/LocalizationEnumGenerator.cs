using UnityEditor;
using System.IO;
using System.Text;

public static class LocalizationEnumGenerator
{
    private const string m_filePath = "Assets/Scripts/Localization/LocalizationKey.cs";

    [MenuItem("Tools/Localization/Generate Enum")]
    public static void Generate ()
    {
        var db = GetDatabase();

        if (db == null)
        {
            UnityEngine.Debug.LogError("LocalizationDatabase not found");
            return;
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("// Auto-generated file. Do not modify manually!");
        sb.AppendLine("public enum LocalizationKey");
        sb.AppendLine("{");

        foreach (var entry in db.entries)
        {
            string enumName = Sanitize(entry.key);
            sb.AppendLine($"    {enumName},");
        }

        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public static class LocalizationKeyExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static string ToKey(this LocalizationKey key)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch(key)");
        sb.AppendLine("        {");

        foreach (var entry in db.entries)
        {
            string enumName = Sanitize(entry.key);
            sb.AppendLine($"            case LocalizationKey.{enumName}: return \"{entry.key}\";");
        }

        sb.AppendLine("            default: return string.Empty;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        File.WriteAllText(m_filePath, sb.ToString());
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log("Localization enum and mapping generated!");
    }

    private static string Sanitize ( string _key )
    {
        // Remplace / et les caractères invalides par _
        string enumName = _key.Replace("/", "_").Replace(" ", "_").Replace("-", "_");

        // Assurer que ça commence par une lettre (sinon on préfixe)
        if (enumName.Length > 0 && !char.IsLetter(enumName[0]))
            enumName = "_" + enumName;

        return enumName;
    }

    private static LocalizationDatabase GetDatabase ()
    {
        string[] guids = AssetDatabase.FindAssets("t:LocalizationDatabase");

        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<LocalizationDatabase>(path);
    }
}