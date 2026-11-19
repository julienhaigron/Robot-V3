using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AnimationLabSession
{
	public const string ScenePath = "Assets/Scenes/Test/AnimationLab.unity";
	private const string c_defaultUnitPath = "Assets/Prefabs/Entity/Paladin_Tank_Armor.prefab";
	private const string c_unitKey = "AnimationLab.Unit";
	private const string c_heldKey = "AnimationLab.Held";
	private const int c_baselineFrame = 10;
	private const int c_noiseFrame = 90;
	private const float c_loopPause = .35f;

	public static AnimationLabRig Rig { get; private set; }
	public static string Message { get; private set; }
	public static Dictionary<string, string> LegsBaseline { get; private set; }
	public static HashSet<string> LegsNoise { get; private set; }
	public static bool IsLegsBaselineReady => LegsBaseline != null && LegsNoise != null;

	public static bool IsLooping { get; private set; }
	private static float s_idleSince = -1f;
	private static int s_buildFrame;
	private static Dictionary<string, string> s_firstSnapshot;

	static AnimationLabSession ()
	{
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		EditorApplication.update += Update;
	}

	public static bool IsLabSceneActive => SceneManager.GetActiveScene().path == ScenePath;

	public static GameObject UnitPrefab
	{
		get
		{
			string path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(c_unitKey, ""));
			GameObject prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
			return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(c_defaultUnitPath);
		}
		set => EditorPrefs.SetString(c_unitKey, value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : "");
	}

	public static string GetSavedHeld ( int _hand )
	{
		return EditorPrefs.GetString(c_heldKey + _hand, "");
	}

	public static void SaveHeld ( int _hand, AnimationLabAssets.HeldOption _option )
	{
		EditorPrefs.SetString(c_heldKey + _hand, _option != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_option.data)) : "");
	}

	public static void OpenOrCreateScene ()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
		{
			EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
			return;
		}

		Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
		GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
		floor.name = "Floor";
		floor.transform.localScale = new Vector3(4f, 1f, 4f);
		Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
		if (camera != null)
		{
			Vector3 position = new(2.2f, 2.4f, 4.4f);
			camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(0f, 1.1f, 0f) - position));
		}

		Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
		EditorSceneManager.SaveScene(scene, ScenePath);
	}

	public static void Launch ()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			return;

		if (!IsLabSceneActive)
			OpenOrCreateScene();
		if (IsLabSceneActive)
			EditorApplication.EnterPlaymode();
	}

	public static void Rebuild ()
	{
		Teardown();
		if (!EditorApplication.isPlaying || !IsLabSceneActive)
			return;

		GameObject prefab = UnitPrefab;
		if (prefab == null)
		{
			Message = "Choisis un prefab d'unité.";
			return;
		}

		try
		{
			Rig = AnimationLabRig.Build(prefab);
			List<AnimationLabAssets.HeldOption> options = AnimationLabAssets.LoadHeldOptions();
			for (int hand = 0; hand < 2; hand++)
			{
				string guid = GetSavedHeld(hand);
				AnimationLabAssets.HeldOption option = options.FirstOrDefault(candidate => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(candidate.data)) == guid);
				if (option == null && hand == AnimationLabRig.MainHand && string.IsNullOrEmpty(guid))
					option = options.FirstOrDefault(candidate => candidate.data is WeaponEquipmentData);
				Rig.Equip(hand, option);
			}

			s_buildFrame = Time.frameCount;
			s_firstSnapshot = null;
			LegsBaseline = null;
			LegsNoise = null;
			Message = null;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Message = exception.Message;
			Teardown();
		}
	}

	public static void SetLoop ( bool _isLooping )
	{
		IsLooping = _isLooping;
		s_idleSince = -1f;
	}

	private static void Teardown ()
	{
		IsLooping = false;
		if (Rig != null)
			Rig.Teardown();
		Rig = null;
	}

	private static void OnPlayModeStateChanged ( PlayModeStateChange _state )
	{
		switch (_state)
		{
			case PlayModeStateChange.EnteredPlayMode:
				Rebuild();
				break;
			case PlayModeStateChange.ExitingPlayMode:
				Time.timeScale = 1f;
				Teardown();
				break;
		}
	}

	private static void Update ()
	{
		if (!EditorApplication.isPlaying || Rig == null || !Rig.IsAlive)
			return;

		Rig.TickGuard(Time.deltaTime);
		CaptureLegsBaseline();

		if (!IsLooping || Rig.Animator.speed == 0f || Rig.Request == null)
			return;

		if (Rig.IsSequencePlaying || !Rig.IsTopIdle())
		{
			s_idleSince = -1f;
			return;
		}

		if (s_idleSince < 0f)
			s_idleSince = Time.realtimeSinceStartup;
		else if (Time.realtimeSinceStartup - s_idleSince >= c_loopPause)
		{
			s_idleSince = -1f;
			Rig.Play(Rig.Request.Value);
		}
	}

	private static void CaptureLegsBaseline ()
	{
		if (Rig.Legs == null || IsLegsBaselineReady)
			return;

		int elapsed = Time.frameCount - s_buildFrame;
		if (s_firstSnapshot == null && elapsed >= c_baselineFrame)
			s_firstSnapshot = AnimationLabAssets.Snapshot(Rig.Legs);
		else if (s_firstSnapshot != null && elapsed >= c_noiseFrame)
		{
			Dictionary<string, string> second = AnimationLabAssets.Snapshot(Rig.Legs);
			LegsNoise = new HashSet<string>(second.Where(pair => !s_firstSnapshot.TryGetValue(pair.Key, out string first) || first != pair.Value).Select(pair => pair.Key));
			LegsBaseline = second;
		}
	}
}
