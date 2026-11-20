using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationLabWindow : EditorWindow
{
	private const string c_handKey = "AnimationLab.StrikeOffHand";
	private const string c_effectsKey = "AnimationLab.Effects";
	private const string c_sectionKey = "AnimationLab.Section.";

	private readonly AnimationLabContext m_context = new();
	private AnimationLabSection[] m_sections;
	private Vector2 m_scroll;
	private bool m_effects = true;
	private float m_timeScale = 1f;
	private bool m_showHelp;
	private double m_lastRepaint;
	private AnimationLabRig m_lastRig;

	[MenuItem("Tools/Animation Lab")]
	public static void Open ()
	{
		GetWindow<AnimationLabWindow>("Animation Lab");
	}

	private void OnEnable ()
	{
		m_sections = new AnimationLabSection[] { new AnimationLabHandsSection(), new AnimationLabAttackSection(), new AnimationLabShotSection(), new AnimationLabGripSection(), new AnimationLabMovementSection() };
		m_context.hand = EditorPrefs.GetInt(c_handKey, 0);
		m_effects = EditorPrefs.GetBool(c_effectsKey, true);
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private void OnDisable ()
	{
		EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
	}

	private void OnPlayModeStateChanged ( PlayModeStateChange _state )
	{
		ResetSections();
		m_timeScale = 1f;
		m_context.status = null;
		Repaint();
	}

	private void ResetSections ()
	{
		m_context.playbackOwner = null;
		foreach (AnimationLabSection section in m_sections)
			section.Reset();
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
			if (rig != m_lastRig)
			{
				m_lastRig = rig;
				ResetSections();
			}

			m_context.rig = rig;
			DrawPlayback(rig);
			foreach (AnimationLabSection section in m_sections)
				DrawSection(section);
		}

		DrawHelp();
		EditorGUILayout.EndScrollView();
	}

	private void EnsureOptions ()
	{
		if (m_context.options != null)
			return;

		m_context.options = AnimationLabAssets.LoadHeldOptions();
		m_context.optionLabels = new[] { "Rien" }.Concat(m_context.options.Select(option => option.label)).ToArray();
		m_context.actions = AnimationLabAssets.LoadActions();
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
				Defer(AnimationLabSession.Rebuild);

			using (new EditorGUI.DisabledScope(rig == null || !rig.IsAlive))
			{
				if (GUILayout.Button("Sélectionner l'unité"))
					Selection.activeGameObject = rig.Root;
				if (GUILayout.Button("Recentrer"))
					rig.ResetPosition();
			}
		}

		if (!string.IsNullOrEmpty(m_context.status))
			EditorGUILayout.HelpBox(m_context.status, MessageType.None);

		if (rig == null || !rig.IsAlive)
		{
			EditorGUILayout.HelpBox(AnimationLabSession.Message ?? "L'unité n'est pas encore créée.", MessageType.Warning);
			return null;
		}

		return rig;
	}

	private void DrawPlayback ( AnimationLabRig _rig )
	{
		EditorGUILayout.Space(4f);
		int hand = GUILayout.Toolbar(m_context.hand, new[] { "Main jouée : principale", "Main jouée : secondaire" });
		if (hand != m_context.hand)
		{
			m_context.hand = hand;
			EditorPrefs.SetInt(c_handKey, hand);
			m_context.StopPlayback();
			ResetSections();
		}

		bool effects = EditorGUILayout.ToggleLeft(new GUIContent("Effets visuels et sonores", "Joue les VFX et les sons des actions au moment du jeu : slash, muzzle, projectiles, impacts, sorts, objets posés. Rien ne sert de cible : les coups partent vers un point devant l'unité."), m_effects);
		if (effects != m_effects)
		{
			m_effects = effects;
			EditorPrefs.SetBool(c_effectsKey, effects);
			if (!effects)
				_rig.Feedback.Clear();
		}

		_rig.Feedback.IsEnabled = m_effects;
		float timeScale = EditorGUILayout.Slider(new GUIContent("Vitesse du temps", "Ralenti global du Lab (Time.timeScale). N'est pas enregistré."), m_timeScale, .05f, 1f);
		if (!Mathf.Approximately(timeScale, m_timeScale))
		{
			m_timeScale = timeScale;
			Time.timeScale = m_timeScale;
		}
	}

	private void DrawSection ( AnimationLabSection _section )
	{
		string key = c_sectionKey + _section.Key;
		bool isOpen = EditorPrefs.GetBool(key, true);
		bool open = EditorGUILayout.BeginFoldoutHeaderGroup(isOpen, _section.Title);
		if (open != isOpen)
			EditorPrefs.SetBool(key, open);
		EditorGUILayout.EndFoldoutHeaderGroup();
		if (!open)
			return;

		_section.Draw(m_context);
		EditorGUILayout.Space(6f);
	}

	private void DrawHelp ()
	{
		EditorGUILayout.Space(8f);
		m_showHelp = EditorGUILayout.Foldout(m_showHelp, "Aide", true);
		if (!m_showHelp)
			return;

		string sections = string.Join("\n\n", m_sections.Select(section => section.Help));
		EditorGUILayout.HelpBox("Le Lab affiche l'unité seule, telle qu'en jeu : même Animator, même LegsAnimator, mêmes sockets. Rien du match ne tourne. La main jouée choisit l'arme de l'attaque, du tir et de la prise. « Effets visuels et sonores » joue les VFX et les sons au moment du jeu, vers un point devant l'unité.\n\n"
			+ sections + "\n\nTout ce qui n'est pas enregistré disparaît en quittant le Play.", MessageType.None);
	}

	private static void Defer ( EditorApplication.CallbackFunction _action )
	{
		EditorApplication.delayCall += _action;
		GUIUtility.ExitGUI();
	}
}
