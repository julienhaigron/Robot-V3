using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ComponentTestLabLauncher
{
	private const string c_pendingKey = "ComponentTestLab.Pending";
	private const string c_relaunchKey = "ComponentTestLab.Relaunch";
	private const string c_preventSaveKey = "ComponentTestLab.PreventSave";
	private const string c_ownsStartSceneKey = "ComponentTestLab.OwnsStartScene";

	public static bool IsLabSession => SessionState.GetBool(c_pendingKey, false);

	static ComponentTestLabLauncher ()
	{
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;

		if (!EditorApplication.isPlayingOrWillChangePlaymode)
		{
			RestorePreventSave();
			EditorApplication.delayCall += RefreshStartScene;
		}
	}

	public static void OpenLabScene ()
	{
		ComponentTestLabConfig config = ComponentTestLabConfig.LoadOrCreate();
		if (EditorApplication.isPlayingOrWillChangePlaymode || ComponentTestLabMap.IsLabSceneActive(config))
			return;

		if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			EditorSceneManager.OpenScene(config.ScenePath, OpenSceneMode.Single);
	}

	public static void Launch ()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			return;

		OpenLabScene();
		if (ComponentTestLabMap.IsLabSceneActive(ComponentTestLabConfig.Find()))
			EditorApplication.EnterPlaymode();
	}

	public static void Relaunch ()
	{
		if (!EditorApplication.isPlaying)
		{
			Launch();
			return;
		}

		SessionState.SetBool(c_relaunchKey, true);
		EditorApplication.ExitPlaymode();
	}

	private static void OnActiveSceneChanged ( Scene _previous, Scene _current )
	{
		RefreshStartScene();
	}

	private static void OnPlayModeStateChanged ( PlayModeStateChange _state )
	{
		switch (_state)
		{
			case PlayModeStateChange.ExitingEditMode:
				PrepareLaunch();
				break;
			case PlayModeStateChange.EnteredPlayMode:
				if (IsLabSession)
					ComponentTestLabSession.Begin();
				break;
			case PlayModeStateChange.ExitingPlayMode:
				ComponentTestLabSession.End();
				break;
			case PlayModeStateChange.EnteredEditMode:
				RestorePreventSave();
				SessionState.EraseBool(c_pendingKey);
				RefreshStartScene();
				if (SessionState.GetBool(c_relaunchKey, false))
				{
					SessionState.EraseBool(c_relaunchKey);
					EditorApplication.delayCall += Launch;
				}
				break;
		}
	}

	private static void PrepareLaunch ()
	{
		SessionState.EraseBool(c_pendingKey);
		ComponentTestLabConfig config = ComponentTestLabConfig.Find();
		if (config == null || !ComponentTestLabMap.IsLabSceneActive(config))
			return;

		try
		{
			ComponentTestLabLayout layout = ComponentTestLabLayout.Build(config);
			if (!ComponentTestLabMap.Sync(config, layout, out string error))
				throw new InvalidOperationException(error);

			SceneAsset startScene = FindStartScene();
			if (startScene == null)
				throw new InvalidOperationException("La scène de démarrage du jeu est introuvable dans les Build Settings.");
			EditorSceneManager.playModeStartScene = startScene;
			SessionState.SetBool(c_ownsStartSceneKey, true);

			GameDatas datas = Resources.Load<GameDatas>("GameDatas");
			if (datas == null)
				throw new InvalidOperationException("GameDatas introuvable.");
			if (string.IsNullOrEmpty(SessionState.GetString(c_preventSaveKey, "")))
				SessionState.SetString(c_preventSaveKey, datas.preventSave ? "1" : "0");
			datas.preventSave = true;

			SessionState.SetBool(c_pendingKey, true);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			RestorePreventSave();
			EditorApplication.isPlaying = false;
			EditorUtility.DisplayDialog("Labo de composants", "Le labo ne peut pas démarrer :\n" + exception.Message, "OK");
		}
	}

	private static void RestorePreventSave ()
	{
		string original = SessionState.GetString(c_preventSaveKey, "");
		if (string.IsNullOrEmpty(original))
			return;

		SessionState.EraseString(c_preventSaveKey);
		GameDatas datas = Resources.Load<GameDatas>("GameDatas");
		if (datas != null)
			datas.preventSave = original == "1";
	}

	private static void RefreshStartScene ()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			return;

		ComponentTestLabConfig config = ComponentTestLabConfig.Find();
		SceneAsset startScene = FindStartScene();
		if (config != null && startScene != null && ComponentTestLabMap.IsLabSceneActive(config))
		{
			EditorSceneManager.playModeStartScene = startScene;
			SessionState.SetBool(c_ownsStartSceneKey, true);
		}
		else if (SessionState.GetBool(c_ownsStartSceneKey, false))
		{
			if (EditorSceneManager.playModeStartScene == startScene)
				EditorSceneManager.playModeStartScene = null;
			SessionState.EraseBool(c_ownsStartSceneKey);
		}
	}

	private static SceneAsset FindStartScene ()
	{
		GameConfig gameConfig = GameConfig.current;
		string sceneName = gameConfig != null && gameConfig.game != null ? gameConfig.game.startScreenSceneName : null;
		string fallbackPath = null;
		foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
		{
			if (!buildScene.enabled)
				continue;

			fallbackPath ??= buildScene.path;
			if (Path.GetFileNameWithoutExtension(buildScene.path) == sceneName)
				return AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path);
		}

		return fallbackPath == null ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(fallbackPath);
	}
}
