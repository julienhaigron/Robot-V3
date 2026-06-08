using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SfxWindow : EditorWindow
{
	private SfxDatabase database;
	private Vector2 scroll;

	private readonly Dictionary<SfxCategory, bool> foldouts = new();

	[MenuItem("Tools/Sound Manager")]
	public static void Open ()
	{
		GetWindow<SfxWindow>("Sound Manager");
	}
	private void OnGUI ()
	{
		DrawHeader();
		DrawDatabaseField();

		if (database == null)
		{
			EditorGUILayout.HelpBox("Assign a SfxDatabase to begin.", MessageType.Info);
			return;
		}

		if (GUILayout.Button("+ Add SFX"))
		{
			SfxCreatePopup.Open(database);
		}

		if (GUILayout.Button("Generate Enum"))
		{
			SfxEnumGenerator.GenerateAndRefresh(database);
		}

		DrawContent();

		HandleDragAndDrop();
	}

	#region Draw

	private void DrawHeader ()
	{
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("SFX Manager", EditorStyles.boldLabel);
		EditorGUILayout.Space();
	}

	private void DrawDatabaseField ()
	{
		database = (SfxDatabase)EditorGUILayout.ObjectField("Database", database, typeof(SfxDatabase), false);
	}

	private void DrawContent ()
	{
		scroll = EditorGUILayout.BeginScrollView(scroll);

		foreach (SfxCategory category in System.Enum.GetValues(typeof(SfxCategory)))
		{
			DrawCategory(category);
		}

		EditorGUILayout.EndScrollView();
	}

	private void DrawCategory ( SfxCategory category )
	{
		if (!foldouts.ContainsKey(category))
			foldouts[category] = true;

		foldouts[category] = EditorGUILayout.Foldout(
			foldouts[category],
			category.ToString(),
			true);

		if (!foldouts[category])
			return;

		EditorGUI.indentLevel++;

		foreach (var sound in database.EditorSounds)
		{
			if (sound.Category != category)
				continue;

			DrawSfxLine(sound);
		}

		EditorGUI.indentLevel--;
	}

	private void DrawSfxLine ( SfxData sound )
	{
		EditorGUILayout.BeginHorizontal("box");

		if (GUILayout.Button(EditorGUIUtility.IconContent("IN foldout focus").image, GUILayout.Width(25)))
		{
			if (sound.Clip != null)
				EditorAudioPreview.Play(sound.Clip);
		}

		if (GUILayout.Button(EditorGUIUtility.IconContent("sv_label_6").image, GUILayout.Width(25)))
		{
			EditorAudioPreview.Stop();
		}

		EditorGUILayout.LabelField(sound.Id, GUILayout.Width(150));

		sound.Category = (SfxCategory)EditorGUILayout.EnumPopup(sound.Category, GUILayout.Width(120));
		AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(sound.Clip,typeof(AudioClip),false);
		if (newClip != sound.Clip)
		{
			sound.Clip = newClip;

			if (newClip != null)
				EditorAudioPreview.Play(newClip);
		}

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Volume", GUILayout.Width(60));
		sound.Volume = EditorGUILayout.Slider(sound.Volume, 0f, 1f);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Pitch", GUILayout.Width(60));
		sound.Pitch = EditorGUILayout.Slider(sound.Pitch, 0.5f, 2f);
		EditorGUILayout.EndHorizontal();


		if (GUILayout.Button("X", GUILayout.Width(25)))
		{
			RemoveSfx(sound);
			return;
		}

		EditorGUILayout.EndHorizontal();

		EditorUtility.SetDirty(database);
	}

	private void RemoveSfx ( SfxData sound )
	{
		database.EditorSounds.Remove(sound);
		EditorUtility.SetDirty(database);
	}

	#endregion

	#region Interaction

	private void HandleDragAndDrop ()
	{
		DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
		Event evt = Event.current;
		Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true)); 
		GUI.Box(dropArea, "Drop AudioClips here to create SFX", EditorStyles.helpBox);

		if (!dropArea.Contains(evt.mousePosition))
			return;

		switch (evt.type)
		{
			case EventType.DragUpdated:
			case EventType.DragPerform:

				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				if (evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();

					foreach (Object dragged in DragAndDrop.objectReferences)
					{
						if (dragged is AudioClip clip)
						{
							CreateSfxFromClip(clip);
						}
					}
				}

				Event.current.Use();
				break;
		}
	}

	private void CreateSfxFromClip ( AudioClip clip )
	{
		if (database == null || clip == null)
			return;

		string id = clip.name;
		if (database.EditorSounds.Exists(x => x.Id == id))
		{
			Debug.LogWarning($"SFX '{id}' already exists.");
			return;
		}

		var newSfx = new SfxData
		{
			Id = id,
			Category = SfxCategory.UI,
			Clip = clip,
			Volume = 1f,
			Pitch = 1f
		};

		database.EditorSounds.Add(newSfx);
		EditorUtility.SetDirty(database);
	}

	#endregion

	private void OnDisable ()
	{
		EditorAudioPreview.Stop();
	}
}