using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationLabContext
{
	public AnimationLabRig rig;
	public List<AnimationLabAssets.HeldOption> options;
	public string[] optionLabels;
	public Dictionary<EntityActionEnumID, EntityActionData> actions;
	public int hand;
	public string status;
	public AnimationLabSection playbackOwner;

	public static float TickDuration => GameConfig.current != null && GameConfig.current.game != null ? GameConfig.current.game.actionDuration : 1f;

	public AnimationLabAssets.HeldOption HeldOption => rig.heldOptions[hand];
	public bool IsLeftHand => rig.IsHeldInLeftHand(hand);
	public bool IsLooping ( AnimationLabSection _section ) => AnimationLabSession.IsLooping && playbackOwner == _section;

	public void Play ( AnimationLabSection _owner, Func<AnimationLabRig.Plan> _plan )
	{
		playbackOwner = _owner;
		rig.Request = _plan;
		rig.Release();
		rig.Play(_plan());
	}

	public void SetLoop ( AnimationLabSection _owner, Func<AnimationLabRig.Plan> _plan, bool _isLooping )
	{
		if (_isLooping)
		{
			playbackOwner = _owner;
			rig.Request = _plan;
			rig.Release();
		}

		AnimationLabSession.SetLoop(_isLooping);
	}

	public void StopPlayback ()
	{
		AnimationLabSession.SetLoop(false);
		rig.Feedback.Clear();
		rig.Release();
	}
}

public abstract class AnimationLabSection
{
	public abstract string Key { get; }
	public abstract string Title { get; }
	public abstract string Help { get; }

	public virtual void Reset ()
	{
	}

	public abstract void Draw ( AnimationLabContext _context );
}

public static class AnimationLabGUI
{
	public const float Padding = 8f;
	public static readonly Color EntryColor = new(.35f, .7f, 1f);
	public static readonly Color HoldColor = new(1f, .72f, .2f);
	public static readonly Color ImpactColor = new(1f, .35f, .3f);
	public static readonly Color RecoveryColor = new(.72f, .5f, 1f);
	public static readonly Color ExitColor = new(.45f, .9f, .45f);
	public static readonly Color Background = new(.13f, .13f, .13f);

	private static GUIStyle s_segmentStyle;

	public static string Ticks ( int _count )
	{
		return _count + (_count > 1 ? " ticks" : " tick");
	}

	public static bool IsMultiTick ( EntityActionData _action )
	{
		return _action.GetTokenPreparationCost(null, null, null) > 0 || _action.GetTokenCooldownCost(null, null, null) > 0 || _action.tokenDuration > 1;
	}

	public static string TicksLabel ( EntityActionData _action )
	{
		return IsMultiTick(_action) ? "   (" + _action.GetTokenPreparationCost(null, null, null) + " + " + _action.tokenDuration + " + " + _action.GetTokenCooldownCost(null, null, null) + " ticks)" : "";
	}

	public static void Marker ( float _x, Rect _rect, Color _color )
	{
		EditorGUI.DrawRect(new Rect(_x - 1.5f, _rect.y + 2f, 3f, _rect.height - 14f), _color);
		EditorGUI.DrawRect(new Rect(_x - 4f, _rect.y + 2f, 8f, 5f), _color);
	}

	public static void Legend ( Color _color, string _text )
	{
		Rect swatch = GUILayoutUtility.GetRect(10f, 14f, GUILayout.Width(10f));
		swatch.y += 3f;
		swatch.height = 8f;
		EditorGUI.DrawRect(swatch, _color);
		GUILayout.Label(_text, EditorStyles.miniLabel);
	}

	public static Color ColorOf ( AnimationLabRig.SegmentKind _kind )
	{
		return _kind switch
		{
			AnimationLabRig.SegmentKind.Preparation => HoldColor,
			AnimationLabRig.SegmentKind.Recovery => RecoveryColor,
			_ => ImpactColor
		};
	}

	public static void PlanBar ( AnimationLabContext _context, AnimationLabSection _owner, AnimationLabRig.Plan _preview )
	{
		bool isPlaying = _context.rig.IsSequencePlaying && _context.playbackOwner == _owner;
		AnimationLabRig.Plan plan = isPlaying ? _context.rig.CurrentPlan : _preview;
		Rect rect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
		if (Event.current.type != EventType.Repaint || plan == null || plan.segments.Count == 0)
			return;

		float total = Mathf.Max(.01f, plan.Duration);
		float X ( float _time ) => rect.x + Padding + Mathf.Clamp01(_time / total) * (rect.width - 2f * Padding);
		s_segmentStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, normal = { textColor = Color.black } };
		EditorGUI.DrawRect(rect, Background);
		foreach (AnimationLabRig.Segment segment in plan.segments)
		{
			Rect bar = new(X(segment.start) + 1f, rect.y + 2f, Mathf.Max(1f, X(segment.end) - X(segment.start) - 2f), rect.height - 8f);
			EditorGUI.DrawRect(bar, ColorOf(segment.kind));
			GUI.Label(bar, segment.label + " " + (segment.end - segment.start).ToString("0.00") + " s", s_segmentStyle);
		}

		foreach (float mark in plan.marks)
			EditorGUI.DrawRect(new Rect(X(mark) - 1f, rect.yMax - 7f, 2f, 6f), Color.white);
		if (isPlaying)
			EditorGUI.DrawRect(new Rect(X(_context.rig.PlayTime) - 1f, rect.y, 2f, rect.height), Color.white);
	}

	public static bool PlayRow ( AnimationLabContext _context, AnimationLabSection _owner, string _label, string _tooltip, Func<AnimationLabRig.Plan> _plan )
	{
		bool isPlayed = false;
		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button(new GUIContent(_label, _tooltip), GUILayout.Height(24)))
			{
				_context.Play(_owner, _plan);
				isPlayed = true;
			}

			bool isLooping = _context.IsLooping(_owner);
			bool loop = GUILayout.Toggle(isLooping, new GUIContent("Boucle", "Rejoue l'action en boucle, avec les réglages en cours."), "Button", GUILayout.Width(70), GUILayout.Height(24));
			if (loop != isLooping)
			{
				_context.SetLoop(_owner, _plan, loop);
				isPlayed |= loop;
			}
		}

		return isPlayed;
	}

	public static string TriggerPopup ( AnimationLabRig _rig, string _label, string _current )
	{
		List<string> triggers = AnimationLabAssets.TriggerNames(_rig.Animator.runtimeAnimatorController);
		if (triggers.Count == 0)
		{
			EditorGUILayout.LabelField(_label, "aucun déclencheur dans le contrôleur");
			return _current;
		}

		int index = Mathf.Max(0, triggers.IndexOf(_current));
		return triggers[EditorGUILayout.Popup(new GUIContent(_label, "Déclencheur du contrôleur joué à ce moment-là : il faut un état du calque Top qui y réponde."), index, triggers.ToArray())];
	}

	public static string KeyPopup ( GUIContent _label, string _current, List<string> _keys, Func<string, bool> _exists, string _none )
	{
		List<string> keys = new(_keys);
		if (!string.IsNullOrEmpty(_current) && !keys.Contains(_current))
			keys.Add(_current);

		string[] labels = new[] { _none }.Concat(keys.Select(key => _exists(key) ? key : key + " (absente du contrôleur)")).ToArray();
		int index = string.IsNullOrEmpty(_current) ? 0 : keys.IndexOf(_current) + 1;
		int chosen = EditorGUILayout.Popup(_label, index, labels);
		return chosen == index ? _current : chosen == 0 ? "" : keys[chosen - 1];
	}

	public static void UnitTuning ( AnimationLabContext _context, EntityActionData _action, string _trigger, string _title, ref bool _showDefaults, Action _onChanged )
	{
		AnimationLabRig rig = _context.rig;
		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField(_title, EditorStyles.miniBoldLabel);
		using (new EditorGUI.DisabledScope(true))
			EditorGUILayout.ObjectField(new GUIContent("Profil d'animation", "Profil branché sur l'EntitySkinPlugin du prefab d'unité. Ses réglages ne concernent que cette unité."), rig.Profile, typeof(EntityAnimationProfile), false);

		(float speed, float intensity) tuning = rig.TuningFor(_action, _trigger);
		EditorGUI.BeginChangeCheck();
		float speed = EditorGUILayout.Slider(new GUIContent("Vitesse ×", "Multiplie la vitesse du clip pour cette action, sur cette unité seulement. Le délai d'impact en jeu est divisé d'autant : l'impact reste synchronisé."), tuning.speed, EntityAnimationProfile.MinSpeed, EntityAnimationProfile.MaxSpeed);
		float intensity = EditorGUILayout.Slider(new GUIContent("Intensité", "Poids du calque d'attaque pour cette action, sur cette unité seulement. 1 joue le geste entier ; moins garde une part de la pose de repos et réduit l'amplitude."), tuning.intensity, EntityAnimationProfile.MinIntensity, 1f);
		if (EditorGUI.EndChangeCheck())
		{
			rig.SetTuning(_action, _trigger, speed, intensity);
			_onChanged?.Invoke();
		}

		bool isSaved = rig.IsTuningSaved(_action, _trigger);
		EntityAnimationProfile.TuningSource source = rig.SourceFor(_action, _trigger);
		string origin = source switch
		{
			EntityAnimationProfile.TuningSource.Action => "Réglage propre à " + _action.name + ".",
			EntityAnimationProfile.TuningSource.Trigger => "Hérité du réglage de " + _trigger + ", partagé par ses actions sans réglage propre.",
			_ => "Valeurs par défaut de l'unité."
		};
		EditorGUILayout.LabelField(isSaved ? origin : origin + " Modifié, pas encore enregistré.", EditorStyles.wordWrappedMiniLabel);

		if (rig.Profile == null)
		{
			EditorGUILayout.HelpBox("Cette unité n'a pas de profil d'animation : ses attaques jouent à vitesse ×1 et intensité 1.", MessageType.Info);
			if (GUILayout.Button("Créer le profil de l'unité"))
			{
				EntityAnimationProfile profile = AnimationLabAssets.CreateProfile(AnimationLabSession.UnitPrefab, out string path);
				rig.SetProfile(profile);
				_context.status = profile != null ? "Profil créé dans " + path + " et branché sur " + AnimationLabSession.UnitPrefab.name + "." : "Impossible de créer le profil : EntitySkinPlugin introuvable sur le prefab d'unité.";
				GUIUtility.ExitGUI();
			}

			return;
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			using (new EditorGUI.DisabledScope(isSaved && source == EntityAnimationProfile.TuningSource.Action))
			{
				if (GUILayout.Button(new GUIContent("Enregistrer pour " + _action.name, "Écrit ces deux valeurs dans le profil de l'unité, pour cette action seulement.")))
				{
					(float speed, float intensity) live = rig.TuningFor(_action, _trigger);
					AnimationLabAssets.SaveTuning(rig.Profile, _action, _trigger, live.speed, live.intensity);
					rig.ForgetTuning(_action, _trigger);
					_context.status = _action.name + " enregistré dans " + AssetDatabase.GetAssetPath(rig.Profile) + " : vitesse ×" + live.speed.ToString("0.00") + ", intensité " + live.intensity.ToString("0.00") + ".";
					GUIUtility.ExitGUI();
				}
			}

			using (new EditorGUI.DisabledScope(isSaved))
			{
				if (GUILayout.Button("Annuler"))
				{
					rig.ForgetTuning(_action, _trigger);
					_onChanged?.Invoke();
				}
			}

			using (new EditorGUI.DisabledScope(source != EntityAnimationProfile.TuningSource.Action))
			{
				if (GUILayout.Button(new GUIContent("Retirer", "Supprime le réglage propre à cette action : elle reprend le réglage de son déclencheur ou les valeurs par défaut de l'unité.")))
				{
					AnimationLabAssets.RemoveTuning(rig.Profile, _action, _trigger);
					rig.ForgetTuning(_action, _trigger);
					_context.status = "Réglage propre à " + _action.name + " retiré de " + AssetDatabase.GetAssetPath(rig.Profile) + ".";
					GUIUtility.ExitGUI();
				}
			}
		}

		UnitDefaults(_context, _action, _trigger, ref _showDefaults, _onChanged);
	}

	private static void UnitDefaults ( AnimationLabContext _context, EntityActionData _action, string _trigger, ref bool _showDefaults, Action _onChanged )
	{
		AnimationLabRig rig = _context.rig;
		_showDefaults = EditorGUILayout.Foldout(_showDefaults, new GUIContent("Valeurs par défaut de l'unité", "Utilisées par les actions qui n'ont ni réglage propre ni réglage de leur déclencheur."), true);
		if (!_showDefaults)
			return;

		using (new EditorGUI.IndentLevelScope())
		{
			(float speed, float intensity) defaults = rig.DefaultTuning;
			EditorGUI.BeginChangeCheck();
			float speed = EditorGUILayout.Slider("Vitesse ×", defaults.speed, EntityAnimationProfile.MinSpeed, EntityAnimationProfile.MaxSpeed);
			float intensity = EditorGUILayout.Slider("Intensité", defaults.intensity, EntityAnimationProfile.MinIntensity, 1f);
			if (EditorGUI.EndChangeCheck())
			{
				rig.SetDefaults(speed, intensity);
				rig.ApplyTuning(_action, _trigger);
				_onChanged?.Invoke();
			}

			using (new EditorGUILayout.HorizontalScope())
			using (new EditorGUI.DisabledScope(rig.AreDefaultsSaved))
			{
				if (GUILayout.Button("Enregistrer les valeurs par défaut"))
				{
					(float speed, float intensity) live = rig.DefaultTuning;
					AnimationLabAssets.SaveDefaults(rig.Profile, live.speed, live.intensity);
					rig.ForgetDefaults();
					_context.status = "Valeurs par défaut enregistrées dans " + AssetDatabase.GetAssetPath(rig.Profile) + " : vitesse ×" + live.speed.ToString("0.00") + ", intensité " + live.intensity.ToString("0.00") + ".";
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Annuler"))
				{
					rig.ForgetDefaults();
					rig.ApplyTuning(_action, _trigger);
				}
			}
		}
	}
}

public class AnimationLabHandsSection : AnimationLabSection
{
	private AnimationLabAssets.HeldOption m_guardFor;
	private int m_guardHand = -1;
	private string m_guardKey;
	private float m_guardTorso;

	public override string Key => "Hands";
	public override string Title => "Mains et garde";
	public override string Help => "Mains et garde : choisis ce que tient chaque main. La main jouée (en haut) sert à l'attaque, au tir et à la prise. Les mains qui tiennent quelque chose se ferment, à la fermeture réglée pour l'unité. Chaque arme a une garde, l'Idle joué au repos avec elle (les états « Guard <nom> R/L » du contrôleur) ; « Rotation du buste » remet le buste et les bras de face sans bouger la tête ni les jambes, « De face » la calcule. L'unité revient au repos neutre pour se déplacer.";

	public override void Reset ()
	{
		m_guardFor = null;
	}

	public override void Draw ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		string[] labels = { "Main principale (os hand.right, emplacements 1 et 3)", "Main secondaire (os hand.left, emplacements 2 et 4)" };
		for (int hand = 0; hand < 2; hand++)
		{
			int current = _context.options.IndexOf(rig.heldOptions[hand]) + 1;
			int chosen = EditorGUILayout.Popup(labels[hand], current, _context.optionLabels);
			if (chosen != current)
				Equip(_context, hand, chosen == 0 ? null : _context.options[chosen - 1]);
		}

		if (GUILayout.Button("Échanger les mains"))
		{
			AnimationLabAssets.HeldOption main = rig.heldOptions[AnimationLabRig.MainHand];
			Equip(_context, AnimationLabRig.MainHand, rig.heldOptions[AnimationLabRig.OffHand]);
			Equip(_context, AnimationLabRig.OffHand, main);
		}

		float grip = EditorGUILayout.Slider(new GUIContent("Fermeture des mains", "Fermeture des doigts sur l'arme ou l'outil tenu, pour cette unité : 1 = la prise complète, 0 = les doigts suivent l'animation."), rig.IK.GripAmount, 0f, 1f);
		if (!Mathf.Approximately(grip, rig.IK.GripAmount))
			rig.IK.GripAmount = grip;

		if (rig.Profile != null)
		{
			using (new EditorGUI.DisabledScope(Mathf.Abs(rig.IK.GripAmount - rig.Profile.gripAmount) < .005f))
			{
				if (GUILayout.Button("Enregistrer la fermeture dans le profil de l'unité"))
				{
					AnimationLabAssets.SaveGripAmount(rig.Profile, rig.IK.GripAmount);
					_context.status = "Fermeture des mains " + rig.IK.GripAmount.ToString("0.00") + " enregistrée dans " + AssetDatabase.GetAssetPath(rig.Profile) + ".";
					GUIUtility.ExitGUI();
				}
			}
		}

		DrawGuard(_context);
	}

	public static void Equip ( AnimationLabContext _context, int _hand, AnimationLabAssets.HeldOption _option )
	{
		_context.rig.Equip(_hand, _option);
		AnimationLabSession.SaveHeld(_hand, _option);
	}

	private void DrawGuard ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		if (_context.HeldOption?.data is not WeaponEquipmentData weapon)
			return;

		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Garde de " + weapon.name + " (main jouée)", EditorStyles.miniBoldLabel);
		bool isLeftHand = _context.IsLeftHand;
		if (m_guardFor != _context.HeldOption || m_guardHand != _context.hand)
		{
			m_guardFor = _context.HeldOption;
			m_guardHand = _context.hand;
			m_guardKey = weapon.guardAnimation ?? "";
			m_guardTorso = weapon.guardTorsoYaw;
		}

		m_guardKey = AnimationLabGUI.KeyPopup(new GUIContent("Garde", "Idle joué au repos avec cette arme : les états « Guard <nom> R » et « Guard <nom> L » du calque de base, en miroir dans l'autre main."),
			m_guardKey, AnimationLabAssets.GuardKeys(rig.Animator.runtimeAnimatorController), key => rig.Guard.HasState(key, isLeftHand), "Aucune : repos neutre");

		using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_guardKey)))
		using (new EditorGUILayout.HorizontalScope())
		{
			m_guardTorso = EditorGUILayout.Slider(new GUIContent("Rotation du buste", "Tourne le buste et les bras dans la garde, sans bouger la tête ni les jambes. Valeur de la main principale : l'autre main prend l'angle opposé."), m_guardTorso, -GuardAnimationPlayer.TorsoRange, GuardAnimationPlayer.TorsoRange);
			if (GUILayout.Button(new GUIContent("De face", "Calcule la rotation qui met les épaules face à la direction de l'unité dans cette garde."), GUILayout.Width(70)))
				m_guardTorso = Mathf.Round(rig.FrontTorsoYaw());
		}

		rig.SetGuard(m_guardKey, isLeftHand, m_guardTorso);
		bool isSaved = m_guardKey == (weapon.guardAnimation ?? "") && Mathf.Abs(m_guardTorso - weapon.guardTorsoYaw) < .5f;
		using (new EditorGUI.DisabledScope(isSaved))
		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Enregistrer la garde dans " + weapon.name))
			{
				AnimationLabAssets.SaveGuard(weapon, m_guardKey, m_guardTorso);
				_context.status = "Garde de " + weapon.name + " enregistrée : " + (string.IsNullOrEmpty(weapon.guardAnimation) ? "repos neutre" : weapon.guardAnimation + ", buste " + weapon.guardTorsoYaw.ToString("0") + "°") + ".";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler", GUILayout.Width(70)))
			{
				m_guardKey = weapon.guardAnimation ?? "";
				m_guardTorso = weapon.guardTorsoYaw;
			}
		}
	}
}

public class AnimationLabGripSection : AnimationLabSection
{
	private int m_target;
	private AnimationLabAssets.HeldOption m_twinsFor;
	private List<AnimationLabAssets.HeldOption> m_twins;

	public override string Key => "Grip";
	public override string Title => "Prise en main";
	public override string Help => "Prise : fige la pose à l'impact, puis tourne et place l'arme (champs, boutons +90 ou outils W/E de la Scene après « Sélectionner »). L'enregistrement écrit la pose de la racine du prefab de l'arme ; « Enregistrer aussi dans les armes au même visuel » l'écrit dans toutes les armes qui ont le même mesh à la même place. La main secondaire en reçoit le miroir, en jeu comme ici. Le socket principal déplace toutes les armes à la fois : à réserver au calage de la main elle-même.";

	public override void Reset ()
	{
		m_twinsFor = null;
	}

	public override void Draw ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		m_target = GUILayout.Toolbar(m_target, new[] { "Arme de la main jouée", "Socket principal (toutes les armes)" });
		bool isSocket = m_target == 1;
		Transform target = isSocket ? rig.IK.handGrabSocket : rig.held[_context.hand] != null ? rig.held[_context.hand].transform : null;
		if (target == null)
		{
			EditorGUILayout.HelpBox(isSocket ? "Ce skin n'a pas de socket principal." : "Aucune arme dans la main jouée.", MessageType.Info);
			return;
		}

		bool isMirrored = !isSocket && target.parent == rig.IK.offHandGrabSocket && rig.IK.offHandGrabSocket != rig.IK.handGrabSocket;
		EditorGUILayout.HelpBox(isSocket
				? "Le socket principal est sur la main principale. Le socket secondaire en est le miroir, recalculé au rechargement de l'unité : les armes de la main secondaire ne suivent qu'après « Enregistrer »."
				: isMirrored
					? "Arme dans la main secondaire : les valeurs affichées sont le miroir de la prise. L'enregistrement les reconvertit pour la main principale, et les deux mains restent symétriques."
					: "La prise est la pose de la racine du prefab de l'arme dans le socket. La main secondaire en reçoit le miroir automatiquement.",
			MessageType.None);

		EditorGUI.BeginChangeCheck();
		Vector3 position = EditorGUILayout.Vector3Field("Position locale", target.localPosition);
		Vector3 euler = EditorGUILayout.Vector3Field("Rotation locale", target.localEulerAngles);
		if (EditorGUI.EndChangeCheck())
		{
			target.localPosition = position;
			target.localEulerAngles = euler;
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("X +90"))
				target.localRotation *= Quaternion.Euler(90f, 0f, 0f);
			if (GUILayout.Button("Y +90"))
				target.localRotation *= Quaternion.Euler(0f, 90f, 0f);
			if (GUILayout.Button("Z +90"))
				target.localRotation *= Quaternion.Euler(0f, 0f, 90f);
			if (GUILayout.Button(new GUIContent("Sélectionner", "Sélectionne la cible : utilise les outils Déplacer (W) et Tourner (E) de la Scene, en repère Local.")))
			{
				Selection.activeGameObject = target.gameObject;
				Tools.pivotRotation = PivotRotation.Local;
				SceneView.lastActiveSceneView?.FrameSelected();
			}
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button(isSocket ? "Enregistrer dans le skin" : "Enregistrer dans le prefab de l'arme", GUILayout.Height(22)))
			{
				Save(_context, target, isSocket, isMirrored);
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Recharger"))
				Reload(_context, isSocket);

			if (GUILayout.Button("Zéro"))
			{
				target.localPosition = Vector3.zero;
				target.localRotation = Quaternion.identity;
			}
		}

		if (!isSocket)
			DrawTwins(_context, target, isMirrored);
	}

	private void DrawTwins ( AnimationLabContext _context, Transform _target, bool _isMirrored )
	{
		AnimationLabAssets.HeldOption option = _context.HeldOption;
		if (option != m_twinsFor)
		{
			m_twinsFor = option;
			m_twins = AnimationLabAssets.FindSameVisual(option, _context.options);
		}

		if (option == null || m_twins == null || m_twins.Count == 0)
			return;

		string names = string.Join(", ", m_twins.Select(twin => twin.prefab.name));
		if (!GUILayout.Button(new GUIContent("Enregistrer aussi dans les " + m_twins.Count + " armes au même visuel", "Même mesh à la même pose sous la racine : " + names + ".")))
			return;

		if (EditorUtility.DisplayDialog("Animation Lab", "La prise va être écrite dans " + option.prefab.name + " et dans :\n\n" + names, "Enregistrer", "Annuler"))
		{
			MainHandGrip(_target, _isMirrored, out Vector3 position, out Quaternion rotation);
			HashSet<GameObject> saved = new(m_twins.Select(twin => twin.prefab)) { option.prefab };
			foreach (GameObject prefab in saved)
				AnimationLabAssets.SaveGrip(prefab, position, rotation);
			ReEquip(_context, saved);
			_context.status = "Prise enregistrée dans " + option.prefab.name + " et " + names + ".";
		}

		GUIUtility.ExitGUI();
	}

	private static void MainHandGrip ( Transform _target, bool _isMirrored, out Vector3 _position, out Quaternion _rotation )
	{
		_position = _target.localPosition;
		_rotation = _target.localRotation;
		if (_isMirrored)
			HumanoidEntityIK.MirrorPose(ref _position, ref _rotation);
	}

	private static void ReEquip ( AnimationLabContext _context, HashSet<GameObject> _prefabs )
	{
		for (int hand = 0; hand < 2; hand++)
		{
			if (_context.rig.heldOptions[hand] != null && _prefabs.Contains(_context.rig.heldOptions[hand].prefab))
				_context.rig.Equip(hand, _context.rig.heldOptions[hand]);
		}
	}

	private static void Save ( AnimationLabContext _context, Transform _target, bool _isSocket, bool _isMirrored )
	{
		if (_isSocket)
		{
			if (!AnimationLabAssets.SaveMainSocket(AnimationLabSession.UnitPrefab, _target.localPosition, _target.localRotation, out string path))
			{
				_context.status = "Impossible d'enregistrer le socket : skin introuvable.";
				return;
			}

			_context.status = "Socket principal enregistré dans " + path + ". L'unité est rechargée pour recalculer le socket secondaire.";
			AnimationLabSession.Rebuild();
			return;
		}

		MainHandGrip(_target, _isMirrored, out Vector3 position, out Quaternion rotation);
		AnimationLabAssets.HeldOption option = _context.HeldOption;
		AnimationLabAssets.SaveGrip(option.prefab, position, rotation);
		ReEquip(_context, new HashSet<GameObject> { option.prefab });
		_context.status = "Prise enregistrée dans " + AssetDatabase.GetAssetPath(option.prefab) + ".";
	}

	private static void Reload ( AnimationLabContext _context, bool _isSocket )
	{
		if (!_isSocket)
		{
			_context.rig.Equip(_context.hand, _context.HeldOption);
			return;
		}

		if (AnimationLabAssets.TryReadMainSocket(AnimationLabSession.UnitPrefab, out Vector3 position, out Quaternion rotation))
		{
			_context.rig.IK.handGrabSocket.localPosition = position;
			_context.rig.IK.handGrabSocket.localRotation = rotation;
		}
	}
}
