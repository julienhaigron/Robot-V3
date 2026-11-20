using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationLabShotSection : AnimationLabSection
{
	private const string c_actionKey = "AnimationLab.ShotAction";

	private static readonly string[] s_preparationLabels = { "Aucune", "Viser pendant la préparation", "Déclencheur dédié" };
	private static readonly string[] s_recoveryLabels = { "Aucune", "Garder la visée" };

	private string m_actionName = EditorPrefs.GetString(c_actionKey, "");
	private AnimationLabAssets.HeldOption m_weaponFor;
	private int m_weaponHand = -1;
	private string m_aimKey;
	private float m_aimDuration;
	private float m_bulletInterval;
	private float m_shootCooldown;
	private EntityActionData m_phaseAction;
	private EntityActionData.PreparationAnimationMode m_preparationMode;
	private string m_preparationKey;
	private EntityActionData.RecoveryAnimationMode m_recoveryMode;
	private bool m_showDefaults;

	public override string Key => "Shot";
	public override string Title => "Tir";
	public override string Help => "Tir : pour une arme à feu, choisis l'action et la pose de visée (états « Aim <nom> R/L » du calque d'attaque). « Tirer comme en jeu » joue l'action entière comme en combat : préparation, visée, balles au rythme de l'arme avec le recul, refroidissement. La frise montre chaque phase à la durée du jeu, un trait blanc par tir. Pour une action de plus d'un tick, « Viser pendant la préparation » lève l'arme vers la cible dès la préparation, « Garder la visée » la garde pendant le refroidissement ; entre deux ticks de tir, l'unité garde toujours la visée. Le timing de l'arme (visée, intervalle, pause) s'enregistre dans son prefab et vaut pour toutes les unités. Les spéciaux à projectile de l'arme (grenades, plasma) visent " + SpecialAction.WeaponAimDuration.ToString("0.0") + " s avant de partir.";

	public override void Reset ()
	{
		m_weaponFor = null;
		m_phaseAction = null;
	}

	public override void Draw ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		AnimationLabAssets.HeldOption option = _context.HeldOption;
		AnimationLabFeedback.WeaponVisual visual = rig.weaponVisuals[_context.hand];
		List<EntityActionData> actions = option?.data is WeaponEquipmentData held ? AnimationLabAssets.GetShotActions(held, _context.actions) : new List<EntityActionData>();
		if (option?.data is not WeaponEquipmentData weapon || visual == null || actions.Count == 0 || !visual.isBulletWeapon && string.IsNullOrEmpty(weapon.aimAnimation))
		{
			EditorGUILayout.HelpBox("Mets une arme à feu dans la main jouée pour régler ses tirs.", MessageType.Info);
			return;
		}

		LoadWeapon(_context, weapon, visual);
		int index = Mathf.Max(0, actions.FindIndex(action => action.name == m_actionName));
		EntityActionData selected = actions[EditorGUILayout.Popup("Action", index, actions.Select(action => action.name + AnimationLabGUI.TicksLabel(action) + ShotLabel(action, visual)).ToArray())];
		if (selected.name != m_actionName)
		{
			m_actionName = selected.name;
			EditorPrefs.SetString(c_actionKey, m_actionName);
		}

		if (m_phaseAction != selected)
			LoadPhases(selected);

		DrawAim(_context, weapon, selected);
		AnimationLabGUI.PlayRow(_context, this, "▶ Tirer comme en jeu", "Joue l'action entière comme en combat : préparation, visée, tirs, refroidissement.", () => PlanFor(_context, SelectedAction(_context)));
		AnimationLabGUI.PlanBar(_context, this, PlanFor(_context, selected));
		if (selected.IsShot)
			DrawPhases(_context, selected);
		if (visual.isBulletWeapon && selected.IsShot)
			DrawWeaponTiming(_context, weapon, visual);
		if (!string.IsNullOrEmpty(m_aimKey))
			AnimationLabGUI.UnitTuning(_context, selected, AttackAnimationPlayer.AimStatePrefix + m_aimKey, "Réglage de la visée de " + selected.name + " sur cette unité", ref m_showDefaults, null);
	}

	private void LoadWeapon ( AnimationLabContext _context, WeaponEquipmentData _weapon, AnimationLabFeedback.WeaponVisual _visual )
	{
		if (m_weaponFor == _context.HeldOption && m_weaponHand == _context.hand)
			return;

		m_weaponFor = _context.HeldOption;
		m_weaponHand = _context.hand;
		m_aimKey = _weapon.aimAnimation ?? "";
		m_aimDuration = _visual.aimDuration;
		m_bulletInterval = _visual.bulletInterval;
		m_shootCooldown = _visual.shootCooldown;
	}

	private void LoadPhases ( EntityActionData _action )
	{
		m_phaseAction = _action;
		m_preparationMode = _action.preparationAnimationMode;
		m_preparationKey = _action.preparationAnimationKey;
		m_recoveryMode = _action.recoveryAnimationMode == EntityActionData.RecoveryAnimationMode.Hold ? EntityActionData.RecoveryAnimationMode.Hold : EntityActionData.RecoveryAnimationMode.None;
	}

	private EntityActionData SelectedAction ( AnimationLabContext _context )
	{
		return _context.HeldOption?.data is WeaponEquipmentData weapon ? AnimationLabAssets.GetShotActions(weapon, _context.actions).FirstOrDefault(action => action.name == m_actionName) : null;
	}

	private AnimationLabRig.Plan PlanFor ( AnimationLabContext _context, EntityActionData _action )
	{
		if (_action == null || _context.rig.weaponVisuals[_context.hand] == null)
			return null;

		bool isEdited = m_phaseAction == _action;
		return _context.rig.ShotPlan(new AnimationLabRig.ShotRequest
		{
			action = _action,
			aimKey = m_aimKey,
			hand = _context.hand,
			preparationTicks = _action.GetTokenPreparationCost(null, null, null),
			performTicks = Mathf.Max(1, _action.tokenDuration),
			cooldownTicks = _action.GetTokenCooldownCost(null, null, null),
			tickDuration = AnimationLabContext.TickDuration,
			aimsDuringPreparation = isEdited ? m_preparationMode == EntityActionData.PreparationAnimationMode.WindUp : _action.AimsDuringPreparation,
			preparationKey = (isEdited ? m_preparationMode : _action.preparationAnimationMode) == EntityActionData.PreparationAnimationMode.Trigger ? isEdited ? m_preparationKey : _action.preparationAnimationKey : null,
			holdsAimDuringCooldown = isEdited ? m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold : _action.HoldsAimDuringCooldown,
			aimDuration = m_aimDuration,
			bulletInterval = m_bulletInterval,
			shootCooldown = m_shootCooldown,
			strikeTail = (_context.HeldOption?.data as WeaponEquipmentData)?.singleAttackAnimationDuration ?? .3f
		});
	}

	private void DrawAim ( AnimationLabContext _context, WeaponEquipmentData _weapon, EntityActionData _action )
	{
		AnimationLabRig rig = _context.rig;
		bool isLeftHand = _context.IsLeftHand;
		m_aimKey = AnimationLabGUI.KeyPopup(new GUIContent("Pose de visée", "État « Aim <nom> R » ou « Aim <nom> L » du calque d'attaque, joué pendant la visée et les tirs, en miroir dans l'autre main. L'IK oriente ensuite le bras vers la cible."),
			m_aimKey, AnimationLabAssets.AimKeys(rig.Animator.runtimeAnimatorController), key => rig.Player.HasAimState(key, isLeftHand), "Aucune : le bras se lève par IK");

		using (new EditorGUI.DisabledScope(m_aimKey == (_weapon.aimAnimation ?? "")))
		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Enregistrer la visée dans " + _weapon.name))
			{
				AnimationLabAssets.SaveAim(_weapon, m_aimKey);
				_context.status = "Visée de " + _weapon.name + " enregistrée : " + (string.IsNullOrEmpty(_weapon.aimAnimation) ? "IK seule" : _weapon.aimAnimation) + ".";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler", GUILayout.Width(70)))
				m_aimKey = _weapon.aimAnimation ?? "";
		}
	}

	private void DrawPhases ( AnimationLabContext _context, EntityActionData _action )
	{
		int preparation = _action.GetTokenPreparationCost(null, null, null);
		int cooldown = _action.GetTokenCooldownCost(null, null, null);
		if (preparation <= 0 && cooldown <= 0)
			return;

		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Phases de " + _action.name + " (cette action)", EditorStyles.miniBoldLabel);
		if (preparation > 0)
		{
			m_preparationMode = (EntityActionData.PreparationAnimationMode)EditorGUILayout.Popup(new GUIContent("Préparation (" + AnimationLabGUI.Ticks(preparation) + ")", "Ce que fait l'unité pendant la préparation. « Viser pendant la préparation » lève l'arme vers la cible dès le début de l'action et la tient jusqu'au tir."), (int)m_preparationMode, s_preparationLabels);
			if (m_preparationMode == EntityActionData.PreparationAnimationMode.Trigger)
				m_preparationKey = AnimationLabGUI.TriggerPopup(_context.rig, "Déclencheur", m_preparationKey);
		}

		if (cooldown > 0)
			m_recoveryMode = EditorGUILayout.Popup(new GUIContent("Récupération (" + AnimationLabGUI.Ticks(cooldown) + ")", "Ce que fait l'unité pendant le refroidissement. « Garder la visée » tient l'arme levée vers la cible jusqu'à la fin de l'action."), m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold ? 1 : 0, s_recoveryLabels) == 1
				? EntityActionData.RecoveryAnimationMode.Hold : EntityActionData.RecoveryAnimationMode.None;

		EditorGUILayout.LabelField(DescribePhases(_action, preparation, cooldown), EditorStyles.wordWrappedMiniLabel);
		bool isSaved = (preparation <= 0 || m_preparationMode == _action.preparationAnimationMode && (m_preparationMode != EntityActionData.PreparationAnimationMode.Trigger || m_preparationKey == _action.preparationAnimationKey))
			&& (cooldown <= 0 || m_recoveryMode == _action.recoveryAnimationMode);
		using (new EditorGUILayout.HorizontalScope())
		using (new EditorGUI.DisabledScope(isSaved))
		{
			if (GUILayout.Button("Enregistrer les phases"))
			{
				AnimationLabAssets.WriteShotPhases(_action, preparation > 0 ? m_preparationMode : _action.preparationAnimationMode, m_preparationKey, cooldown > 0 ? m_recoveryMode : _action.recoveryAnimationMode);
				_context.status = "Phases de " + _action.name + " enregistrées dans l'action.";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler"))
				LoadPhases(_action);
		}
	}

	private string DescribePhases ( EntityActionData _action, int _preparation, int _cooldown )
	{
		List<string> parts = new();
		if (_preparation > 0)
		{
			float seconds = _preparation * AnimationLabContext.TickDuration;
			parts.Add(m_preparationMode switch
			{
				EntityActionData.PreparationAnimationMode.WindUp => "préparation : l'unité vise sa cible pendant " + seconds.ToString("0.0") + " s",
				EntityActionData.PreparationAnimationMode.Trigger => "préparation : joue " + m_preparationKey,
				_ => "préparation : l'unité attend " + seconds.ToString("0.0") + " s en garde"
			});
		}

		parts.Add("tir : visée " + m_aimDuration.ToString("0.00") + " s puis " + Mathf.Max(1, _action.hitAmount) + " tir(s) toutes les " + m_bulletInterval.ToString("0.00") + " s" + (_action.tokenDuration > 1 ? ", sur " + AnimationLabGUI.Ticks(_action.tokenDuration) + " sans baisser l'arme" : ""));
		if (_cooldown > 0)
			parts.Add(m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold ? "récupération : garde la visée " + AnimationLabGUI.Ticks(_cooldown) : "récupération : baisse l'arme puis attend");
		return "En jeu, " + string.Join(" ; ", parts) + ".";
	}

	private void DrawWeaponTiming ( AnimationLabContext _context, WeaponEquipmentData _weapon, AnimationLabFeedback.WeaponVisual _visual )
	{
		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("Timing de " + _weapon.prefab.name + " (toutes les unités)", EditorStyles.miniBoldLabel);
		m_aimDuration = EditorGUILayout.Slider(new GUIContent("Temps de visée (s)", "Temps entre le début du tick de tir et la première balle, pendant lequel l'arme se lève."), m_aimDuration, .05f, 2f);
		m_bulletInterval = EditorGUILayout.Slider(new GUIContent("Intervalle entre balles (s)", "Temps entre deux balles d'une même rafale."), m_bulletInterval, 0f, 1f);
		m_shootCooldown = EditorGUILayout.Slider(new GUIContent("Pause après la rafale (s)", "Attente après la dernière balle, avant la fin du tick."), m_shootCooldown, 0f, 2f);
		bool isSaved = Mathf.Abs(m_aimDuration - _visual.aimDuration) < .005f && Mathf.Abs(m_bulletInterval - _visual.bulletInterval) < .005f && Mathf.Abs(m_shootCooldown - _visual.shootCooldown) < .005f;
		using (new EditorGUILayout.HorizontalScope())
		using (new EditorGUI.DisabledScope(isSaved))
		{
			if (GUILayout.Button("Enregistrer dans le prefab de l'arme"))
			{
				if (AnimationLabAssets.SaveWeaponTiming(_weapon.prefab.gameObject, m_aimDuration, m_bulletInterval, m_shootCooldown))
				{
					_visual.aimDuration = m_aimDuration;
					_visual.bulletInterval = m_bulletInterval;
					_visual.shootCooldown = m_shootCooldown;
					_context.status = "Timing enregistré dans " + AssetDatabase.GetAssetPath(_weapon.prefab) + ".";
				}
				else
					_context.status = "Impossible d'enregistrer le timing : pas de BulletWeapon dans " + _weapon.prefab.name + ".";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler"))
			{
				m_aimDuration = _visual.aimDuration;
				m_bulletInterval = _visual.bulletInterval;
				m_shootCooldown = _visual.shootCooldown;
			}
		}
	}

	private static string ShotLabel ( EntityActionData _action, AnimationLabFeedback.WeaponVisual _visual )
	{
		if (!_action.IsShot)
			return "   spécial à projectile";

		if (!_visual.isBulletWeapon || !AnimationLabFeedback.IsSingleTargetShot(_action))
			return "   zone";

		int bullets = Mathf.Max(1, _action.hitAmount);
		return "   " + bullets + (bullets > 1 ? " balles" : " balle");
	}
}
