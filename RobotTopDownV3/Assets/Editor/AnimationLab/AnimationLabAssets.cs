using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FIMSpace.FProceduralAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimationLabAssets
{
	public const string HandParameter = "LeftHandAttack";
	public const string MovingParameter = "IsMoving";
	public const string AttackLayer = EntitySkinPlugin.AttackLayerName;
	public const string ProfileFolder = "Assets/Animations/Profiles";

	public class HeldOption
	{
		public EntityEquipmentData data;
		public GameObject prefab;
		public string label;
	}

	public class AttackTiming
	{
		public string trigger;
		public AnimatorController controller;
		public AnimatorState right;
		public AnimatorState left;
		public AnimatorStateTransition rightEnter;
		public AnimatorStateTransition leftEnter;
		public AnimationClip clip;
		public float speed = 1f;
		public float offset;
		public float exitTime = 1f;
		public float enterDuration = .1f;
		public float exitDuration = .25f;
		public float assetSpeed = 1f;
		public float assetOffset;

		public float ClipLength => clip != null && clip.length > 0f ? clip.length : 1f;
		public bool HasSeparateClips => right != null && left != null && right.motion != left.motion;

		public AnimatorState StateFor ( bool _leftHand )
		{
			return _leftHand ? left ?? right : right ?? left;
		}

		public float HitDelayFor ( float _impact )
		{
			return Mathf.Max(0f, (_impact - offset) * ClipLength / Mathf.Max(.01f, speed));
		}

		public float ImpactFor ( float _hitDelay )
		{
			return Mathf.Clamp01(offset + _hitDelay * Mathf.Max(.01f, speed) / ClipLength);
		}

		public float AssetDelayFor ( float _normalizedTime )
		{
			return Mathf.Max(0f, (_normalizedTime - assetOffset) * ClipLength / Mathf.Max(.01f, assetSpeed));
		}

		public float AssetNormalizedFor ( float _delay )
		{
			return Mathf.Clamp01(assetOffset + _delay * Mathf.Max(.01f, assetSpeed) / ClipLength);
		}
	}

	public static List<HeldOption> LoadHeldOptions ()
	{
		List<HeldOption> options = new();
		foreach (string guid in AssetDatabase.FindAssets("t:EntityEquipmentData"))
		{
			EntityEquipmentData data = AssetDatabase.LoadAssetAtPath<EntityEquipmentData>(AssetDatabase.GUIDToAssetPath(guid));
			GameObject prefab = data switch
			{
				WeaponEquipmentData weapon when weapon.prefab != null => weapon.prefab.gameObject,
				ToolEquipmentData tool when tool.prefab != null => tool.prefab.gameObject,
				_ => null
			};

			if (prefab != null)
				options.Add(new HeldOption { data = data, prefab = prefab, label = (data is ToolEquipmentData ? "Outils/" : "Armes/") + data.name });
		}

		return options.OrderBy(option => option.label).ToList();
	}

	public static Dictionary<EntityActionEnumID, EntityActionData> LoadActions ()
	{
		Dictionary<EntityActionEnumID, EntityActionData> actions = new();
		GameAssets assets = GameAssets.current;
		if (assets != null && assets.game != null && assets.game.entityActionsData != null)
		{
			foreach (KeyValuePair<EntityActionEnumID, EntityActionData> pair in assets.game.entityActionsData)
			{
				if (pair.Value != null)
					actions[pair.Key] = pair.Value;
			}
		}

		if (actions.Count > 0)
			return actions;

		foreach (string guid in AssetDatabase.FindAssets("t:EntityActionData"))
		{
			EntityActionData action = AssetDatabase.LoadAssetAtPath<EntityActionData>(AssetDatabase.GUIDToAssetPath(guid));
			if (action != null && !actions.ContainsKey(action.enumID))
				actions.Add(action.enumID, action);
		}

		return actions;
	}

	public static List<EntityActionData> GetAnimatedActions ( EntityEquipmentData _data, Dictionary<EntityActionEnumID, EntityActionData> _actions )
	{
		List<EntityActionData> result = new();
		if (_data == null || _data.knownedActions == null)
			return result;

		foreach (EntityActionEnumID id in _data.knownedActions)
		{
			if (_actions.TryGetValue(id, out EntityActionData action) && !string.IsNullOrEmpty(action.performAnimationKey) && !result.Contains(action))
				result.Add(action);
		}

		return result;
	}

	public static List<EntityActionData> FindActionsUsing ( string _trigger, Dictionary<EntityActionEnumID, EntityActionData> _actions )
	{
		return _actions.Values.Where(action => action.performAnimationKey == _trigger).Distinct().OrderBy(action => action.name).ToList();
	}

	public static AttackTiming ReadTiming ( RuntimeAnimatorController _runtimeController, string _trigger )
	{
		if (_runtimeController is not AnimatorController controller || string.IsNullOrEmpty(_trigger))
			return null;

		AnimatorControllerLayer layer = controller.layers.FirstOrDefault(candidate => candidate.name == AttackLayer);
		if (layer == null)
			return null;

		AttackTiming timing = new() { trigger = _trigger, controller = controller };
		foreach (AnimatorStateTransition transition in layer.stateMachine.anyStateTransitions)
		{
			if (transition.destinationState == null || !transition.conditions.Any(condition => condition.parameter == _trigger && condition.mode == AnimatorConditionMode.If))
				continue;

			if (transition.conditions.Any(condition => condition.parameter == HandParameter && condition.mode == AnimatorConditionMode.If))
			{
				timing.left = transition.destinationState;
				timing.leftEnter = transition;
			}
			else
			{
				timing.right = transition.destinationState;
				timing.rightEnter = transition;
			}
		}

		AnimatorState reference = timing.right ?? timing.left;
		if (reference == null)
			return null;

		AnimatorStateTransition enter = timing.right != null ? timing.rightEnter : timing.leftEnter;
		AnimatorStateTransition exit = reference.transitions.FirstOrDefault(transition => transition.hasExitTime);
		timing.clip = reference.motion as AnimationClip;
		timing.speed = reference.speed;
		timing.offset = enter.offset;
		timing.assetSpeed = timing.speed;
		timing.assetOffset = timing.offset;
		timing.enterDuration = enter.duration;
		timing.exitTime = exit != null ? exit.exitTime : 1f;
		timing.exitDuration = exit != null ? exit.duration : .25f;
		return timing;
	}

	public static void WriteTiming ( AttackTiming _timing )
	{
		foreach ((AnimatorState state, AnimatorStateTransition enter) in new[] { (_timing.right, _timing.rightEnter), (_timing.left, _timing.leftEnter) })
		{
			if (state == null)
				continue;

			state.speed = _timing.speed;
			EditorUtility.SetDirty(state);
			if (enter != null)
			{
				enter.offset = _timing.offset;
				enter.duration = _timing.enterDuration;
				EditorUtility.SetDirty(enter);
			}

			foreach (AnimatorStateTransition exit in state.transitions.Where(transition => transition.hasExitTime))
			{
				exit.exitTime = _timing.exitTime;
				exit.duration = _timing.exitDuration;
				EditorUtility.SetDirty(exit);
			}
		}

		EditorUtility.SetDirty(_timing.controller);
		AssetDatabase.SaveAssets();
	}

	public static void WriteHitDelay ( IEnumerable<EntityActionData> _actions, float _hitDelay )
	{
		float rounded = Mathf.Round(_hitDelay * 1000f) / 1000f;
		foreach (EntityActionData action in _actions)
		{
			action.performAnimationHitDelay = rounded;
			EditorUtility.SetDirty(action);
		}

		AssetDatabase.SaveAssets();
	}

	public static void RescalePhaseDelays ( IEnumerable<EntityActionData> _actions, AttackTiming _timing )
	{
		foreach (EntityActionData action in _actions)
		{
			float hold = Round(_timing.HitDelayFor(_timing.AssetNormalizedFor(action.preparationHoldDelay)), 1000f);
			float recovery = Round(_timing.HitDelayFor(_timing.AssetNormalizedFor(action.recoveryHoldDelay)), 1000f);
			bool rescalesHold = action.preparationAnimationMode == EntityActionData.PreparationAnimationMode.WindUp && !Mathf.Approximately(hold, action.preparationHoldDelay);
			bool rescalesRecovery = action.recoveryAnimationMode != EntityActionData.RecoveryAnimationMode.None && !Mathf.Approximately(recovery, action.recoveryHoldDelay);
			if (!rescalesHold && !rescalesRecovery)
				continue;

			if (rescalesHold)
				action.preparationHoldDelay = hold;
			if (rescalesRecovery)
				action.recoveryHoldDelay = recovery;
			EditorUtility.SetDirty(action);
		}

		AssetDatabase.SaveAssets();
	}

	public static void WritePhases ( EntityActionData _action, EntityActionData.PreparationAnimationMode _preparation, float _holdDelay, float _windUpRatio, string _preparationKey
		, EntityActionData.RecoveryAnimationMode _recovery, float _recoveryDelay, string _recoveryKey )
	{
		_action.preparationAnimationMode = _preparation;
		_action.preparationHoldDelay = Round(_holdDelay, 1000f);
		_action.preparationWindUpRatio = Round(_windUpRatio, 100f);
		_action.preparationAnimationKey = _preparationKey ?? "";
		_action.recoveryAnimationMode = _recovery;
		_action.recoveryHoldDelay = Round(_recoveryDelay, 1000f);
		_action.afterPerformAnimationKey = _recoveryKey ?? "";
		EditorUtility.SetDirty(_action);
		AssetDatabase.SaveAssets();
	}

	public static List<string> TriggerNames ( RuntimeAnimatorController _runtimeController )
	{
		return _runtimeController is AnimatorController controller
			? controller.parameters.Where(parameter => parameter.type == AnimatorControllerParameterType.Trigger).Select(parameter => parameter.name).ToList()
			: new List<string>();
	}

	public static EntityAnimationProfile FindProfile ( GameObject _unitPrefab )
	{
		EntitySkinPlugin skin = _unitPrefab != null ? _unitPrefab.GetComponentInChildren<EntitySkinPlugin>(true) : null;
		return skin != null ? skin.AnimationProfile : null;
	}

	public static void SaveTuning ( EntityAnimationProfile _profile, EntityActionData _action, float _speed, float _intensity )
	{
		EntityAnimationProfile.AttackTuning tuning = _profile.FindOwn(_action);
		if (tuning == null)
		{
			tuning = new EntityAnimationProfile.AttackTuning { action = _action };
			_profile.attacks.Add(tuning);
		}

		tuning.trigger = _action.performAnimationKey;
		tuning.speed = Round(_speed, 100f);
		tuning.intensity = Round(_intensity, 100f);
		EditorUtility.SetDirty(_profile);
		AssetDatabase.SaveAssets();
	}

	public static void RemoveTuning ( EntityAnimationProfile _profile, EntityActionData _action )
	{
		_profile.attacks.RemoveAll(tuning => tuning != null && tuning.action == _action);
		EditorUtility.SetDirty(_profile);
		AssetDatabase.SaveAssets();
	}

	public static void SaveDefaults ( EntityAnimationProfile _profile, float _speed, float _intensity )
	{
		_profile.attackSpeed = Round(_speed, 100f);
		_profile.attackIntensity = Round(_intensity, 100f);
		EditorUtility.SetDirty(_profile);
		AssetDatabase.SaveAssets();
	}

	private static float Round ( float _value, float _steps )
	{
		return Mathf.Round(_value * _steps) / _steps;
	}

	public static List<HeldOption> FindSameVisual ( HeldOption _option, IEnumerable<HeldOption> _options )
	{
		List<HeldOption> result = new();
		if (_option == null || _option.prefab == null)
			return result;

		List<(string mesh, Matrix4x4 pose)> reference = VisualParts(_option.prefab);
		if (reference.Count == 0)
			return result;

		HashSet<GameObject> seen = new() { _option.prefab };
		foreach (HeldOption candidate in _options)
		{
			if (candidate != null && candidate.prefab != null && seen.Add(candidate.prefab) && IsSameVisual(reference, VisualParts(candidate.prefab)))
				result.Add(candidate);
		}

		return result;
	}

	private static List<(string mesh, Matrix4x4 pose)> VisualParts ( GameObject _prefab )
	{
		List<(string mesh, Matrix4x4 pose)> parts = new();
		Transform root = _prefab.transform;
		foreach (Renderer renderer in _prefab.GetComponentsInChildren<Renderer>(true))
		{
			Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
				: renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
			if (mesh != null && renderer.enabled && IsActiveUnder(renderer.transform, root))
				parts.Add((AssetDatabase.GetAssetPath(mesh) + "/" + mesh.name, root.worldToLocalMatrix * renderer.transform.localToWorldMatrix));
		}

		return parts;
	}

	private static bool IsActiveUnder ( Transform _transform, Transform _root )
	{
		for (Transform current = _transform; current != null; current = current.parent)
		{
			if (!current.gameObject.activeSelf)
				return false;
			if (current == _root)
				return true;
		}

		return true;
	}

	private static bool IsSameVisual ( List<(string mesh, Matrix4x4 pose)> _reference, List<(string mesh, Matrix4x4 pose)> _candidate )
	{
		if (_reference.Count != _candidate.Count)
			return false;

		bool[] isUsed = new bool[_candidate.Count];
		foreach ((string mesh, Matrix4x4 pose) part in _reference)
		{
			int match = -1;
			for (int i = 0; i < _candidate.Count && match < 0; i++)
			{
				if (!isUsed[i] && _candidate[i].mesh == part.mesh && IsClose(_candidate[i].pose, part.pose))
					match = i;
			}

			if (match < 0)
				return false;
			isUsed[match] = true;
		}

		return true;
	}

	private static bool IsClose ( Matrix4x4 _a, Matrix4x4 _b )
	{
		for (int i = 0; i < 16; i++)
		{
			if (Mathf.Abs(_a[i] - _b[i]) > .001f)
				return false;
		}

		return true;
	}

	public static EntityAnimationProfile CreateProfile ( GameObject _unitPrefab, out string _path )
	{
		_path = null;
		string prefabPath = AssetDatabase.GetAssetPath(_unitPrefab);
		if (string.IsNullOrEmpty(prefabPath))
			return null;

		GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
		try
		{
			EntitySkinPlugin skin = root.GetComponentInChildren<EntitySkinPlugin>(true);
			if (skin == null)
				return null;

			if (!AssetDatabase.IsValidFolder(ProfileFolder))
				AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(ProfileFolder).Replace('\\', '/'), System.IO.Path.GetFileName(ProfileFolder));

			EntityAnimationProfile profile = ScriptableObject.CreateInstance<EntityAnimationProfile>();
			_path = AssetDatabase.GenerateUniqueAssetPath(ProfileFolder + "/" + _unitPrefab.name + ".asset");
			AssetDatabase.CreateAsset(profile, _path);

			SerializedObject serialized = new(skin);
			serialized.FindProperty("m_animationProfile").objectReferenceValue = profile;
			serialized.ApplyModifiedPropertiesWithoutUndo();
			PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
			AssetDatabase.SaveAssets();
			return profile;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	public static void SaveGrip ( GameObject _prefab, Vector3 _localPosition, Quaternion _localRotation )
	{
		string path = AssetDatabase.GetAssetPath(_prefab);
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			root.transform.localPosition = _localPosition;
			root.transform.localRotation = _localRotation;
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	public static string FindSkinPrefabPath ( GameObject _unitPrefab )
	{
		HumanoidEntityIK assetIK = _unitPrefab != null ? _unitPrefab.GetComponentInChildren<HumanoidEntityIK>(true) : null;
		if (assetIK == null)
			return null;

		HumanoidEntityIK source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(assetIK);
		return AssetDatabase.GetAssetPath(source != null ? source : assetIK);
	}

	public static bool TryReadMainSocket ( GameObject _unitPrefab, out Vector3 _localPosition, out Quaternion _localRotation )
	{
		HumanoidEntityIK assetIK = _unitPrefab != null ? _unitPrefab.GetComponentInChildren<HumanoidEntityIK>(true) : null;
		_localPosition = assetIK != null && assetIK.handGrabSocket != null ? assetIK.handGrabSocket.localPosition : Vector3.zero;
		_localRotation = assetIK != null && assetIK.handGrabSocket != null ? assetIK.handGrabSocket.localRotation : Quaternion.identity;
		return assetIK != null && assetIK.handGrabSocket != null;
	}

	public static bool SaveMainSocket ( GameObject _unitPrefab, Vector3 _localPosition, Quaternion _localRotation, out string _path )
	{
		_path = FindSkinPrefabPath(_unitPrefab);
		if (string.IsNullOrEmpty(_path))
			return false;

		GameObject root = PrefabUtility.LoadPrefabContents(_path);
		try
		{
			HumanoidEntityIK ik = root.GetComponentInChildren<HumanoidEntityIK>(true);
			if (ik == null || ik.handGrabSocket == null)
				return false;

			ik.handGrabSocket.localPosition = _localPosition;
			ik.handGrabSocket.localRotation = _localRotation;
			PrefabUtility.SaveAsPrefabAsset(root, _path);
			return true;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	public static Dictionary<string, string> Snapshot ( Object _target )
	{
		Dictionary<string, string> values = new();
		if (_target == null)
			return values;

		SerializedObject serialized = new(_target);
		SerializedProperty property = serialized.GetIterator();
		bool enterChildren = true;
		while (property.Next(enterChildren))
		{
			enterChildren = property.propertyType == SerializedPropertyType.Generic;
			string value = Describe(property);
			if (value != null)
				values[property.propertyPath] = value;
		}

		return values;
	}

	public static List<string> ChangedPaths ( Dictionary<string, string> _baseline, HashSet<string> _noise, Object _target )
	{
		List<string> changed = new();
		if (_baseline == null || _target == null)
			return changed;

		foreach (KeyValuePair<string, string> pair in Snapshot(_target))
		{
			if (_noise != null && _noise.Contains(pair.Key))
				continue;
			if (!_baseline.TryGetValue(pair.Key, out string before) || before != pair.Value)
				changed.Add(pair.Key);
		}

		return changed;
	}

	public static string DescribeChange ( Dictionary<string, string> _baseline, Object _target, string _path )
	{
		SerializedProperty property = new SerializedObject(_target).FindProperty(_path);
		string before = _baseline != null && _baseline.TryGetValue(_path, out string value) ? value : "?";
		string after = property != null ? Describe(property) : "?";
		return property != null && property.propertyType == SerializedPropertyType.AnimationCurve ? _path + " (courbe)" : _path + " : " + before + " -> " + after;
	}

	public static int SaveLegs ( GameObject _unitPrefab, LegsAnimator _source, IReadOnlyCollection<string> _paths )
	{
		string path = AssetDatabase.GetAssetPath(_unitPrefab);
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		int copied = 0;
		try
		{
			LegsAnimator target = root.GetComponentInChildren<LegsAnimator>(true);
			if (target == null)
				return 0;

			SerializedObject source = new(_source);
			SerializedObject destination = new(target);
			foreach (string propertyPath in _paths)
			{
				SerializedProperty from = source.FindProperty(propertyPath);
				SerializedProperty to = destination.FindProperty(propertyPath);
				if (from == null || to == null || from.propertyType != to.propertyType)
					continue;

				destination.CopyFromSerializedProperty(from);
				copied++;
			}

			destination.ApplyModifiedPropertiesWithoutUndo();
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}

		return copied;
	}

	private static string Describe ( SerializedProperty _property )
	{
		CultureInfo culture = CultureInfo.InvariantCulture;
		switch (_property.propertyType)
		{
			case SerializedPropertyType.Integer:
				return _property.longValue.ToString(culture);
			case SerializedPropertyType.LayerMask:
			case SerializedPropertyType.Character:
			case SerializedPropertyType.Enum:
				return _property.intValue.ToString(culture);
			case SerializedPropertyType.Boolean:
				return _property.boolValue ? "true" : "false";
			case SerializedPropertyType.Float:
				return _property.doubleValue.ToString("G7", culture);
			case SerializedPropertyType.String:
				return _property.stringValue;
			case SerializedPropertyType.Color:
				return _property.colorValue.ToString("G5");
			case SerializedPropertyType.Vector2:
				return _property.vector2Value.ToString("G5");
			case SerializedPropertyType.Vector3:
				return _property.vector3Value.ToString("G5");
			case SerializedPropertyType.Vector4:
				return _property.vector4Value.ToString("G5");
			case SerializedPropertyType.Quaternion:
				return _property.quaternionValue.ToString("G5");
			case SerializedPropertyType.Rect:
				return _property.rectValue.ToString("G5");
			case SerializedPropertyType.Bounds:
				return _property.boundsValue.ToString("G5");
			case SerializedPropertyType.Vector2Int:
				return _property.vector2IntValue.ToString();
			case SerializedPropertyType.Vector3Int:
				return _property.vector3IntValue.ToString();
			case SerializedPropertyType.AnimationCurve:
				return DescribeCurve(_property.animationCurveValue);
			default:
				return null;
		}
	}

	private static string DescribeCurve ( AnimationCurve _curve )
	{
		if (_curve == null)
			return "null";

		StringBuilder builder = new();
		builder.Append((int)_curve.preWrapMode).Append('/').Append((int)_curve.postWrapMode);
		foreach (Keyframe key in _curve.keys)
		{
			builder.Append(';').Append(key.time.ToString("G6", CultureInfo.InvariantCulture))
				.Append(',').Append(key.value.ToString("G6", CultureInfo.InvariantCulture))
				.Append(',').Append(key.inTangent.ToString("G6", CultureInfo.InvariantCulture))
				.Append(',').Append(key.outTangent.ToString("G6", CultureInfo.InvariantCulture))
				.Append(',').Append((int)key.weightedMode)
				.Append(',').Append(key.inWeight.ToString("G6", CultureInfo.InvariantCulture))
				.Append(',').Append(key.outWeight.ToString("G6", CultureInfo.InvariantCulture));
		}

		return builder.ToString();
	}
}
