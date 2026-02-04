using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using System.IO;
using Sirenix.Utilities.Editor;

[CustomEditor(typeof(GameDatas))]
public class GameDatasEditor : OdinEditor
{
	string backupPath = "";
	GameDatas datas;
	string backupFolder = "Objects/Backups/";

	string backupName = "";

	string[] backupFiles;

	protected override void OnEnable ()
	{
		base.OnEnable();

		if (datas == null)
		{
			datas = target as GameDatas;
		}
	}

	public override void OnInspectorGUI ()
	{
		if (datas == null)
		{
			datas = target as GameDatas;
		}

		if (datas.preventSave)
		{
			if (Application.isPlaying)
			{
				SirenixEditorGUI.InfoMessageBox("Can't edit at runtime when PreventSave if enabled!", true);
				GUI.enabled = false;
			}
		}
		base.OnInspectorGUI();

		SirenixEditorGUI.Title("Editor Tools", "", TextAlignment.Left, true);

		bool hasBackup = !string.IsNullOrWhiteSpace(backupPath);
		if (hasBackup)
		{
			if (!File.Exists(backupPath))
				hasBackup = !hasBackup;
		}

		//Get all backup files
		backupFiles = Directory.GetFiles(Application.dataPath + "/" + backupFolder, "*.json");

		GUILayout.BeginHorizontal();
		if (backupFiles.Length > 0)
		{
			string[] backupNames = new string[backupFiles.Length];
			for (int i = 0; i < backupFiles.Length; i++)
			{
				backupNames[i] = backupFiles[i].Substring(backupFiles[i].LastIndexOf("/") + 1);
			}

			string prevBackupName = EditorPrefs.GetString("GameDatasEditorBackupName", "");
			int selectedBackup = 0;
			for (int i = 0; i < backupNames.Length; i++)
			{
				if (backupNames[i] == prevBackupName)
				{
					selectedBackup = i;
					break;
				}
			}
			selectedBackup = EditorGUILayout.Popup("Select backup to load", selectedBackup, backupNames);
			EditorPrefs.SetString("GameDatasEditorBackupName", backupNames[selectedBackup]);
			backupPath = backupFiles[selectedBackup];
		}
		else
		{
			hasBackup = false;
		}

		GUI.enabled = hasBackup;
		if (GUILayout.Button("Load backup"))
		{
			LoadBackup(backupPath);
		}
		GUI.enabled = true;
		GUILayout.EndHorizontal();

		GUILayout.Space(5f);
		EditorGUILayout.HelpBox("Editor backups are saved in Assets/Objects/Backups/", MessageType.Info, true);
		backupName = EditorGUILayout.TextField("Save to:", backupName);
		backupName = backupName.Trim();
		if (!string.IsNullOrWhiteSpace(backupName))
		{
			if (GUILayout.Button("Save backup as \"" + backupName + ".json\""))
			{
				SaveBackup(backupName);
			}
		}

		GUILayout.Space(20);

		if (GUILayout.Button("Clear Datas"))
		{
			GameDatas.Clear();
		}
		GUI.enabled = true;
	}

	void GuiLine ( int height = 1 )

	{

		Rect rect = EditorGUILayout.GetControlRect(false, height);

		rect.height = height;

		EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

	}

	private void SaveBackup ( string backupName )
	{
		string path = Application.dataPath + "/" + backupFolder + backupName + (backupName.Contains(".json") ? "" : ".json");
		GameDatas.Save(datas, path, false);
		AssetDatabase.Refresh();
	}

	private void LoadBackup ( string backupPath )
	{
		bool prevSave = datas.preventSave;
		string path = backupPath + (backupPath.Contains(".json") ? "" : ".json");
		if (File.Exists(path))
		{
			GameDatas.Override(datas, path, false);
			datas.preventSave = prevSave;

			EditorUtility.SetDirty(datas);
		}
		else
		{
			Debug.LogError("Couldn't find " + path);
		}
	}

	private void Clear ()
	{
		bool prevSave = datas.preventSave;
		string savePath = GameDatas.defaultSaveFile;
		if (!savePath.StartsWith(Application.persistentDataPath))
		{
			savePath = Application.persistentDataPath + "/" + savePath;
		}
		if (File.Exists(savePath))
		{
			File.Delete(savePath);
		}

		GameDatas.OverrideFromJson(datas, GameDatas.GetEmptyDatas());
		datas.preventSave = prevSave;
		EditorUtility.SetDirty(datas);
	}

}
