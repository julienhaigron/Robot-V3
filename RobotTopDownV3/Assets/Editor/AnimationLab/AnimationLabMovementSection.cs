using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationLabMovementSection : AnimationLabSection
{
	private const string c_tilesKey = "AnimationLab.WalkTiles";
	private const string c_durationKey = "AnimationLab.WalkDuration";
	private const string c_loopKey = "AnimationLab.WalkLoop";
	private const string c_styleKey = "AnimationLab.MoveStyle";
	private const string c_flyingKey = "AnimationLab.Flying";

	private int m_tiles = EditorPrefs.GetInt(c_tilesKey, 3);
	private float m_tileDuration = EditorPrefs.GetFloat(c_durationKey, -1f);
	private bool m_loop = EditorPrefs.GetBool(c_loopKey, true);
	private string m_style = EditorPrefs.GetString(c_styleKey, "");
	private bool m_isFlying = EditorPrefs.GetBool(c_flyingKey, false);
	private string m_assignTo;
	private List<string> m_legChanges;

	public override string Key => "Movement";
	public override string Title => "Déplacements";
	public override string Help => "Déplacements : chaque action de déplacement joue un style, l'état « Move <nom> » du calque Locomotion (Walk par défaut, Run, Dash…) ; « Donner ce style » l'écrit dans l'action. En vol (statut Flying), l'état « Move Fly » passe devant et les pieds quittent le sol. Pour un nouveau mouvement, ajoute un état « Move <nom> » au calque Locomotion : il apparaît ici. Règle le LegsAnimator dans l'Inspector pendant que l'unité se déplace, puis « Enregistrer les réglages des jambes » : seuls les réglages modifiés depuis le lancement sont écrits dans le prefab d'unité, après confirmation.";

	public override void Reset ()
	{
		m_legChanges = null;
	}

	public override void Draw ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		float gameTileDuration = AnimationLabContext.TickDuration;
		float turnDuration = GameConfig.current != null && GameConfig.current.game != null ? GameConfig.current.game.entityRotationDuration : .5f;
		if (m_tileDuration <= 0f)
			m_tileDuration = gameTileDuration;

		List<string> styles = AnimationLabAssets.MoveStyles(rig.Animator.runtimeAnimatorController).Union(new[] { LocomotionAnimationPlayer.RunStyle })
			.Where(style => style != LocomotionAnimationPlayer.WalkStyle && style != LocomotionAnimationPlayer.FlyStyle).ToList();
		if (m_style == LocomotionAnimationPlayer.WalkStyle)
			m_style = "";
		EditorGUI.BeginChangeCheck();
		m_style = AnimationLabGUI.KeyPopup(new GUIContent("Style", "État « Move <nom> » du calque Locomotion joué pendant le déplacement. Un style absent du contrôleur joue Walk."), m_style, styles, rig.Locomotion.HasStyle, LocomotionAnimationPlayer.WalkStyle + " (par défaut)");
		bool hasFly = rig.Locomotion.HasStyle(LocomotionAnimationPlayer.FlyStyle);
		m_isFlying = EditorGUILayout.ToggleLeft(new GUIContent(hasFly ? "En vol (statut Flying)" : "En vol (statut Flying) : « Move Fly » absent du contrôleur", "Comme en jeu après Soar : l'état « Move Fly » joue à l'arrêt comme en mouvement, et le LegsAnimator ne pose plus les pieds."), m_isFlying);
		m_tiles = EditorGUILayout.IntSlider("Cases", m_tiles, 1, 6);
		m_tileDuration = EditorGUILayout.Slider(new GUIContent("Durée par case (s)", "Le jeu utilise " + gameTileDuration.ToString("0.##") + " s par tick, divisé par la vitesse de l'action en cases par tick."), m_tileDuration, .15f, 3f);
		m_loop = EditorGUILayout.Toggle("Aller-retour en boucle", m_loop);
		if (EditorGUI.EndChangeCheck())
		{
			EditorPrefs.SetString(c_styleKey, m_style ?? "");
			EditorPrefs.SetBool(c_flyingKey, m_isFlying);
			EditorPrefs.SetInt(c_tilesKey, m_tiles);
			EditorPrefs.SetFloat(c_durationKey, m_tileDuration);
			EditorPrefs.SetBool(c_loopKey, m_loop);
		}

		rig.SetFlying(m_isFlying);

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button(rig.IsWalking ? "▶ Relancer" : "▶ Se déplacer", GUILayout.Height(24)))
			{
				rig.ResetPosition();
				rig.Move(m_style, m_isFlying, m_tiles, m_tileDuration, turnDuration, m_loop);
			}

			if (GUILayout.Button("■ Arrêter", GUILayout.Height(24)))
				rig.StopWalk();
		}

		DrawActions(_context);
		DrawLegs(_context);
	}

	private void DrawActions ( AnimationLabContext _context )
	{
		List<EntityActionData> movements = AnimationLabAssets.GetMovementActions(_context.actions);
		if (movements.Count == 0)
			return;

		string style = string.IsNullOrEmpty(m_style) ? LocomotionAnimationPlayer.WalkStyle : m_style;
		string users = string.Join(", ", movements.Where(action => StyleOf(action) == style).Select(action => action.name));
		EditorGUILayout.LabelField(new GUIContent("Actions avec ce style", "Actions de déplacement dont le champ movementAnimation vaut ce style (vide = Walk)."), new GUIContent(users.Length > 0 ? users : "aucune"), EditorStyles.wordWrappedLabel);

		using (new EditorGUILayout.HorizontalScope())
		{
			int index = Mathf.Max(0, movements.FindIndex(action => action.name == m_assignTo));
			EntityActionData target = movements[EditorGUILayout.Popup(index, movements.Select(action => action.name + " (" + StyleOf(action) + ")").ToArray())];
			m_assignTo = target.name;
			using (new EditorGUI.DisabledScope(StyleOf(target) == style))
			{
				if (GUILayout.Button(new GUIContent("Donner ce style", "Écrit le style dans le champ movementAnimation de l'action."), GUILayout.Width(110)))
				{
					AnimationLabAssets.SaveMovementAnimation(target, style);
					_context.status = target.name + " joue maintenant le style " + style + ".";
					GUIUtility.ExitGUI();
				}
			}
		}
	}

	private static string StyleOf ( EntityActionData _action )
	{
		return string.IsNullOrEmpty(_action.movementAnimation) ? LocomotionAnimationPlayer.WalkStyle : _action.movementAnimation;
	}

	private void DrawLegs ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Jambes (LegsAnimator de l'unité)", EditorStyles.miniBoldLabel);
		if (rig.Legs == null)
		{
			EditorGUILayout.HelpBox("Ce skin n'a pas de LegsAnimator.", MessageType.Info);
			return;
		}

		if (GUILayout.Button(new GUIContent("Régler le LegsAnimator dans l'Inspector", "Sélectionne le LegsAnimator du Lab : règle-le pendant que l'unité se déplace, puis enregistre ici.")))
		{
			Selection.activeObject = rig.Legs;
			EditorGUIUtility.PingObject(rig.Legs);
		}

		if (!AnimationLabSession.IsLegsBaselineReady)
		{
			EditorGUILayout.HelpBox("Mesure des réglages de départ du LegsAnimator…", MessageType.None);
			return;
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Voir les réglages modifiés"))
				m_legChanges = AnimationLabAssets.ChangedPaths(AnimationLabSession.LegsBaseline, AnimationLabSession.LegsNoise, rig.Legs);

			if (GUILayout.Button("Enregistrer les réglages des jambes"))
			{
				SaveLegs(_context);
				GUIUtility.ExitGUI();
			}
		}

		if (m_legChanges == null)
			return;

		string text = m_legChanges.Count == 0 ? "Aucun réglage modifié depuis le lancement."
			: string.Join("\n", m_legChanges.Take(12).Select(path => AnimationLabAssets.DescribeChange(AnimationLabSession.LegsBaseline, rig.Legs, path)))
				+ (m_legChanges.Count > 12 ? "\n… et " + (m_legChanges.Count - 12) + " autre(s)" : "");
		EditorGUILayout.HelpBox(text, MessageType.None);
	}

	private void SaveLegs ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		List<string> changes = AnimationLabAssets.ChangedPaths(AnimationLabSession.LegsBaseline, AnimationLabSession.LegsNoise, rig.Legs);
		m_legChanges = changes;
		if (changes.Count == 0)
		{
			_context.status = "Aucun réglage des jambes à enregistrer.";
			return;
		}

		GameObject unit = AnimationLabSession.UnitPrefab;
		string list = string.Join("\n", changes.Take(20).Select(path => AnimationLabAssets.DescribeChange(AnimationLabSession.LegsBaseline, rig.Legs, path)));
		if (!EditorUtility.DisplayDialog("Animation Lab", changes.Count + " réglage(s) du LegsAnimator vont être écrits dans " + unit.name + " :\n\n" + list + (changes.Count > 20 ? "\n…" : ""), "Enregistrer", "Annuler"))
			return;

		int copied = AnimationLabAssets.SaveLegs(unit, rig.Legs, changes);
		_context.status = copied + " réglage(s) des jambes enregistré(s) dans " + AssetDatabase.GetAssetPath(unit) + ".";
		m_legChanges = null;
		AnimationLabSession.Rebuild();
	}
}
