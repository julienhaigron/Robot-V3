using UnityEditor;
using UnityEngine;

public class SfxCreatePopup : EditorWindow
{
	private string sfxName = "NewSFX";
	private SfxCategory category = SfxCategory.UI;
	private AudioClip clip;

	private SfxDatabase database;

	public static void Open ( SfxDatabase db )
	{
		var window = CreateInstance<SfxCreatePopup>();
		window.database = db;

		window.titleContent = new GUIContent("Create SFX");
		window.minSize = new Vector2(300, 150);

		window.ShowUtility();
	}

	private void OnGUI ()
	{
		EditorGUILayout.LabelField("Create new SFX", EditorStyles.boldLabel);
		EditorGUILayout.Space();

		sfxName = EditorGUILayout.TextField("Name", sfxName);
		category = (SfxCategory)EditorGUILayout.EnumPopup("Category", category);
		clip = (AudioClip)EditorGUILayout.ObjectField("Clip", clip, typeof(AudioClip), false);

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();

		if (GUILayout.Button("Create"))
		{
			CreateSfx();
			Close();
		}

		if (GUILayout.Button("Cancel"))
		{
			Close();
		}

		EditorGUILayout.EndHorizontal();
	}

	private void CreateSfx ()
	{
		if (database == null)
			return;

		var newSfx = new SfxData
		{
			Id = sfxName,
			Category = category,
			Clip = clip,
			Volume = 1f,
			Pitch = 1f
		};

		database.EditorSounds.Add(newSfx);

		EditorUtility.SetDirty(database);
	}
}