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

	private enum Marker { None, Entry, Impact, Exit, Playhead }

	private static readonly Color s_entryColor = new(.35f, .7f, 1f);
	private static readonly Color s_impactColor = new(1f, .35f, .3f);
	private static readonly Color s_exitColor = new(.45f, .9f, .45f);

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
		int chosenAction = EditorGUILayout.Popup("Action", actionIndex, actions.Select(action => action.name + "   " + action.performAnimationKey).ToArray());
		EntityActionData selected = actions[chosenAction];
		if (selected.name != m_actionName)
		{
			m_actionName = selected.name;
			EditorPrefs.SetString(c_actionKey, m_actionName);
		}

		EnsureTiming(_rig, selected.performAnimationKey);
		if (m_timing == null)
		{
			EditorGUILayout.HelpBox("Aucun état du calque « " + AnimationLabAssets.AttackLayer + " » ne répond au déclencheur " + selected.performAnimationKey + ".", MessageType.Warning);
			return;
		}

		bool isLeftHand = _rig.IsHeldInLeftHand(m_strikeHand);
		AnimatorState state = m_timing.StateFor(isLeftHand);
		EditorGUILayout.LabelField("État joué", state.name + "   (clip " + (m_timing.clip != null ? m_timing.clip.name : "aucun") + ", " + m_timing.ClipLength.ToString("0.00") + " s)");
		if (m_timing.HasSeparateClips)
			EditorGUILayout.HelpBox("Les états R et L ont des clips différents : le timing affiché est celui de " + (m_timing.right != null ? m_timing.right.name : state.name) + ".", MessageType.Warning);

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("▶ Jouer comme en jeu", GUILayout.Height(24)))
			{
				m_isScrubbing = false;
				_rig.Release();
				_rig.Trigger(m_timing.trigger, isLeftHand);
				if (m_loop)
					AnimationLabSession.SetLoop(m_timing.trigger, isLeftHand);
			}

			bool loop = GUILayout.Toggle(m_loop, "Boucle", "Button", GUILayout.Width(70), GUILayout.Height(24));
			if (loop != m_loop)
			{
				m_loop = loop;
				AnimationLabSession.SetLoop(m_loop ? m_timing.trigger : null, isLeftHand);
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
				AnimationLabSession.SetLoop(null, false);
				m_playhead = m_impact;
				_rig.Scrub(state, m_playhead);
			}
			else
				_rig.Release();
		}

		if (!m_isScrubbing && _rig.TryGetPlayingTime(state, out float playing))
			m_playhead = Mathf.Clamp01(playing);

		DrawTimeline(_rig, state);
		DrawTimingFields(_rig, state);
	}

	private void EnsureTiming ( AnimationLabRig _rig, string _trigger )
	{
		if (m_timing != null && m_timingTrigger == _trigger)
			return;

		m_timing = AnimationLabAssets.ReadTiming(_rig.Animator.runtimeAnimatorController, _trigger);
		m_timingTrigger = _trigger;
		m_isTimingDirty = false;
		if (m_timing == null)
			return;

		List<EntityActionData> users = AnimationLabAssets.FindActionsUsing(_trigger, m_actions);
		float hitDelay = users.Count > 0 ? users[0].performAnimationHitDelay : 0f;
		m_impact = m_timing.ImpactFor(hitDelay);
		m_playhead = m_impact;
		if (m_isScrubbing)
			_rig.Scrub(m_timing.StateFor(_rig.IsHeldInLeftHand(m_strikeHand)), m_playhead);
	}

	private void StopPlayback ( AnimationLabRig _rig )
	{
		m_loop = false;
		AnimationLabSession.SetLoop(null, false);
		m_isScrubbing = false;
		m_timing = null;
		_rig.Release();
	}

	private void DrawTimeline ( AnimationLabRig _rig, AnimatorState _state )
	{
		Rect rect = GUILayoutUtility.GetRect(10f, 38f, GUILayout.ExpandWidth(true));
		float length = m_timing.ClipLength;
		float X ( float _t ) => rect.x + c_timelinePadding + Mathf.Clamp01(_t) * (rect.width - 2f * c_timelinePadding);
		float T ( float _x ) => Mathf.Clamp01((_x - rect.x - c_timelinePadding) / Mathf.Max(1f, rect.width - 2f * c_timelinePadding));

		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(rect, new Color(.13f, .13f, .13f));
			EditorGUI.DrawRect(new Rect(X(m_timing.offset), rect.y + 6f, Mathf.Max(1f, X(m_timing.exitTime) - X(m_timing.offset)), rect.height - 18f), new Color(.24f, .3f, .38f));
			DrawMarker(X(m_timing.offset), rect, s_entryColor);
			DrawMarker(X(m_impact), rect, s_impactColor);
			DrawMarker(X(m_timing.exitTime), rect, s_exitColor);
			EditorGUI.DrawRect(new Rect(X(m_playhead) - 1f, rect.y, 2f, rect.height - 12f), Color.white);
			GUI.Label(new Rect(rect.x + 2f, rect.yMax - 13f, 60f, 13f), "0 s", EditorStyles.miniLabel);
			GUI.Label(new Rect(rect.xMax - 60f, rect.yMax - 13f, 58f, 13f), length.ToString("0.00") + " s", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperRight });
		}

		Event current = Event.current;
		switch (current.type)
		{
			case EventType.MouseDown when current.button == 0 && rect.Contains(current.mousePosition):
				m_dragged = PickMarker(current.mousePosition.x, X);
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
	}

	private Marker PickMarker ( float _mouseX, System.Func<float, float> _x )
	{
		(Marker marker, float x)[] markers = { (Marker.Impact, _x(m_impact)), (Marker.Entry, _x(m_timing.offset)), (Marker.Exit, _x(m_timing.exitTime)) };
		(Marker marker, float x) closest = markers.OrderBy(candidate => Mathf.Abs(candidate.x - _mouseX)).First();
		return Mathf.Abs(closest.x - _mouseX) <= c_markerPickDistance ? closest.marker : Marker.Playhead;
	}

	private void ApplyDrag ( AnimationLabRig _rig, AnimatorState _state, float _t )
	{
		switch (m_dragged)
		{
			case Marker.Entry:
				m_timing.offset = Mathf.Min(_t, m_impact - .01f);
				m_isTimingDirty = true;
				break;
			case Marker.Impact:
				m_impact = Mathf.Clamp(_t, m_timing.offset + .01f, m_timing.exitTime - .01f);
				m_isTimingDirty = true;
				break;
			case Marker.Exit:
				m_timing.exitTime = Mathf.Max(_t, m_impact + .01f);
				m_isTimingDirty = true;
				break;
		}

		m_playhead = m_dragged switch
		{
			Marker.Entry => m_timing.offset,
			Marker.Impact => m_impact,
			Marker.Exit => m_timing.exitTime,
			_ => _t
		};

		if (!m_isScrubbing)
		{
			m_isScrubbing = true;
			m_loop = false;
			AnimationLabSession.SetLoop(null, false);
		}

		_rig.Scrub(_state, m_playhead);
	}

	private void DrawTimingFields ( AnimationLabRig _rig, AnimatorState _state )
	{
		EditorGUI.BeginChangeCheck();
		float speed = EditorGUILayout.Slider(new GUIContent("Vitesse du clip", "Multiplicateur de vitesse des états R et L."), m_timing.speed, .3f, 2.5f);
		float enter = EditorGUILayout.Slider(new GUIContent("Fondu d'entrée (s)", "Durée du mélange depuis la pose de repos."), m_timing.enterDuration, 0f, .5f);
		float exit = EditorGUILayout.Slider(new GUIContent("Fondu de sortie (s)", "Durée du retour à la pose de repos après la sortie."), m_timing.exitDuration, 0f, .8f);
		if (EditorGUI.EndChangeCheck())
		{
			m_timing.speed = speed;
			m_timing.enterDuration = enter;
			m_timing.exitDuration = exit;
			m_isTimingDirty = true;
		}

		float hitDelay = m_timing.HitDelayFor(m_impact);
		float visible = (m_timing.exitTime - m_timing.offset) * m_timing.ClipLength / Mathf.Max(.01f, m_timing.speed);
		EditorGUILayout.LabelField("En jeu", "impact " + hitDelay.ToString("0.000") + " s après le déclenchement, frappe visible " + visible.ToString("0.00") + " s");

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
					m_isTimingDirty = false;
					m_status = "Timing de " + m_timing.trigger + " enregistré : contrôleur (états R et L) et délai d'impact " + hitDelay.ToString("0.000") + " s sur " + users.Count + " action(s).";
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Annuler"))
				{
					m_timing = null;
					EnsureTiming(_rig, m_timingTrigger ?? "");
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

		if (_isMirrored)
		{
			position = new Vector3(-position.x, position.y, position.z);
			rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
		}

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
			+ "Attaque : choisis la main, puis l'action de l'arme. « Jouer comme en jeu » envoie le même déclencheur que le combat. « Image par image » fige la pose : fais glisser la tête de lecture blanche, ou les repères Entrée (bleu), Impact (rouge) et Sortie (vert). L'impact donne le délai avant les dégâts, les effets et le projectile. « Enregistrer le timing » écrit la vitesse et les fondus dans le contrôleur (états R et L), et le délai d'impact dans toutes les actions qui utilisent ce déclencheur.\n\n"
			+ "Prise : fige la pose à l'impact, puis tourne et place l'arme (champs, boutons +90 ou outils W/E de la Scene après « Sélectionner »). L'enregistrement écrit la pose de la racine du prefab de l'arme. La main secondaire en reçoit le miroir, en jeu comme ici. Le socket principal déplace toutes les armes à la fois : à réserver au calage de la main elle-même.\n\n"
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
