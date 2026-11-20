using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationLabAttackSection : AnimationLabSection
{
	private const string c_actionKey = "AnimationLab.Action";
	private const float c_markerPickDistance = 7f;
	private const float c_markerGap = .01f;

	private enum Marker { None, Entry, Hold, Impact, Recovery, Exit, Playhead }

	private static readonly string[] s_preparationLabels = { "Aucune", "Armer la frappe", "Déclencheur dédié" };
	private static readonly string[] s_recoveryLabels = { "Aucune", "Tenir la fin du geste", "Déclencheur dédié" };

	private string m_actionName = EditorPrefs.GetString(c_actionKey, "");
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
	private Marker m_dragged;

	public override string Key => "Attack";
	public override string Title => "Attaque";
	public override string Help => "Attaque : choisis l'action de l'arme de la main jouée. « Jouer comme en jeu » joue l'action entière, tick par tick, comme le combat. « Image par image » fige la pose : fais glisser la tête de lecture blanche, ou les repères Entrée (bleu), Impact (rouge) et Sortie (vert). L'impact donne le délai avant les dégâts, les effets et le projectile. La vitesse du clip s'entend tout de suite ; « Enregistrer le timing » écrit la vitesse, l'entrée, la sortie et les fondus dans le contrôleur (états R et L, toutes les unités), et le délai d'impact dans toutes les actions de ce déclencheur.\n\n"
		+ "Phases (actions de plus d'un tick) : la frise du dessous montre la préparation, la frappe et la récupération à la durée du jeu. « Armer la frappe » monte le geste jusqu'à la pose armée (repère orange) en ralentissant, la tient, puis la frappe repart de là. « Tenir la fin du geste » fige la pose tenue (repère violet) pendant le refroidissement. « Déclencheur dédié » joue un état du contrôleur à la place. « Enregistrer les phases » n'écrit que dans l'action choisie.\n\n"
		+ "Réglage sur l'unité : la vitesse × et l'intensité ne valent que pour l'unité chargée et l'action choisie, dans le profil d'animation de l'unité. Sans réglage propre, l'action reprend le réglage de son déclencheur, sinon les valeurs par défaut de l'unité.";

	private bool ShowsHold => m_phaseAction != null && m_phaseAction.GetTokenPreparationCost(null, null, null) > 0 && m_preparationMode == EntityActionData.PreparationAnimationMode.WindUp;
	private bool ShowsRecovery => m_phaseAction != null && m_phaseAction.GetTokenCooldownCost(null, null, null) > 0 && m_recoveryMode != EntityActionData.RecoveryAnimationMode.None;

	public override void Reset ()
	{
		m_timing = null;
		m_timingTrigger = null;
		m_isScrubbing = false;
		m_isTimingDirty = false;
		m_phaseAction = null;
		m_isPhaseDirty = false;
	}

	public override void Draw ( AnimationLabContext _context )
	{
		AnimationLabRig rig = _context.rig;
		AnimationLabAssets.HeldOption option = _context.HeldOption;
		if (option == null)
		{
			EditorGUILayout.HelpBox("Mets une arme dans la main jouée pour jouer ses attaques.", MessageType.Info);
			return;
		}

		List<EntityActionData> actions = AnimationLabAssets.GetAnimatedActions(option.data, _context.actions);
		if (actions.Count == 0)
		{
			EditorGUILayout.HelpBox(option.data.name + " n'a aucune action animée (performAnimationKey vide).", MessageType.Info);
			return;
		}

		int actionIndex = Mathf.Max(0, actions.FindIndex(action => action.name == m_actionName));
		EntityActionData selected = actions[EditorGUILayout.Popup("Action", actionIndex, actions.Select(action => action.name + "   " + action.performAnimationKey + AnimationLabGUI.TicksLabel(action)).ToArray())];
		if (selected.name != m_actionName)
		{
			m_actionName = selected.name;
			EditorPrefs.SetString(c_actionKey, m_actionName);
		}

		EnsureTiming(rig, selected, _context.IsLeftHand);
		if (m_timing == null)
		{
			EditorGUILayout.HelpBox("Aucun état du calque « " + AnimationLabAssets.AttackLayer + " » ne répond au déclencheur " + selected.performAnimationKey + ".", MessageType.Warning);
			return;
		}

		bool isLeftHand = _context.IsLeftHand;
		AnimatorState state = m_timing.StateFor(isLeftHand);
		rig.SetClipSpeedScale(m_timing.trigger, m_timing.speed / Mathf.Max(.01f, m_timing.assetSpeed));
		EditorGUILayout.LabelField("État joué", state.name + "   (clip " + (m_timing.clip != null ? m_timing.clip.name : "aucun") + ", " + m_timing.ClipLength.ToString("0.00") + " s)");
		if (m_timing.HasSeparateClips)
			EditorGUILayout.HelpBox("Les états R et L ont des clips différents : le timing affiché est celui de " + (m_timing.right != null ? m_timing.right.name : state.name) + ".", MessageType.Warning);

		if (AnimationLabGUI.PlayRow(_context, this, AnimationLabGUI.IsMultiTick(selected) ? "▶ Jouer l'action comme en jeu" : "▶ Jouer comme en jeu", "Joue l'action entière comme en combat, tick par tick.", () => PlanFor(_context, SelectedAction(_context))))
			m_isScrubbing = false;

		bool scrub = EditorGUILayout.ToggleLeft(new GUIContent("Image par image", "Fige l'animation : la tête de lecture et les repères se déplacent dans la frise."), m_isScrubbing);
		if (scrub != m_isScrubbing)
		{
			m_isScrubbing = scrub;
			if (m_isScrubbing)
			{
				AnimationLabSession.SetLoop(false);
				m_playhead = m_impact;
				rig.Scrub(state, m_playhead);
			}
			else
				rig.Release();
		}

		if (!m_isScrubbing && rig.TryGetPlayingTime(state, out float playing))
			m_playhead = Mathf.Clamp01(playing);

		DrawTimeline(rig, state);
		if (AnimationLabGUI.IsMultiTick(selected))
			AnimationLabGUI.PlanBar(_context, this, PlanFor(_context, selected));
		DrawTimingFields(_context, selected);
		DrawPhases(_context, state, selected, isLeftHand);
		AnimationLabGUI.UnitTuning(_context, selected, m_timing.trigger, "Réglage de " + selected.name + " sur cette unité", ref m_showDefaults, () => Rescrub(rig, state));
	}

	private void Rescrub ( AnimationLabRig _rig, AnimatorState _state )
	{
		if (m_isScrubbing && _state != null)
			_rig.Scrub(_state, m_playhead);
	}

	private EntityActionData SelectedAction ( AnimationLabContext _context )
	{
		return AnimationLabAssets.GetAnimatedActions(_context.HeldOption?.data, _context.actions).FirstOrDefault(action => action.name == m_actionName);
	}

	private AnimationLabRig.Plan PlanFor ( AnimationLabContext _context, EntityActionData _action )
	{
		if (_action == null || m_timing == null || m_timing.trigger != _action.performAnimationKey)
			return null;

		WeaponEquipmentData weapon = _context.HeldOption?.data as WeaponEquipmentData;
		return _context.rig.AttackPlan(new AnimationLabRig.AttackRequest
		{
			action = _action,
			key = _action.performAnimationKey,
			timing = CurrentTiming(_action),
			isLeftHand = _context.IsLeftHand,
			hand = _context.hand,
			preparationTicks = _action.GetTokenPreparationCost(null, null, null),
			performTicks = Mathf.Max(1, _action.tokenDuration),
			cooldownTicks = _action.GetTokenCooldownCost(null, null, null),
			tickDuration = AnimationLabContext.TickDuration,
			strikeTail = weapon != null ? weapon.singleAttackAnimationDuration : .3f
		});
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

	private void EnsureTiming ( AnimationLabRig _rig, EntityActionData _action, bool _isLeftHand )
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
			_rig.Scrub(m_timing.StateFor(_isLeftHand), m_playhead);
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

	private void DrawTimeline ( AnimationLabRig _rig, AnimatorState _state )
	{
		Rect rect = GUILayoutUtility.GetRect(10f, 38f, GUILayout.ExpandWidth(true));
		float length = m_timing.ClipLength;
		bool showsHold = ShowsHold;
		bool showsRecovery = ShowsRecovery;
		float X ( float _t ) => rect.x + AnimationLabGUI.Padding + Mathf.Clamp01(_t) * (rect.width - 2f * AnimationLabGUI.Padding);
		float T ( float _x ) => Mathf.Clamp01((_x - rect.x - AnimationLabGUI.Padding) / Mathf.Max(1f, rect.width - 2f * AnimationLabGUI.Padding));

		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(rect, AnimationLabGUI.Background);
			EditorGUI.DrawRect(new Rect(X(m_timing.offset), rect.y + 6f, Mathf.Max(1f, X(m_timing.exitTime) - X(m_timing.offset)), rect.height - 18f), new Color(.24f, .3f, .38f));
			if (showsHold)
				EditorGUI.DrawRect(new Rect(X(m_timing.offset), rect.yMax - 16f, Mathf.Max(1f, X(m_hold) - X(m_timing.offset)), 3f), AnimationLabGUI.HoldColor);
			if (showsRecovery)
				EditorGUI.DrawRect(new Rect(X(m_impact), rect.yMax - 16f, Mathf.Max(1f, X(m_recovery) - X(m_impact)), 3f), AnimationLabGUI.RecoveryColor);

			AnimationLabGUI.Marker(X(m_timing.offset), rect, AnimationLabGUI.EntryColor);
			if (showsHold)
				AnimationLabGUI.Marker(X(m_hold), rect, AnimationLabGUI.HoldColor);
			AnimationLabGUI.Marker(X(m_impact), rect, AnimationLabGUI.ImpactColor);
			if (showsRecovery)
				AnimationLabGUI.Marker(X(m_recovery), rect, AnimationLabGUI.RecoveryColor);
			AnimationLabGUI.Marker(X(m_timing.exitTime), rect, AnimationLabGUI.ExitColor);
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
			AnimationLabGUI.Legend(AnimationLabGUI.EntryColor, "Entrée " + (m_timing.offset * length).ToString("0.00") + " s");
			AnimationLabGUI.Legend(AnimationLabGUI.ImpactColor, "Impact " + (m_impact * length).ToString("0.00") + " s");
			AnimationLabGUI.Legend(AnimationLabGUI.ExitColor, "Sortie " + (m_timing.exitTime * length).ToString("0.00") + " s");
			AnimationLabGUI.Legend(Color.white, "Tête " + (m_playhead * length).ToString("0.00") + " s");
		}

		if (!showsHold && !showsRecovery)
			return;

		using (new EditorGUILayout.HorizontalScope())
		{
			if (showsHold)
				AnimationLabGUI.Legend(AnimationLabGUI.HoldColor, "Pose armée " + (m_hold * length).ToString("0.00") + " s");
			if (showsRecovery)
				AnimationLabGUI.Legend(AnimationLabGUI.RecoveryColor, (m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold ? "Pose tenue " : "Récupération ") + (m_recovery * length).ToString("0.00") + " s");
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
			AnimationLabSession.SetLoop(false);
		}

		_rig.Scrub(_state, m_playhead);
	}

	private void DrawTimingFields ( AnimationLabContext _context, EntityActionData _action )
	{
		AnimationLabRig rig = _context.rig;
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
			rig.SetClipSpeedScale(m_timing.trigger, m_timing.speed / Mathf.Max(.01f, m_timing.assetSpeed));
			rig.ApplyTuning(_action, m_timing.trigger);
		}

		float hitDelay = m_timing.HitDelayFor(m_impact);
		float unitSpeed = rig.TuningFor(_action, m_timing.trigger).speed;
		float played = m_timing.speed * unitSpeed;
		float visible = (m_timing.exitTime - m_timing.offset) * m_timing.ClipLength / Mathf.Max(.01f, played);
		EditorGUILayout.LabelField(new GUIContent("Vitesse jouée", "Vitesse du clip × vitesse de l'unité pour cette action."), new GUIContent("×" + m_timing.speed.ToString("0.00") + " × " + unitSpeed.ToString("0.00") + " = ×" + played.ToString("0.00")));
		EditorGUILayout.LabelField("En jeu", "impact " + (hitDelay / unitSpeed).ToString("0.000") + " s après le déclenchement, frappe visible " + visible.ToString("0.00") + " s");

		List<EntityActionData> users = AnimationLabAssets.FindActionsUsing(m_timing.trigger, _context.actions);
		string delays = string.Join(", ", users.Select(action => action.name + " " + action.performAnimationHitDelay.ToString("0.00")));
		EditorGUILayout.LabelField(new GUIContent("Actions concernées", "Toutes les actions dont performAnimationKey vaut " + m_timing.trigger + " reçoivent le même délai d'impact."), new GUIContent(delays, delays), EditorStyles.wordWrappedLabel);

		using (new EditorGUILayout.HorizontalScope())
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
				_context.status = "Timing de " + m_timing.trigger + " enregistré : contrôleur (états R et L) et délai d'impact " + hitDelay.ToString("0.000") + " s sur " + users.Count + " action(s).";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler"))
				ReloadTiming(rig, _action);
		}
	}

	private void DrawPhases ( AnimationLabContext _context, AnimatorState _state, EntityActionData _action, bool _isLeftHand )
	{
		AnimationLabRig rig = _context.rig;
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
			EntityActionData.PreparationAnimationMode mode = (EntityActionData.PreparationAnimationMode)EditorGUILayout.Popup(new GUIContent("Préparation (" + AnimationLabGUI.Ticks(preparation) + ")", "Ce que fait l'unité pendant la préparation, avant le tick de frappe. « Armer la frappe » monte le geste jusqu'à la pose armée en ralentissant, la tient, puis la frappe repart de là."), (int)m_preparationMode, s_preparationLabels);
			if (mode != m_preparationMode)
			{
				m_preparationMode = mode;
				isRefreshNeeded = true;
				if (mode == EntityActionData.PreparationAnimationMode.WindUp && (_action.preparationAnimationMode != mode || m_hold <= m_timing.offset || m_hold >= m_impact))
					m_hold = DetectHold(rig, _state, _isLeftHand);
			}

			if (m_preparationMode == EntityActionData.PreparationAnimationMode.WindUp)
			{
				m_windUpRatio = EditorGUILayout.Slider(new GUIContent("Part passée à armer", "Part de la préparation où le geste monte jusqu'à la pose armée en ralentissant. Le reste du temps, la pose est tenue."), m_windUpRatio, .1f, 1f);
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField(new GUIContent("Pose armée", "Repère orange de la frise : fais-le glisser, ou « Détecter » le moment où la main est la plus loin de sa position d'impact."), new GUIContent((m_hold * m_timing.ClipLength).ToString("0.00") + " s du clip"));
					if (GUILayout.Button("Détecter", GUILayout.Width(70)))
					{
						m_hold = DetectHold(rig, _state, _isLeftHand);
						isRefreshNeeded = true;
					}
				}
			}
			else if (m_preparationMode == EntityActionData.PreparationAnimationMode.Trigger)
				m_preparationKey = AnimationLabGUI.TriggerPopup(rig, "Déclencheur", m_preparationKey);
		}

		if (cooldown > 0)
		{
			EntityActionData.RecoveryAnimationMode mode = (EntityActionData.RecoveryAnimationMode)EditorGUILayout.Popup(new GUIContent("Récupération (" + AnimationLabGUI.Ticks(cooldown) + ")", "Ce que fait l'unité pendant le refroidissement, après la dernière frappe. « Tenir la fin du geste » ralentit le geste jusqu'à la pose tenue (repère violet) et la garde jusqu'à la fin de l'action."), (int)m_recoveryMode, s_recoveryLabels);
			if (mode != m_recoveryMode)
			{
				m_recoveryMode = mode;
				isRefreshNeeded = true;
				if (mode != EntityActionData.RecoveryAnimationMode.None && (m_recovery <= m_impact || m_recovery >= m_timing.exitTime))
					m_recovery = Mathf.Lerp(m_impact, m_timing.exitTime, .75f);
			}

			if (m_recoveryMode == EntityActionData.RecoveryAnimationMode.Trigger)
				m_recoveryKey = AnimationLabGUI.TriggerPopup(rig, "Déclencheur", m_recoveryKey);
			if (m_recoveryMode != EntityActionData.RecoveryAnimationMode.None)
				EditorGUILayout.LabelField(new GUIContent(m_recoveryMode == EntityActionData.RecoveryAnimationMode.Hold ? "Pose tenue" : "Départ", "Repère violet de la frise, entre l'impact et la sortie."), new GUIContent((m_recovery * m_timing.ClipLength).ToString("0.00") + " s du clip"));
		}

		if (EditorGUI.EndChangeCheck())
			m_isPhaseDirty = true;

		EditorGUILayout.LabelField(DescribePhases(rig, _action, preparation, cooldown), EditorStyles.wordWrappedMiniLabel);
		using (new EditorGUILayout.HorizontalScope())
		using (new EditorGUI.DisabledScope(!m_isPhaseDirty))
		{
			if (GUILayout.Button("Enregistrer les phases"))
			{
				AnimationLabAssets.WritePhases(_action, m_preparationMode, m_timing.AssetDelayFor(m_hold), m_windUpRatio, m_preparationKey, m_recoveryMode, m_timing.AssetDelayFor(m_recovery), m_recoveryKey);
				m_isPhaseDirty = false;
				_context.status = "Phases de " + _action.name + " enregistrées dans l'action.";
				GUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Annuler"))
			{
				LoadPhases(_action);
				isRefreshNeeded = true;
			}
		}

		if (!isRefreshNeeded)
			return;

		Rescrub(rig, _state);
		GUIUtility.ExitGUI();
	}

	private string DescribePhases ( AnimationLabRig _rig, EntityActionData _action, int _preparation, int _cooldown )
	{
		float unitSpeed = Mathf.Max(.01f, _rig.TuningFor(_action, m_timing.trigger).speed);
		float hitDelay = m_timing.HitDelayFor(m_impact);
		List<string> parts = new();
		if (_preparation > 0)
		{
			float seconds = _preparation * AnimationLabContext.TickDuration;
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
				EntityActionData.RecoveryAnimationMode.Hold => "récupération : le geste ralentit jusqu'à la pose tenue et la garde " + AnimationLabGUI.Ticks(_cooldown) + ", puis repart vers le repos",
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
		AnimationLabSession.SetLoop(false);
		m_playhead = hold;
		_rig.Scrub(_state, hold);
		return hold;
	}
}
