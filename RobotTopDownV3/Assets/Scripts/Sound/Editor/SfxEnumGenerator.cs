using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SfxEnumGenerator
{
	private const string OUTPUT_PATH =
		"Assets/Generated/SfxIdEnumID.cs";

	public static void Generate ( SfxDatabase database )
	{
		if (database == null)
			return;

#if UNITY_EDITOR

		List<string> ids = database.EditorSounds
			.Select(s => s.Id.ToString())
			.Distinct()
			.OrderBy(x => x)
			.ToList();

		StringBuilder builder = new();

		builder.AppendLine("public enum SfxId");
		builder.AppendLine("{");
		builder.AppendLine("\tNone,");

		foreach (string rawId in ids)
		{
			if (rawId == "None")
				continue;

			string id = Sanitize(rawId);
			builder.AppendLine($"\t{id},");
		}

		builder.AppendLine("}");

		File.WriteAllText(OUTPUT_PATH, builder.ToString());

		AssetDatabase.Refresh();

#endif
	}

	private static string Sanitize ( string id )
	{
		id = Regex.Replace(id, @"[^a-zA-Z0-9_]", "");

		if (string.IsNullOrWhiteSpace(id))
			return "Invalid";

		if (char.IsDigit(id[0]))
			id = "_" + id;

		return id;
	}

	public static void GenerateAndRefresh ( SfxDatabase database )
	{
		Generate(database);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}