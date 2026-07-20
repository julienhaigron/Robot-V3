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
	ScriptableObject targetScriptable;


	protected override void OnEnable ()
	{
		base.OnEnable();
		Init();
	}

	private void Init ()
	{
		targetScriptable = (target as ScriptableObject);
	}

	public override void OnInspectorGUI ()
	{
		OnInspectorGUI(targetScriptable);
	}

	private void OnInspectorGUI ( ScriptableObject target )
	{
		if (target == null)
		{
			Init();
		}

		if (!string.Equals(target.name, ((IScriptableEnum)targetScriptable).GetEnumName().ToString()))
		{
			Regex rgx = new Regex("[^a-zA-Z0-9]");
			target.name = rgx.Replace(target.name, "");
			if (GUILayout.Button("Compute enum"))
			{
				GenerateAllEnums();
			}
			if (GUILayout.Button("Rename enum"))
			{
				string enumValue = ((IScriptableEnum)targetScriptable).GetEnumName();
				string previousLine = "\t\t" + enumValue + ",\n";
				string newLine = "\t\t" + targetScriptable.name + ",\n";
				ScriptGenerator.RewriteContent("EntityActionEnumID.cs", "EntityActionEnumID", previousLine, newLine);
			}
			if (GUILayout.Button("Refresh enum"))
			{
				if (Enum.TryParse(targetScriptable.name, out EntityActionEnumID _result))
				{
					Type scriptableType = targetScriptable.GetType();
					Type enumType = scriptableType.BaseType.GetGenericArguments()[0];
					SetEnumValue(targetScriptable, scriptableType, enumType, _result.ToString());
					EditorUtility.SetDirty(targetScriptable);
				}
			}
		}

		GUILayout.Space(5f);

		base.OnInspectorGUI();
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