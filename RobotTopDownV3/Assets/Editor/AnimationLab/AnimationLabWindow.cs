using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationLabWindow : EditorWindow
{
	private const string c_strikeLeftKey = "AnimationLab.StrikeOffHand";
	private const string c_actionKey = "AnimationLab.Action";
	private const string c_walkTilesKey = "AnimationLab.WalkTiles";
	private const string c_walkDurationKey = "AnimationLab.WalkDuration";
	private const string c_walkLoopKey = "AnimationLab.WalkLoop";
	private const float c_timelinePadding = 8f;
	private const float c_markerPickDistance = 7f;
	private const float c_markerGap = .01f;

	private enum Marker { None, Entry, Hold, Impact, Recovery, Exit, Playhead }

	private static readonly Color s_entryColor = new(.35f, .7f, 1f);
	private static readonly Color s_holdColor = new(1f, .72f, .2f);
	private static readonly Color s_impactColor = new(1f, .35f, .3f);
	private static readonly Color s_recoveryColor = new(.72f, .5f, 1f);
	private static readonly Color s_exitColor = new(.45f, .9f, .45f);
	private static readonly string[] s_preparationLabels = { "Aucune", "Armer la frappe", "Déclencheur dédié" };
	private static readonly string[] s_recoveryLabels = { "Aucune", "Tenir la fin du geste", "Déclencheur dédié" };

	private Vector2 m_scroll;
	private List<AnimationLabAssets.HeldOption> m_options;
	private string[] m_optionLabels;
	private Dictionary<EntityActionEnumID, EntityActionData> m_actions;
	private int m_strikeHand;
	private string m_actionName;
	private AnimationLabAssets.AttackTiming m_timing;
	private string m_timingTrigger;
	private float m_impact;
	private float m_playhead;
	private bool m_isScrubbing;
	private bool m_isTimingDirty;
	private EntityActionData m_phaseAction;
	private EntityActionData.PreparationAnimationMode m_preparationMode;
	private float m_hold;
	private float m_windUpRatio = .8f;
	private string m_preparationKey;
	private EntityActionData.RecoveryAnimationMode m_recoveryMode;
	private float m_recovery;
	private string m_recoveryKey;
	private bool m_isPhaseDirty;
	private bool m_showDefaults;
	private AnimationLabAssets.HeldOption m_twinsFor;
	private List<AnimationLabAssets.HeldOption> m_twins;
	private AnimationLabAssets.HeldOption m_guardFor;
	private int m_guardHand = -1;
	private string m_guardKey;
	private float m_guardTorso;
	private Marker m_dragged;
	private float m_timeScale = 1f;
	private bool m_loop;
	private int m_gripTarget;
	private int m_walkTiles;
	private float m_walkTileDuration;
	private bool m_walkLoop;
	private List<string> m_legChanges;
	private bool m_showHelp;
	private string m_status;
	private double m_lastRepaint;

	[MenuItem("Tools/Animation Lab")]
	public static void Open ()
	{
		GetWindow<AnimationLabWindow>("Animation Lab");
	}

	private void OnEnable ()
	{
		m_strikeHand = EditorPrefs.GetInt(c_strikeLeftKey, 0);
		m_actionName = EditorPrefs.GetString(c_actionKey, "");
		m_walkTiles = EditorPrefs.GetInt(c_walkTilesKey, 3);
		m_walkTileDuration = EditorPrefs.GetFloat(c_walkDurationKey, -1f);
		m_walkLoop = EditorPrefs.GetBool(c_walkLoopKey, true);
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private void OnDisable ()
	{
		EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
	}

	private void OnPlayModeStateChanged ( PlayModeStateChange _state )
	{
		m_timing = null;
		m_timingTrigger = null;
		m_isScrubbing = false;
		m_isTimingDirty = false;
		m_phaseAction = null;
		m_isPhaseDirty = false;
		m_twinsFor = null;
		m_guardFor = null;
		m_loop = false;
		m_timeScale = 1f;
		m_legChanges = null;
		m_status = null;
		Repaint();
	}

	private void Update ()
	{
		if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - m_lastRepaint < .033)
			return;

		m_lastRepaint = EditorApplication.timeSinceStartup;
		Repaint();
	}

	private void OnGUI ()
	{
		EnsureOptions();
		m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
		AnimationLabRig rig = DrawSetup();
		if (rig != null)
		{
			DrawHands(rig);
			DrawAttack(rig);
			DrawGrip(rig);
			DrawWalk(rig);
		}

		DrawHelp();
		EditorGUILayout.EndScrollView();
	}

	#region Setup

	private void EnsureOptions ()
	{
		if (m_options != null)
			return;

		m_options = AnimationLabAssets.LoadHeldOptions();
		m_optionLabels = new[] { "Rien" }.Concat(m_options.Select(option => option.label)).ToArray();
		m_actions = AnimationLabAssets.LoadActions();
	}

	private AnimationLabRig DrawSetup ()
	{
		EditorGUILayout.LabelField("Animation Lab", EditorStyles.boldLabel);
		GameObject unit = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Unité", "Prefab d'unité, par exemple Paladin_Tank_Armor. Le skin, ses réglages de jambes et ses sockets viennent de là."), AnimationLabSession.UnitPrefab, typeof(GameObject), false);
		if (unit != AnimationLabSession.UnitPrefab)
		{
			AnimationLabSession.UnitPrefab = unit;
			if (EditorApplication.isPlaying)
				AnimationLabSession.Rebuild();
		}

		if (!EditorApplication.isPlaying)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Ouvrir la scène du Lab"))
					Defer(AnimationLabSession.OpenOrCreateScene);
				if (GUILayout.Button("▶ Lancer le Lab"))
					Defer(AnimationLabSession.Launch);
			}

			EditorGUILayout.HelpBox(AnimationLabSession.IsLabSceneActive ? "Lance le Play : l'unité apparaît au centre de la scène." : "Le Lab tourne dans sa propre scène, sans le jeu. « Lancer le Lab » l'ouvre (ou la crée) et lance le Play.", MessageType.Info);
			return null;
		}

		if (!AnimationLabSession.IsLabSceneActive)
		{
			EditorGUILayout.HelpBox("Le Lab ne tourne que dans sa scène : " + AnimationLabSession.ScenePath, MessageType.Info);
			return null;
		}

		AnimationLabRig rig = AnimationLabSession.Rig;
		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button(new GUIContent("Recharger l'unité", "Réinstancie l'unité depuis son prefab : utile après avoir enregistré un socket ou modifié le prefab à la main.")))
			{
				m_timing = null;
				Defer(AnimationLabSession.Rebuild);
			}

			using (new EditorGUI.DisabledScope(rig == null || !rig.IsAlive))
			{
				if (GUILayout.Button("Sélectionner l'unité"))
					Selection.activeGameObject = rig.Root;
				if (GUILayout.Button("Recentrer"))
					rig.ResetPosition();
			}
		}

		if (!string.IsNullOrEmpty(m_status))
			EditorGUILayout.HelpBox(m_status, MessageType.None);

		if (rig == null || !rig.IsAlive)
		{
			EditorGUILayout.HelpBox(AnimationLabSession.Message ?? "L'unité n'est pas encore créée.", MessageType.Warning);
			return null;
		}

		return rig;
	}

	#endregion

	#region Hands

	private void DrawHands ( AnimationLabRig _rig )
	{
		Header("Mains");
		string[] labels = { "Main principale (os hand.right, emplacements 1 et 3)", "Main secondaire (os hand.left, emplacements 2 et 4)" };
		for (int hand = 0; hand < 2; hand++)
		{
			int current = m_options.IndexOf(_rig.heldOptions[hand]) + 1;
			int chosen = EditorGUILayout.Popup(labels[hand], current, m_optionLabels);
			if (chosen != current)
				Equip(_rig, hand, chosen == 0 ? null : m_options[chosen - 1]);
		}

		if (GUILayout.Button("Échanger les mains"))
		{
			AnimationLabAssets.HeldOption main = _rig.heldOptions[AnimationLabRig.MainHand];
			Equip(_rig, AnimationLabRig.MainHand, _rig.heldOptions[AnimationLabRig.OffHand]);
			Equip(_rig, AnimationLabRig.OffHand, main);
		}

		float grip = EditorGUILayout.Slider(new GUIContent("Fermeture des mains", "Fermeture des doigts sur l'arme ou l'outil tenu, pour cette unité : 1 = la prise complète, 0 = les doigts suivent l'animation."), _rig.IK.GripAmount, 0f, 1f);
		if (!Mathf.Approximately(grip, _rig.IK.GripAmount))
			_rig.IK.GripAmount = grip;

		if (_rig.Profile == null)
			return;

		using (new EditorGUI.DisabledScope(Mathf.Abs(_rig.IK.GripAmount - _rig.Profile.gripAmount) < .005f))
		{
			if (GUILayout.Button("Enregistrer la fermeture dans le profil de l'unité"))
			{
				AnimationLabAssets.SaveGripAmount(_rig.Profile, _rig.IK.GripAmount);
				m_status = "Fermeture des mains " + _rig.IK.GripAmount.ToString("0.00") + " enregistrée dans " + AssetDatabase.GetAssetPath(_rig.Profile) + ".";
				GUIUtility.ExitGUI();
			}
		}
	}

	private void DrawGuard ( AnimationLabRig _rig, AnimationLabAssets.HeldOption _option, bool _isLeftHand )
	{
		if (_option.data is not WeaponEquipmentData weapon)
			return;

		if (m_guardFor != _option || m_guardHand != m_strikeHand)
		{
			m_guardFor = _option;
			m_guardHand = m_strikeHand;
			m_guardKey = weapon.guardAnimation ?? "";
			m_guardTorso = weapon.guardTorsoYaw;
		}

		List<string> keys = AnimationLabAssets.GuardKeys(_rig.Animator.runtimeAnimatorController);
		if (!string.IsNullOrEmpty(m_guardKey) && !keys.Contains(m_guardKey))
			keys.Add(m_guardKey);
		string[] labels = new[] { "Aucune : repos neutre" }.Concat(keys.Select(key => _rig.Guard.HasState(key, _isLeftHand) ? key : key + " (absente du contrôleur)")).ToArray();
		int index = string.IsNullOrEmpty(m_guardKey) ? 0 : keys.IndexOf(m_guardKey) + 1;
		int chosen = EditorGUILayout.Popup(new GUIContent("Garde de l'arme", "Idle joué au repos avec cette arme : les états « Guard <nom> R » et « Guard <nom> L » du calque de base, en miroir dans l'autre main. L'unité revient au repos neutre pour marcher."), index, labels);
		if (chosen != index)
			m_guardKey = chosen == 0 ? "" : keys[chosen - 1];

		using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_guardKey)))
		using (new EditorGUILayout.HorizontalScope())
		{
			m_guardTorso = EditorGUILayout.Slider(new GUIContent("Rotation du buste", "Tourne le buste et les bras dans la garde, sans bouger la tête ni les jambes. Valeur de la main principale : l'autre main prend l'angle opposé."), m_guardTorso, -GuardAnimationPlayer.TorsoRange, GuardAnimationPlayer.TorsoRange);
			if (GUILayout.Button(new GUIContent("De face", "Calcule la rotation qui met les épaules face à la direction de l'unité dans cette garde."), GUILayout.Width(70)))
				m_guardTorso = Mathf.Round(_rig.FrontTorsoYaw());
		}

		_rig.SetGuard(m_guardKey, _isLeftHand, m_guardTorso);
		bool isSaved = m_guardKey == (weapon.guardAnimation ?? "") && Mathf.Abs(m_guardTorso - weapon.guardTorsoYaw) < .5f;
		using (new EditorGUI.DisabledScope(isSaved))
		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Enregistrer la garde dans " + weapon.name))
			{
				AnimationLabAssets.SaveGuard(weapon, m_guardKey, m_guardTorso);
				m_status = "Garde de " + weapon.name + " enregistrée : " + (string.IsNullOrEmpty(weapon.guardAnimation) ? "repos neutre" : weapon.guardAnimation + ", buste " + weapon.guardTorsoYaw.ToString("0") + "°") + ".";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler", GUILayout.Width(70)))
			{
				m_guardKey = weapon.guardAnimation ?? "";
				m_guardTorso = weapon.guardTorsoYaw;
			}
		}
	}

	private void Equip ( AnimationLabRig _rig, int _hand, AnimationLabAssets.HeldOption _option )
	{
		_rig.Equip(_hand, _option);
		AnimationLabSession.SaveHeld(_hand, _option);
		m_timing = null;
	}

	#endregion

	#region Attack

	private void DrawAttack ( AnimationLabRig _rig )
	{
		Header("Attaque");
		int hand = GUILayout.Toolbar(m_strikeHand, new[] { "Main principale", "Main secondaire" });
		if (hand != m_strikeHand)
		{
			m_strikeHand = hand;
			EditorPrefs.SetInt(c_strikeLeftKey, hand);
			StopPlayback(_rig);
		}

		AnimationLabAssets.HeldOption option = _rig.heldOptions[m_strikeHand];
		if (option == null)
		{
			EditorGUILayout.HelpBox("Mets une arme dans cette main pour jouer ses attaques.", MessageType.Info);
			return;
		}

		List<EntityActionData> actions = AnimationLabAssets.GetAnimatedActions(option.data, m_actions);
		if (actions.Count == 0)
		{
			EditorGUILayout.HelpBox(option.data.name + " n'a aucune action animée (performAnimationKey vide).", MessageType.Info);
			return;
		}

		int actionIndex = Mathf.Max(0, actions.FindIndex(action => action.name == m_actionName));
		int chosenAction = EditorGUILayout.Popup("Action", actionIndex, actions.Select(action => action.name + "   " + action.performAnimationKey + TicksLabel(action)).ToArray());
		EntityActionData selected = actions[chosenAction];
		if (selected.name != m_actionName)
		{
			m_actionName = selected.name;
			EditorPrefs.SetString(c_actionKey, m_actionName);
		}

		EnsureTiming(_rig, selected);
		if (m_timing == null)
		{
			EditorGUILayout.HelpBox("Aucun état du calque « " + AnimationLabAssets.AttackLayer + " » ne répond au déclencheur " + selected.performAnimationKey + ".", MessageType.Warning);
			return;
		}

		bool isLeftHand = _rig.IsHeldInLeftHand(m_strikeHand);
		AnimatorState state = m_timing.StateFor(isLeftHand);
		_rig.SetClipSpeedScale(m_timing.trigger, m_timing.speed / Mathf.Max(.01f, m_timing.assetSpeed));
		_rig.Request = BuildRequest(_rig, selected, isLeftHand);
		EditorGUILayout.LabelField("État joué", state.name + "   (clip " + (m_timing.clip != null ? m_timing.clip.name : "aucun") + ", " + m_timing.ClipLength.ToString("0.00") + " s)");
		if (m_timing.HasSeparateClips)
			EditorGUILayout.HelpBox("Les états R et L ont des clips différents : le timing affiché est celui de " + (m_timing.right != null ? m_timing.right.name : state.name) + ".", MessageType.Warning);
		DrawGuard(_rig, option, isLeftHand);

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button(IsMultiTick(selected) ? "▶ Jouer l'action comme en jeu" : "▶ Jouer comme en jeu", GUILayout.Height(24)))
			{
				m_isScrubbing = false;
				_rig.Release();
				_rig.Play(_rig.Request.Value);
			}

			bool loop = GUILayout.Toggle(m_loop, "Boucle", "Button", GUILayout.Width(70), GUILayout.Height(24));
			if (loop != m_loop)
			{
				m_loop = loop;
				AnimationLabSession.SetLoop(m_loop);
				if (m_loop)
				{
					m_isScrubbing = false;
					_rig.Release();
				}
			}
		}

		float timeScale = EditorGUILayout.Slider(new GUIContent("Vitesse du temps", "Ralenti global du Lab (Time.timeScale). N'est pas enregistré."), m_timeScale, .05f, 1f);
		if (!Mathf.Approximately(timeScale, m_timeScale))
		{
			m_timeScale = timeScale;
			Time.timeScale = m_timeScale;
		}

		bool scrub = EditorGUILayout.ToggleLeft(new GUIContent("Image par image", "Fige l'animation : la tête de lecture et les repères se déplacent dans la frise."), m_isScrubbing);
		if (scrub != m_isScrubbing)
		{
			m_isScrubbing = scrub;
			if (m_isScrubbing)
			{
				m_loop = false;
				AnimationLabSession.SetLoop(false);
				m_playhead = m_impact;
				_rig.Scrub(state, m_playhead);
			}
			else
				_rig.Release();
		}

		if (!m_isScrubbing && _rig.TryGetPlayingTime(state, out float playing))
			m_playhead = Mathf.Clamp01(playing);

		DrawTimeline(_rig, state);
		DrawActionPlan(_rig, selected);
		DrawTimingFields(_rig, selected);
		DrawPhases(_rig, state, selected, isLeftHand);
		DrawUnitTuning(_rig, state, selected);
	}

	private static string TicksLabel ( EntityActionData _action )
	{
		int preparation = _action.GetTokenPreparationCost(null, null, null);
		int cooldown = _action.GetTokenCooldownCost(null, null, null);
		return IsMultiTick(_action) ? "   (" + preparation + " + " + _action.tokenDuration + " + " + cooldown + " ticks)" : "";
	}

	private static bool IsMultiTick ( EntityActionData _action )
	{
		return _action.GetTokenPreparationCost(null, null, null) > 0 || _action.GetTokenCooldownCost(null, null, null) > 0 || _action.tokenDuration > 1;
	}

	private static string Ticks ( int _count )
	{
		return _count + (_count > 1 ? " ticks" : " tick");
	}

	private static float TickDuration => GameConfig.current != null && GameConfig.current.game != null ? GameConfig.current.game.actionDuration : 1f;

	private bool ShowsHold => m_phaseAction != null && m_phaseAction.GetTokenPreparationCost(null, null, null) > 0 && m_preparationMode == EntityActionData.PreparationAnimationMode.WindUp;

	private bool ShowsRecovery => m_phaseAction != null && m_phaseAction.GetTokenCooldownCost(null, null, null) > 0 && m_recoveryMode != EntityActionData.RecoveryAnimationMode.None;

	private AnimationLabRig.PlayRequest BuildRequest ( AnimationLabRig _rig, EntityActionData _action, bool _isLeftHand )
	{
		WeaponEquipmentData weapon = _rig.heldOptions[m_strikeHand] != null ? _rig.heldOptions[m_strikeHand].data as WeaponEquipmentData : null;
		return new AnimationLabRig.PlayRequest
		{
			action = _action,
			key = _action.performAnimationKey,
			timing = CurrentTiming(_action),
			isLeftHand = _isLeftHand,
			preparationTicks = _action.GetTokenPreparationCost(null, null, null),
			performTicks = Mathf.Max(1, _action.tokenDuration),
			cooldownTicks = _action.GetTokenCooldownCost(null, null, null),
			tickDuration = TickDuration,
			strikeTail = weapon != null ? weapon.singleAttackAnimationDuration : .3f
		};
	}

	private AttackAnimationPlayer.Timing CurrentTiming ( EntityActionData _action )
	{
		AttackAnimationPlayer.Timing timing = AttackAnimationPlayer.Timing.From(_action);
		timing.hitDelay = m_timing.AssetDelayFor(m_impact);
		if (m_phaseAction != _action)
			return timing;

		timing.preparation = m_preparationMode;
		timing.preparationKey = m_preparationKey;
		timing.holdDelay = m_timing.AssetDelayFor(m_hold);
		timing.windUpRatio = m_windUpRatio;
		timing.recovery = m_recoveryMode;
		timing.recoveryKey = m_recoveryKey;
		timing.recoveryDelay = m_timing.AssetDelayFor(m_recovery);
		return timing;
	}

	private void EnsureTiming ( AnimationLabRig _rig, EntityActionData _action )
	{
		if (m_timing == null || m_timingTrigger != _action.performAnimationKey)
		{
			ReloadTiming(_rig, _action);
			m_phaseAction = null;
		}

		if (m_timing == null || m_phaseAction == _action)
			return;

		LoadPhases(_action);
		_rig.ApplyTuning(_action, m_timing.trigger);
		if (m_isScrubbing)
			_rig.Scrub(m_timing.StateFor(_rig.IsHeldInLeftHand(m_strikeHand)), m_playhead);
	}

	private void ReloadTiming ( AnimationLabRig _rig, EntityActionData _action )
	{
		m_timing = AnimationLabAssets.ReadTiming(_rig.Animator.runtimeAnimatorController, _action.performAnimationKey);
		m_timingTrigger = _action.performAnimationKey;
		m_isTimingDirty = false;
		if (m_timing == null)
			return;

		m_impact = m_timing.AssetNormalizedFor(_action.performAnimationHitDelay);
		m_playhead = m_impact;
	}

	private void LoadPhases ( EntityActionData _action )
	{
		m_phaseAction = _action;
		m_preparationMode = _action.preparationAnimationMode;
		m_hold = m_timing.AssetNormalizedFor(_action.preparationHoldDelay);
		m_windUpRatio = _action.preparationWindUpRatio;
		m_preparationKey = _action.preparationAnimationKey;
		m_recoveryMode = _action.recoveryAnimationMode;
		m_recovery = m_timing.AssetNormalizedFor(_action.recoveryHoldDelay);
		m_recoveryKey = _action.afterPerformAnimationKey;
		m_isPhaseDirty = false;
	}

	private void StopPlayback ( AnimationLabRig _rig )
	{
		m_loop = false;
		AnimationLabSession.SetLoop(false);
		m_isScrubbing = false;
		m_timing = null;
		_rig.Release();
	}

	private void DrawTimeline ( AnimationLabRig _rig, AnimatorState _state )
	{
		Rect rect = GUILayoutUtility.GetRect(10f, 38f, GUILayout.ExpandWidth(true));
		float length = m_timing.ClipLength;
		bool showsHold = ShowsHold;
		bool showsRecovery = ShowsRecovery;
		float X ( float _t ) => rect.x + c_timelinePadding + Mathf.Clamp01(_t) * (rect.width - 2f * c_timelinePadding);
		float T ( float _x ) => Mathf.Clamp01((_x - rect.x - c_timelinePadding) / Mathf.Max(1f, rect.width - 2f * c_timelinePadding));

		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(rect, new Color(.13f, .13f, .13f));
			EditorGUI.DrawRect(new Rect(X(m_timing.offset), rect.y + 6f, Mathf.Max(1f, X(m_timing.exitTime) - X(m_timing.offset)), rect.height - 18f), new Color(.24f, .3f, .38f));
			if (showsHold)
				EditorGUI.DrawRect(new Rect(X(m_timing.offset), rect.yMax - 16f, Mathf.Max(1f, X(m_hold) - X(m_timing.offset)), 3f), s_holdColor);
			if (showsRecovery)
				EditorGUI.DrawRect(new Rect(X(m_impact), rect.yMax - 16f, Mathf.Max(1f, X(m_recovery) - X(m_impact)), 3f), s_recoveryColor);

			DrawMarker(X(m_timing.offset), rect, s_entryColor);
			if (showsHold)
				DrawMarker(X(m_hold), rect, s_holdColor);
			DrawMarker(X(m_impact), rect, s_impactColor);
			if (showsRecovery)
				DrawMarker(X(m_recovery), rect, s_recoveryColor);
			DrawMarker(X(m_timing.exitTime), rect, s_exitColor);
			EditorGUI.DrawRect(new Rect(X(m_playhead) - 1f, rect.y, 2f, rect.height - 12f), Color.white);
			GUI.Label(new Rect(rect.x + 2f, rect.yMax - 13f, 60f, 13f), "0 s", EditorStyles.miniLabel);
			GUI.Label(new Rect(rect.xMax - 60f, rect.yMax - 13f, 58f, 13f), length.ToString("0.00") + " s", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperRight });
		}

		Event current = Event.current;
		switch (current.type)
		{
			case EventType.MouseDown when current.button == 0 && rect.Contains(current.mousePosition):
				m_dragged = PickMarker(current.mousePosition.x, X, showsHold, showsRecovery);
				ApplyDrag(_rig, _state, T(current.mousePosition.x));
				current.Use();
				break;
			case EventType.MouseDrag when m_dragged != Marker.None:
				ApplyDrag(_rig, _state, T(current.mousePosition.x));
				current.Use();
				break;
			case EventType.MouseUp when m_dragged != Marker.None:
				m_dragged = Marker.None;
				current.Use();
				break;
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			LegendLabel(s_entryColor, "Entrée " + (m_timing.offset * length).ToString("0.00") + " s");
			LegendLabel(s_impactColor, "Impact " + (m_impact * length).ToString("0.00") + " s");
			LegendLabel(s_exitColor, "Sortie " + (m_timing.exitTime * length).ToString("0.00") + " s");
			LegendLabel(Color.white, "Tête " + (m_playhead * length).ToString("0.00") + " s");
		}

		if (!showsHold && !showsRecovery)
			return;

		using (new EditorGUILayout.HorizontalScope())
		{
			if (showsHold)
				LegendLabel(s_holdColor, "Pose armée " + (m_hold * length).ToString("0.00") + " s");
			if (showsRecovery)
				LegendLabel(s_recoveryColor, (m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold ? "Pose tenue " : "Récupération ") + (m_recovery * length).ToString("0.00") + " s");
		}
	}

	private Marker PickMarker ( float _mouseX, System.Func<float, float> _x, bool _showsHold, bool _showsRecovery )
	{
		List<(Marker marker, float x)> markers = new() { (Marker.Impact, _x(m_impact)), (Marker.Entry, _x(m_timing.offset)), (Marker.Exit, _x(m_timing.exitTime)) };
		if (_showsHold)
			markers.Add((Marker.Hold, _x(m_hold)));
		if (_showsRecovery)
			markers.Add((Marker.Recovery, _x(m_recovery)));

		(Marker marker, float x) closest = markers.OrderBy(candidate => Mathf.Abs(candidate.x - _mouseX)).First();
		return Mathf.Abs(closest.x - _mouseX) <= c_markerPickDistance ? closest.marker : Marker.Playhead;
	}

	private void ApplyDrag ( AnimationLabRig _rig, AnimatorState _state, float _t )
	{
		bool showsHold = ShowsHold;
		bool showsRecovery = ShowsRecovery;
		switch (m_dragged)
		{
			case Marker.Entry:
				m_timing.offset = Mathf.Min(_t, (showsHold ? m_hold : m_impact) - c_markerGap);
				m_isTimingDirty = true;
				break;
			case Marker.Hold:
				m_hold = Mathf.Clamp(_t, m_timing.offset + c_markerGap, m_impact - c_markerGap);
				m_isPhaseDirty = true;
				break;
			case Marker.Impact:
				m_impact = Mathf.Clamp(_t, (showsHold ? m_hold : m_timing.offset) + c_markerGap, (showsRecovery ? m_recovery : m_timing.exitTime) - c_markerGap);
				m_isTimingDirty = true;
				break;
			case Marker.Recovery:
				m_recovery = Mathf.Clamp(_t, m_impact + c_markerGap, m_timing.exitTime - c_markerGap);
				m_isPhaseDirty = true;
				break;
			case Marker.Exit:
				m_timing.exitTime = Mathf.Max(_t, (showsRecovery ? m_recovery : m_impact) + c_markerGap);
				m_isTimingDirty = true;
				break;
		}

		m_playhead = m_dragged switch
		{
			Marker.Entry => m_timing.offset,
			Marker.Hold => m_hold,
			Marker.Impact => m_impact,
			Marker.Recovery => m_recovery,
			Marker.Exit => m_timing.exitTime,
			_ => _t
		};

		if (!m_isScrubbing)
		{
			m_isScrubbing = true;
			m_loop = false;
			AnimationLabSession.SetLoop(false);
		}

		_rig.Scrub(_state, m_playhead);
	}

	private void DrawActionPlan ( AnimationLabRig _rig, EntityActionData _action )
	{
		if (!IsMultiTick(_action) || _rig.Request == null)
			return;

		bool isPlaying = _rig.IsSequencePlaying;
		IReadOnlyList<AnimationLabRig.Segment> plan = isPlaying ? _rig.Plan : _rig.PlanFor(_rig.Request.Value);
		Rect rect = GUILayoutUtility.GetRect(10f, 18f, GUILayout.ExpandWidth(true));
		if (Event.current.type != EventType.Repaint || plan.Count == 0)
			return;

		float total = Mathf.Max(.01f, plan[plan.Count - 1].end);
		float X ( float _time ) => rect.x + c_timelinePadding + Mathf.Clamp01(_time / total) * (rect.width - 2f * c_timelinePadding);
		GUIStyle style = new(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
		style.normal.textColor = Color.black;
		EditorGUI.DrawRect(rect, new Color(.13f, .13f, .13f));
		foreach (AnimationLabRig.Segment segment in plan)
		{
			Rect bar = new(X(segment.start) + 1f, rect.y + 2f, Mathf.Max(1f, X(segment.end) - X(segment.start) - 2f), rect.height - 4f);
			EditorGUI.DrawRect(bar, SegmentColor(segment.kind));
			GUI.Label(bar, SegmentLabel(segment.kind) + " " + (segment.end - segment.start).ToString("0.00") + " s", style);
		}

		if (isPlaying)
			EditorGUI.DrawRect(new Rect(X(Time.time - _rig.PlanStart) - 1f, rect.y, 2f, rect.height), Color.white);
	}

	private static Color SegmentColor ( AnimationLabRig.SegmentKind _kind )
	{
		return _kind switch
		{
			AnimationLabRig.SegmentKind.Preparation => s_holdColor,
			AnimationLabRig.SegmentKind.Recovery => s_recoveryColor,
			_ => s_impactColor
		};
	}

	private static string SegmentLabel ( AnimationLabRig.SegmentKind _kind )
	{
		return _kind switch
		{
			AnimationLabRig.SegmentKind.Preparation => "Préparation",
			AnimationLabRig.SegmentKind.Recovery => "Récupération",
			_ => "Action"
		};
	}

	private void DrawTimingFields ( AnimationLabRig _rig, EntityActionData _action )
	{
		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Animation " + m_timing.trigger + " (toutes les unités)", EditorStyles.miniBoldLabel);
		EditorGUI.BeginChangeCheck();
		float speed = EditorGUILayout.Slider(new GUIContent("Vitesse du clip", "Vitesse des états R et L dans le contrôleur, commune à toutes les unités. Elle s'applique tout de suite à la lecture ; « Enregistrer le timing » l'écrit dans le contrôleur et recalcule les délais des actions."), m_timing.speed, .3f, 2.5f);
		float enter = EditorGUILayout.Slider(new GUIContent("Fondu d'entrée (s)", "Durée du mélange depuis la pose de repos. Pris en compte à l'enregistrement du timing."), m_timing.enterDuration, 0f, .5f);
		float exit = EditorGUILayout.Slider(new GUIContent("Fondu de sortie (s)", "Durée du retour à la pose de repos après la sortie. Pris en compte à l'enregistrement du timing."), m_timing.exitDuration, 0f, .8f);
		if (EditorGUI.EndChangeCheck())
		{
			m_timing.speed = speed;
			m_timing.enterDuration = enter;
			m_timing.exitDuration = exit;
			m_isTimingDirty = true;
			_rig.SetClipSpeedScale(m_timing.trigger, m_timing.speed / Mathf.Max(.01f, m_timing.assetSpeed));
			_rig.ApplyTuning(_action, m_timing.trigger);
		}

		float hitDelay = m_timing.HitDelayFor(m_impact);
		float unitSpeed = _rig.TuningFor(_action, m_timing.trigger).speed;
		float played = m_timing.speed * unitSpeed;
		float visible = (m_timing.exitTime - m_timing.offset) * m_timing.ClipLength / Mathf.Max(.01f, played);
		EditorGUILayout.LabelField(new GUIContent("Vitesse jouée", "Vitesse du clip × vitesse de l'unité pour cette action."), new GUIContent("×" + m_timing.speed.ToString("0.00") + " × " + unitSpeed.ToString("0.00") + " = ×" + played.ToString("0.00")));
		EditorGUILayout.LabelField("En jeu", "impact " + (hitDelay / unitSpeed).ToString("0.000") + " s après le déclenchement, frappe visible " + visible.ToString("0.00") + " s");

		List<EntityActionData> users = AnimationLabAssets.FindActionsUsing(m_timing.trigger, m_actions);
		string delays = string.Join(", ", users.Select(action => action.name + " " + action.performAnimationHitDelay.ToString("0.00")));
		EditorGUILayout.LabelField(new GUIContent("Actions concernées", "Toutes les actions dont performAnimationKey vaut " + m_timing.trigger + " reçoivent le même délai d'impact."), new GUIContent(delays, delays), EditorStyles.wordWrappedLabel);

		using (new EditorGUILayout.HorizontalScope())
		{
			using (new EditorGUI.DisabledScope(!m_isTimingDirty))
			{
				if (GUILayout.Button("Enregistrer le timing"))
				{
					AnimationLabAssets.WriteTiming(m_timing);
					AnimationLabAssets.WriteHitDelay(users, hitDelay);
					AnimationLabAssets.RescalePhaseDelays(users, m_timing);
					m_timing.assetSpeed = m_timing.speed;
					m_timing.assetOffset = m_timing.offset;
					m_isTimingDirty = false;
					m_status = "Timing de " + m_timing.trigger + " enregistré : contrôleur (états R et L) et délai d'impact " + hitDelay.ToString("0.000") + " s sur " + users.Count + " action(s).";
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Annuler"))
					ReloadTiming(_rig, _action);
			}
		}
	}

	private void DrawPhases ( AnimationLabRig _rig, AnimatorState _state, EntityActionData _action, bool _isLeftHand )
	{
		int preparation = _action.GetTokenPreparationCost(null, null, null);
		int cooldown = _action.GetTokenCooldownCost(null, null, null);
		if (preparation <= 0 && cooldown <= 0)
			return;

		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Phases de " + _action.name + " (cette action)", EditorStyles.miniBoldLabel);
		bool isRefreshNeeded = false;
		EditorGUI.BeginChangeCheck();
		if (preparation > 0)
		{
			EntityActionData.PreparationAnimationMode mode = (EntityActionData.PreparationAnimationMode)EditorGUILayout.Popup(new GUIContent("Préparation (" + Ticks(preparation) + ")", "Ce que fait l'unité pendant la préparation, avant le tick de frappe. « Armer la frappe » monte le geste jusqu'à la pose armée en ralentissant, la tient, puis la frappe repart de là."), (int)m_preparationMode, s_preparationLabels);
			if (mode != m_preparationMode)
			{
				m_preparationMode = mode;
				isRefreshNeeded = true;
				if (mode == EntityActionData.PreparationAnimationMode.WindUp && (_action.preparationAnimationMode != mode || m_hold <= m_timing.offset || m_hold >= m_impact))
					m_hold = DetectHold(_rig, _state, _isLeftHand);
			}

			if (m_preparationMode == EntityActionData.PreparationAnimationMode.WindUp)
			{
				m_windUpRatio = EditorGUILayout.Slider(new GUIContent("Part passée à armer", "Part de la préparation où le geste monte jusqu'à la pose armée en ralentissant. Le reste du temps, la pose est tenue."), m_windUpRatio, .1f, 1f);
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField(new GUIContent("Pose armée", "Repère orange de la frise : fais-le glisser, ou « Détecter » le moment où la main est la plus loin de sa position d'impact."), new GUIContent((m_hold * m_timing.ClipLength).ToString("0.00") + " s du clip"));
					if (GUILayout.Button("Détecter", GUILayout.Width(70)))
					{
						m_hold = DetectHold(_rig, _state, _isLeftHand);
						isRefreshNeeded = true;
					}
				}
			}
			else if (m_preparationMode == EntityActionData.PreparationAnimationMode.Trigger)
				m_preparationKey = TriggerPopup(_rig, "Déclencheur", m_preparationKey);
		}

		if (cooldown > 0)
		{
			EntityActionData.RecoveryAnimationMode mode = (EntityActionData.RecoveryAnimationMode)EditorGUILayout.Popup(new GUIContent("Récupération (" + Ticks(cooldown) + ")", "Ce que fait l'unité pendant le refroidissement, après la dernière frappe. « Tenir la fin du geste » ralentit le geste jusqu'à la pose tenue (repère violet) et la garde jusqu'à la fin de l'action."), (int)m_recoveryMode, s_recoveryLabels);
			if (mode != m_recoveryMode)
			{
				m_recoveryMode = mode;
				isRefreshNeeded = true;
				if (mode != EntityActionData.RecoveryAnimationMode.None && (m_recovery <= m_impact || m_recovery >= m_timing.exitTime))
					m_recovery = Mathf.Lerp(m_impact, m_timing.exitTime, .75f);
			}

			if (m_recoveryMode == EntityActionData.RecoveryAnimationMode.Trigger)
				m_recoveryKey = TriggerPopup(_rig, "Déclencheur", m_recoveryKey);
			if (m_recoveryMode != EntityActionData.RecoveryAnimationMode.None)
				EditorGUILayout.LabelField(new GUIContent(m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold ? "Pose tenue" : "Départ", "Repère violet de la frise, entre l'impact et la sortie."), new GUIContent((m_recovery * m_timing.ClipLength).ToString("0.00") + " s du clip"));
		}

		if (EditorGUI.EndChangeCheck())
			m_isPhaseDirty = true;

		EditorGUILayout.LabelField(DescribePhases(_rig, _action, preparation, cooldown), EditorStyles.wordWrappedMiniLabel);
		using (new EditorGUILayout.HorizontalScope())
		{
			using (new EditorGUI.DisabledScope(!m_isPhaseDirty))
			{
				if (GUILayout.Button("Enregistrer les phases"))
				{
					AnimationLabAssets.WritePhases(_action, m_preparationMode, m_timing.AssetDelayFor(m_hold), m_windUpRatio, m_preparationKey, m_recoveryMode, m_timing.AssetDelayFor(m_recovery), m_recoveryKey);
					m_isPhaseDirty = false;
					m_status = "Phases de " + _action.name + " enregistrées dans l'action.";
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Annuler"))
				{
					LoadPhases(_action);
					isRefreshNeeded = true;
				}
			}
		}

		if (!isRefreshNeeded)
			return;

		if (m_isScrubbing)
			_rig.Scrub(_state, m_playhead);
		GUIUtility.ExitGUI();
	}

	private string DescribePhases ( AnimationLabRig _rig, EntityActionData _action, int _preparation, int _cooldown )
	{
		float unitSpeed = Mathf.Max(.01f, _rig.TuningFor(_action, m_timing.trigger).speed);
		float hitDelay = m_timing.HitDelayFor(m_impact);
		List<string> parts = new();
		if (_preparation > 0)
		{
			float seconds = _preparation * TickDuration;
			parts.Add(m_preparationMode switch
			{
				EntityActionData.PreparationAnimationMode.WindUp => "préparation : arme en " + (seconds * m_windUpRatio).ToString("0.00") + " s puis tient la pose ; impact " + (Mathf.Max(0f, hitDelay - m_timing.HitDelayFor(m_hold)) / unitSpeed).ToString("0.00") + " s après le début du tick de frappe",
				EntityActionData.PreparationAnimationMode.Trigger => "préparation : joue " + m_preparationKey + " ; la frappe repart du début, impact " + (hitDelay / unitSpeed).ToString("0.00") + " s après le début du tick de frappe",
				_ => "préparation : l'unité attend " + seconds.ToString("0.0") + " s sans bouger"
			});
		}

		if (_cooldown > 0)
		{
			parts.Add(m_recoveryMode switch
			{
				EntityActionData.RecoveryAnimationMode.Hold => "récupération : le geste ralentit jusqu'à la pose tenue et la garde " + Ticks(_cooldown) + ", puis repart vers le repos",
				EntityActionData.RecoveryAnimationMode.Trigger => "récupération : joue " + m_recoveryKey + " au repère violet",
				_ => "récupération : le geste finit normalement, puis l'unité attend"
			});
		}

		return "En jeu, " + string.Join(" ; ", parts) + ".";
	}

	private float DetectHold ( AnimationLabRig _rig, AnimatorState _state, bool _isLeftHand )
	{
		float hold = _rig.FindWindUpPeak(_state, m_timing.offset + c_markerGap, m_impact - c_markerGap, _isLeftHand);
		m_isScrubbing = true;
		m_loop = false;
		AnimationLabSession.SetLoop(false);
		m_playhead = hold;
		_rig.Scrub(_state, hold);
		return hold;
	}

	private string TriggerPopup ( AnimationLabRig _rig, string _label, string _current )
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

	private void DrawUnitTuning ( AnimationLabRig _rig, AnimatorState _state, EntityActionData _action )
	{
		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Réglage de " + _action.name + " sur cette unité", EditorStyles.miniBoldLabel);
		using (new EditorGUI.DisabledScope(true))
			EditorGUILayout.ObjectField(new GUIContent("Profil d'animation", "Profil branché sur l'EntitySkinPlugin du prefab d'unité. Ses réglages ne concernent que cette unité."), _rig.Profile, typeof(EntityAnimationProfile), false);

		string trigger = m_timing.trigger;
		(float speed, float intensity) tuning = _rig.TuningFor(_action, trigger);
		EditorGUI.BeginChangeCheck();
		float speed = EditorGUILayout.Slider(new GUIContent("Vitesse ×", "Multiplie la vitesse du clip pour cette action, sur cette unité seulement. Le délai d'impact en jeu est divisé d'autant : l'impact reste synchronisé."), tuning.speed, EntityAnimationProfile.MinSpeed, EntityAnimationProfile.MaxSpeed);
		float intensity = EditorGUILayout.Slider(new GUIContent("Intensité", "Poids du calque d'attaque pour cette action, sur cette unité seulement. 1 joue le geste entier ; moins garde une part de la pose de repos et réduit l'amplitude, le pivot du buste et la vitesse de la main."), tuning.intensity, EntityAnimationProfile.MinIntensity, 1f);
		if (EditorGUI.EndChangeCheck())
		{
			_rig.SetTuning(_action, speed, intensity);
			if (m_isScrubbing)
				_rig.Scrub(_state, m_playhead);
		}

		bool isSaved = _rig.IsTuningSaved(_action, trigger);
		EntityAnimationProfile.TuningSource source = _rig.SourceFor(_action, trigger);
		string origin = source switch
		{
			EntityAnimationProfile.TuningSource.Action => "Réglage propre à " + _action.name + ".",
			EntityAnimationProfile.TuningSource.Trigger => "Hérité du réglage de " + trigger + ", partagé par ses actions sans réglage propre.",
			_ => "Valeurs par défaut de l'unité."
		};
		EditorGUILayout.LabelField(isSaved ? origin : origin + " Modifié, pas encore enregistré.", EditorStyles.wordWrappedMiniLabel);

		if (_rig.Profile == null)
		{
			EditorGUILayout.HelpBox("Cette unité n'a pas de profil d'animation : ses attaques jouent à vitesse ×1 et intensité 1.", MessageType.Info);
			if (GUILayout.Button("Créer le profil de l'unité"))
			{
				EntityAnimationProfile profile = AnimationLabAssets.CreateProfile(AnimationLabSession.UnitPrefab, out string path);
				_rig.SetProfile(profile);
				m_status = profile != null ? "Profil créé dans " + path + " et branché sur " + AnimationLabSession.UnitPrefab.name + "." : "Impossible de créer le profil : EntitySkinPlugin introuvable sur le prefab d'unité.";
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
					(float speed, float intensity) live = _rig.TuningFor(_action, trigger);
					AnimationLabAssets.SaveTuning(_rig.Profile, _action, live.speed, live.intensity);
					_rig.ForgetTuning(_action);
					m_status = _action.name + " enregistré dans " + AssetDatabase.GetAssetPath(_rig.Profile) + " : vitesse ×" + live.speed.ToString("0.00") + ", intensité " + live.intensity.ToString("0.00") + ".";
					GUIUtility.ExitGUI();
				}
			}

			using (new EditorGUI.DisabledScope(isSaved))
			{
				if (GUILayout.Button("Annuler"))
				{
					_rig.ForgetTuning(_action);
					if (m_isScrubbing)
						_rig.Scrub(_state, m_playhead);
				}
			}

			using (new EditorGUI.DisabledScope(source != EntityAnimationProfile.TuningSource.Action))
			{
				if (GUILayout.Button(new GUIContent("Retirer", "Supprime le réglage propre à cette action : elle reprend le réglage de son déclencheur ou les valeurs par défaut de l'unité.")))
				{
					AnimationLabAssets.RemoveTuning(_rig.Profile, _action);
					_rig.ForgetTuning(_action);
					m_status = "Réglage propre à " + _action.name + " retiré de " + AssetDatabase.GetAssetPath(_rig.Profile) + ".";
					GUIUtility.ExitGUI();
				}
			}
		}

		DrawUnitDefaults(_rig, _state, _action);
	}

	private void DrawUnitDefaults ( AnimationLabRig _rig, AnimatorState _state, EntityActionData _action )
	{
		m_showDefaults = EditorGUILayout.Foldout(m_showDefaults, new GUIContent("Valeurs par défaut de l'unité", "Utilisées par les actions qui n'ont ni réglage propre ni réglage de leur déclencheur."), true);
		if (!m_showDefaults)
			return;

		using (new EditorGUI.IndentLevelScope())
		{
			(float speed, float intensity) defaults = _rig.DefaultTuning;
			EditorGUI.BeginChangeCheck();
			float speed = EditorGUILayout.Slider("Vitesse ×", defaults.speed, EntityAnimationProfile.MinSpeed, EntityAnimationProfile.MaxSpeed);
			float intensity = EditorGUILayout.Slider("Intensité", defaults.intensity, EntityAnimationProfile.MinIntensity, 1f);
			if (EditorGUI.EndChangeCheck())
			{
				_rig.SetDefaults(speed, intensity);
				_rig.ApplyTuning(_action, m_timing.trigger);
				if (m_isScrubbing)
					_rig.Scrub(_state, m_playhead);
			}

			using (new EditorGUILayout.HorizontalScope())
			using (new EditorGUI.DisabledScope(_rig.AreDefaultsSaved))
			{
				if (GUILayout.Button("Enregistrer les valeurs par défaut"))
				{
					(float speed, float intensity) live = _rig.DefaultTuning;
					AnimationLabAssets.SaveDefaults(_rig.Profile, live.speed, live.intensity);
					_rig.ForgetDefaults();
					m_status = "Valeurs par défaut enregistrées dans " + AssetDatabase.GetAssetPath(_rig.Profile) + " : vitesse ×" + live.speed.ToString("0.00") + ", intensité " + live.intensity.ToString("0.00") + ".";
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Annuler"))
				{
					_rig.ForgetDefaults();
					_rig.ApplyTuning(_action, m_timing.trigger);
				}
			}
		}
	}

	private static void DrawMarker ( float _x, Rect _rect, Color _color )
	{
		EditorGUI.DrawRect(new Rect(_x - 1.5f, _rect.y + 2f, 3f, _rect.height - 14f), _color);
		EditorGUI.DrawRect(new Rect(_x - 4f, _rect.y + 2f, 8f, 5f), _color);
	}

	private static void LegendLabel ( Color _color, string _text )
	{
		Rect swatch = GUILayoutUtility.GetRect(10f, 14f, GUILayout.Width(10f));
		swatch.y += 3f;
		swatch.height = 8f;
		EditorGUI.DrawRect(swatch, _color);
		GUILayout.Label(_text, EditorStyles.miniLabel);
	}

	#endregion

	#region Grip

	private void DrawGrip ( AnimationLabRig _rig )
	{
		Header("Prise en main");
		m_gripTarget = GUILayout.Toolbar(m_gripTarget, new[] { "Arme de la main qui frappe", "Socket principal (toutes les armes)" });
		bool isSocket = m_gripTarget == 1;
		Transform target = isSocket ? _rig.IK.handGrabSocket : _rig.held[m_strikeHand] != null ? _rig.held[m_strikeHand].transform : null;
		if (target == null)
		{
			EditorGUILayout.HelpBox(isSocket ? "Ce skin n'a pas de socket principal." : "Aucune arme dans la main qui frappe.", MessageType.Info);
			return;
		}

		bool isMirrored = !isSocket && target.parent == _rig.IK.offHandGrabSocket && _rig.IK.offHandGrabSocket != _rig.IK.handGrabSocket;
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
				SaveGrip(_rig, target, isSocket, isMirrored);
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Recharger"))
				ReloadGrip(_rig, isSocket);

			if (GUILayout.Button("Zéro"))
			{
				target.localPosition = Vector3.zero;
				target.localRotation = Quaternion.identity;
			}
		}

		if (!isSocket)
			DrawGripTwins(_rig, target, isMirrored);
	}

	private void DrawGripTwins ( AnimationLabRig _rig, Transform _target, bool _isMirrored )
	{
		AnimationLabAssets.HeldOption option = _rig.heldOptions[m_strikeHand];
		if (option != m_twinsFor)
		{
			m_twinsFor = option;
			m_twins = AnimationLabAssets.FindSameVisual(option, m_options);
		}

		if (option == null || m_twins == null || m_twins.Count == 0)
			return;

		string names = string.Join(", ", m_twins.Select(twin => twin.prefab.name));
		if (!GUILayout.Button(new GUIContent("Enregistrer aussi dans les " + m_twins.Count + " armes au même visuel", "Même mesh à la même pose sous la racine : " + names + ".")))
			return;

		if (EditorUtility.DisplayDialog("Animation Lab", "La prise va être écrite dans " + option.prefab.name + " et dans :\n\n" + names, "Enregistrer", "Annuler"))
		{
			MainHandGrip(_target, _isMirrored, out Vector3 position, out Quaternion rotation);
			AnimationLabAssets.SaveGrip(option.prefab, position, rotation);
			foreach (AnimationLabAssets.HeldOption twin in m_twins)
				AnimationLabAssets.SaveGrip(twin.prefab, position, rotation);

			HashSet<GameObject> saved = new(m_twins.Select(twin => twin.prefab)) { option.prefab };
			for (int hand = 0; hand < 2; hand++)
			{
				if (_rig.heldOptions[hand] != null && saved.Contains(_rig.heldOptions[hand].prefab))
					_rig.Equip(hand, _rig.heldOptions[hand]);
			}

			m_status = "Prise enregistrée dans " + option.prefab.name + " et " + names + ".";
		}

		GUIUtility.ExitGUI();
	}

	private static void MainHandGrip ( Transform _target, bool _isMirrored, out Vector3 _position, out Quaternion _rotation )
	{
		_position = _target.localPosition;
		_rotation = _target.localRotation;
		if (!_isMirrored)
			return;

		_position = new Vector3(-_position.x, _position.y, _position.z);
		_rotation = new Quaternion(_rotation.x, -_rotation.y, -_rotation.z, _rotation.w);
	}

	private void SaveGrip ( AnimationLabRig _rig, Transform _target, bool _isSocket, bool _isMirrored )
	{
		Vector3 position = _target.localPosition;
		Quaternion rotation = _target.localRotation;
		if (_isSocket)
		{
			if (!AnimationLabAssets.SaveMainSocket(AnimationLabSession.UnitPrefab, position, rotation, out string path))
			{
				m_status = "Impossible d'enregistrer le socket : skin introuvable.";
				return;
			}

			m_status = "Socket principal enregistré dans " + path + ". L'unité est rechargée pour recalculer le socket secondaire.";
			AnimationLabSession.Rebuild();
			m_timing = null;
			return;
		}

		MainHandGrip(_target, _isMirrored, out position, out rotation);
		AnimationLabAssets.HeldOption option = _rig.heldOptions[m_strikeHand];
		AnimationLabAssets.SaveGrip(option.prefab, position, rotation);
		for (int hand = 0; hand < 2; hand++)
		{
			if (_rig.heldOptions[hand] != null && _rig.heldOptions[hand].prefab == option.prefab)
				_rig.Equip(hand, _rig.heldOptions[hand]);
		}

		m_status = "Prise enregistrée dans " + AssetDatabase.GetAssetPath(option.prefab) + ".";
	}

	private void ReloadGrip ( AnimationLabRig _rig, bool _isSocket )
	{
		if (_isSocket)
		{
			if (AnimationLabAssets.TryReadMainSocket(AnimationLabSession.UnitPrefab, out Vector3 position, out Quaternion rotation))
			{
				_rig.IK.handGrabSocket.localPosition = position;
				_rig.IK.handGrabSocket.localRotation = rotation;
			}

			return;
		}

		_rig.Equip(m_strikeHand, _rig.heldOptions[m_strikeHand]);
	}

	#endregion

	#region Walk

	private void DrawWalk ( AnimationLabRig _rig )
	{
		Header("Marche");
		float gameTileDuration = GameConfig.current != null && GameConfig.current.game != null ? GameConfig.current.game.actionDuration : 1f;
		float turnDuration = GameConfig.current != null && GameConfig.current.game != null ? GameConfig.current.game.entityRotationDuration : .5f;
		if (m_walkTileDuration <= 0f)
			m_walkTileDuration = gameTileDuration;

		EditorGUI.BeginChangeCheck();
		m_walkTiles = EditorGUILayout.IntSlider("Cases", m_walkTiles, 1, 6);
		m_walkTileDuration = EditorGUILayout.Slider(new GUIContent("Durée par case (s)", "Le jeu utilise " + gameTileDuration.ToString("0.##") + " s (GameConfig.actionDuration), à vitesse constante."), m_walkTileDuration, .3f, 3f);
		m_walkLoop = EditorGUILayout.Toggle("Aller-retour en boucle", m_walkLoop);
		if (EditorGUI.EndChangeCheck())
		{
			EditorPrefs.SetInt(c_walkTilesKey, m_walkTiles);
			EditorPrefs.SetFloat(c_walkDurationKey, m_walkTileDuration);
			EditorPrefs.SetBool(c_walkLoopKey, m_walkLoop);
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button(_rig.IsWalking ? "▶ Relancer la marche" : "▶ Marcher", GUILayout.Height(24)))
			{
				_rig.ResetPosition();
				_rig.Walk(m_walkTiles, m_walkTileDuration, turnDuration, m_walkLoop);
			}

			if (GUILayout.Button("■ Arrêter", GUILayout.Height(24)))
				_rig.StopWalk();
		}

		if (_rig.Legs == null)
		{
			EditorGUILayout.HelpBox("Ce skin n'a pas de LegsAnimator.", MessageType.Info);
			return;
		}

		if (GUILayout.Button(new GUIContent("Régler le LegsAnimator dans l'Inspector", "Sélectionne le LegsAnimator du Lab : règle-le pendant que l'unité marche, puis enregistre ici.")))
		{
			Selection.activeObject = _rig.Legs;
			EditorGUIUtility.PingObject(_rig.Legs);
		}

		if (!AnimationLabSession.IsLegsBaselineReady)
		{
			EditorGUILayout.HelpBox("Mesure des réglages de départ du LegsAnimator…", MessageType.None);
			return;
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Voir les réglages modifiés"))
				m_legChanges = AnimationLabAssets.ChangedPaths(AnimationLabSession.LegsBaseline, AnimationLabSession.LegsNoise, _rig.Legs);

			if (GUILayout.Button("Enregistrer les réglages des jambes"))
			{
				SaveLegs(_rig);
				GUIUtility.ExitGUI();
			}
		}

		if (m_legChanges != null)
		{
			string text = m_legChanges.Count == 0 ? "Aucun réglage modifié depuis le lancement."
				: string.Join("\n", m_legChanges.Take(12).Select(path => AnimationLabAssets.DescribeChange(AnimationLabSession.LegsBaseline, _rig.Legs, path)))
					+ (m_legChanges.Count > 12 ? "\n… et " + (m_legChanges.Count - 12) + " autre(s)" : "");
			EditorGUILayout.HelpBox(text, MessageType.None);
		}
	}

	private void SaveLegs ( AnimationLabRig _rig )
	{
		List<string> changes = AnimationLabAssets.ChangedPaths(AnimationLabSession.LegsBaseline, AnimationLabSession.LegsNoise, _rig.Legs);
		m_legChanges = changes;
		if (changes.Count == 0)
		{
			m_status = "Aucun réglage des jambes à enregistrer.";
			return;
		}

		GameObject unit = AnimationLabSession.UnitPrefab;
		string list = string.Join("\n", changes.Take(20).Select(path => AnimationLabAssets.DescribeChange(AnimationLabSession.LegsBaseline, _rig.Legs, path)));
		if (!EditorUtility.DisplayDialog("Animation Lab", changes.Count + " réglage(s) du LegsAnimator vont être écrits dans " + unit.name + " :\n\n" + list + (changes.Count > 20 ? "\n…" : ""), "Enregistrer", "Annuler"))
			return;

		int copied = AnimationLabAssets.SaveLegs(unit, _rig.Legs, changes);
		m_status = copied + " réglage(s) des jambes enregistré(s) dans " + AssetDatabase.GetAssetPath(unit) + ".";
		AnimationLabSession.Rebuild();
		m_timing = null;
		m_legChanges = null;
	}

	#endregion

	#region Help

	private void DrawHelp ()
	{
		EditorGUILayout.Space(8f);
		m_showHelp = EditorGUILayout.Foldout(m_showHelp, "Aide", true);
		if (!m_showHelp)
			return;

		EditorGUILayout.HelpBox(
			"Le Lab affiche l'unité seule, telle qu'en jeu : même Animator, même LegsAnimator, mêmes sockets. Rien du match ne tourne.\n\n"
			+ "Attaque : choisis la main, puis l'action de l'arme. « Jouer comme en jeu » joue l'action entière, tick par tick, comme le combat. « Image par image » fige la pose : fais glisser la tête de lecture blanche, ou les repères Entrée (bleu), Impact (rouge) et Sortie (vert). L'impact donne le délai avant les dégâts, les effets et le projectile. La vitesse du clip s'entend tout de suite ; « Enregistrer le timing » écrit la vitesse, l'entrée, la sortie et les fondus dans le contrôleur (états R et L, toutes les unités), et le délai d'impact dans toutes les actions de ce déclencheur.\n\n"
			+ "Phases (actions de plus d'un tick) : la frise du dessous montre la préparation, la frappe et la récupération à la durée du jeu. « Armer la frappe » monte le geste jusqu'à la pose armée (repère orange) en ralentissant, la tient, puis la frappe repart de là : l'impact arrive plus tôt dans le tick de frappe. « Tenir la fin du geste » fige la pose tenue (repère violet) pendant le refroidissement. « Déclencheur dédié » joue un état du contrôleur à la place. « Enregistrer les phases » n'écrit que dans l'action choisie.\n\n"
			+ "Réglage de l'action sur l'unité : la vitesse × et l'intensité ne valent que pour l'unité chargée et l'action choisie, dans le profil d'animation de l'unité. Sans réglage propre, l'action reprend le réglage de son déclencheur s'il en a un, sinon les valeurs par défaut de l'unité. La vitesse divise le délai d'impact en jeu, donc l'impact reste calé. L'intensité est le poids du calque d'attaque : elle réduit l'amplitude du geste.\n\n"
			+ "Garde : chaque arme a une garde, l'Idle joué au repos avec elle (Axe, Boxing, Torch, Throw : les états « Guard <nom> R/L » du contrôleur). Elle place les jambes comme au début de ses attaques, la tête vers l'avant. « Rotation du buste » remet le buste et les bras de face sans bouger la tête ni les jambes ; « De face » la calcule. L'unité revient au repos neutre pour marcher. Mains : les mains qui tiennent quelque chose se ferment, à la fermeture réglée pour l'unité.\n\n"
			+ "Prise : fige la pose à l'impact, puis tourne et place l'arme (champs, boutons +90 ou outils W/E de la Scene après « Sélectionner »). L'enregistrement écrit la pose de la racine du prefab de l'arme ; « Enregistrer aussi dans les armes au même visuel » l'écrit dans toutes les armes qui ont le même mesh à la même place. La main secondaire en reçoit le miroir, en jeu comme ici. Le socket principal déplace toutes les armes à la fois : à réserver au calage de la main elle-même.\n\n"
			+ "Marche : l'unité parcourt les cases au rythme du jeu. Règle le LegsAnimator dans l'Inspector pendant qu'elle marche, puis « Enregistrer les réglages des jambes » : seuls les réglages modifiés depuis le lancement sont écrits dans le prefab d'unité, après confirmation.\n\n"
			+ "Tout ce qui n'est pas enregistré disparaît en quittant le Play.", MessageType.None);
	}

	private static void Header ( string _title )
	{
		EditorGUILayout.Space(6f);
		EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
	}

	private static void Defer ( EditorApplication.CallbackFunction _action )
	{
		EditorApplication.delayCall += _action;
		GUIUtility.ExitGUI();
	}

	#endregion
}
