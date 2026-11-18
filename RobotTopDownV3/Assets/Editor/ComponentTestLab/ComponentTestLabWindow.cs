using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ComponentTestLabWindow : EditorWindow
{
	private const float c_labelWidth = 84f;
	private static readonly string[] s_coverLabels = { "Rien", "Couvert", "Mur" };

	private class ComponentOptions
	{
		public readonly List<EntityEquipmentData> items = new();
		public string[] labels = Array.Empty<string>();
	}

	private Vector2 m_scroll;
	private ComponentTestLabLayout m_previewLayout;
	private bool m_isPreviewUpToDate;
	private readonly Dictionary<string, bool> m_foldouts = new();
	private bool m_showSettings;
	private bool m_showHelp;
	private EntityStatusEnumID m_status = EntityStatusEnumID.Burn1;
	private int m_statusDuration = 3;
	private Dictionary<string, ComponentOptions> m_options;
	private int m_optionsSourceCount = -1;
	private GUIStyle m_richMiniLabel;

	[MenuItem("Tools/ComponentLab")]
	public static void Open ()
	{
		GetWindow<ComponentTestLabWindow>("ComponentLab");
	}

	private void OnEnable ()
	{
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private void OnDisable ()
	{
		EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
	}

	private void OnPlayModeStateChanged ( PlayModeStateChange _state )
	{
		m_previewLayout = null;
		Repaint();
	}

	private void OnFocus ()
	{
		if (!EditorApplication.isPlaying)
			m_previewLayout = null;
	}

	private void OnInspectorUpdate ()
	{
		if (EditorApplication.isPlaying)
			Repaint();
	}

	private static void Run ( Action _action )
	{
		try
		{
			_action();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void OnGUI ()
	{
		ComponentTestLabConfig config = ComponentTestLabConfig.Find();
		if (config == null)
		{
			EditorGUILayout.HelpBox("La configuration du labo n'existe pas encore.", MessageType.Info);
			if (GUILayout.Button("Créer la configuration"))
				ComponentTestLabConfig.LoadOrCreate();
			return;
		}

		m_richMiniLabel ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { richText = true };
		EnsureOptions();

		m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
		DrawHeader(config);
		if (ComponentTestLabSession.IsRunning)
			DrawTools();
		DrawBench(config);
		DrawStations(config);
		DrawSettings(config);
		DrawHelp();
		EditorGUILayout.EndScrollView();
	}

	#region Header & tools

	private void DrawHeader ( ComponentTestLabConfig _config )
	{
		EditorGUILayout.Space(4);
		if (!EditorApplication.isPlaying)
		{
			ComponentTestLabLayout layout = GetPreviewLayout(_config);
			bool isLabOpen = ComponentTestLabMap.IsLabSceneActive(_config);
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(isLabOpen))
				{
					if (GUILayout.Button(isLabOpen ? "Scène du labo ouverte" : "Ouvrir la scène du labo", GUILayout.Height(26)))
						ComponentTestLabLauncher.OpenLabScene();
				}

				if (GUILayout.Button("Lancer le labo", GUILayout.Height(26)))
					ComponentTestLabLauncher.Launch();
				if (GUILayout.Button("Actualiser", GUILayout.Height(26), GUILayout.Width(80)))
					m_previewLayout = null;
			}

			if (!layout.IsValid)
			{
				EditorGUILayout.HelpBox(string.Join("\n", layout.Errors), MessageType.Error);
				return;
			}

			string mapState = !isLabOpen ? "ouvre la scène du labo puis fais Play"
				: m_isPreviewUpToDate ? "carte à jour" : "la carte sera régénérée au prochain Play";
			EditorGUILayout.HelpBox(layout.Stations.Count + " bancs, " + layout.Stations.Sum(station => station.dummyTiles.Count) + " mannequins, carte "
				+ layout.Width + " x " + layout.Height + " : " + mapState + ".", MessageType.None);
			return;
		}

		if (ComponentTestLabSession.CurrentStep == ComponentTestLabSession.Step.Off)
		{
			EditorGUILayout.HelpBox("Le labo n'est pas lancé : fais Play depuis la scène du labo.", MessageType.Info);
			return;
		}

		string phase = GetPhaseLabel();
		MessageType type = ComponentTestLabSession.CurrentStep == ComponentTestLabSession.Step.Failed ? MessageType.Error : MessageType.Info;
		EditorGUILayout.HelpBox(ComponentTestLabSession.Message + (phase == null ? "" : "\n" + phase), type);
		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Relancer", GUILayout.Height(24)))
				ComponentTestLabLauncher.Relaunch();
			if (GUILayout.Button("Arrêter", GUILayout.Height(24)))
				EditorApplication.ExitPlaymode();
		}
	}

	private static string GetPhaseLabel ()
	{
		TurnManager turnManager = TurnManager.Instance;
		if (!ComponentTestLabSession.IsRunning || turnManager == null)
			return null;

		return turnManager.currentPhase switch
		{
			TurnManager.TurnPhase.Recording => "Phase de saisie : les outils sont actifs.",
			TurnManager.TurnPhase.Calculating or TurnManager.TurnPhase.Playing => "Résolution en cours : les outils reviennent à la prochaine phase de saisie.",
			_ => null,
		};
	}

	private void DrawTools ()
	{
		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField("Outils", EditorStyles.boldLabel);

		bool isRevealed = EditorGUILayout.ToggleLeft("Révéler toute la carte (vision optique partout)", ComponentTestLabSession.IsRevealed);
		if (isRevealed != ComponentTestLabSession.IsRevealed)
			Run(() => ComponentTestLabSession.SetRevealed(isRevealed));

		Entity selected = PlayerController.Instance != null ? PlayerController.Instance.SelectedEntity : null;
		ComponentTestLabSession.StationRuntime station = ComponentTestLabSession.GetStationOf(selected);
		Entity target = station != null && station.dummies.Count > 0 && station.dummies[0].IsAlive ? station.dummies[0].entity : null;
		EditorGUILayout.LabelField("Unité sélectionnée", selected != null ? selected.Data.name : "aucune (clique une unité en jeu)");

		using (new EditorGUI.DisabledScope(!ComponentTestLabSession.IsInputPhase))
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Statut", GUILayout.Width(c_labelWidth));
				m_status = (EntityStatusEnumID)EditorGUILayout.EnumPopup(m_status);
				m_statusDuration = Mathf.Max(1, EditorGUILayout.IntField(m_statusDuration, GUILayout.Width(36)));
				GUILayout.Label(GetDurationUnit(m_status), GUILayout.Width(44));
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(c_labelWidth + 4f);
				using (new EditorGUI.DisabledScope(selected == null || m_status == EntityStatusEnumID.Unknown))
				{
					if (GUILayout.Button("Sur l'unité", EditorStyles.miniButtonLeft))
						Run(() => ComponentTestLabSession.ApplyStatus(selected, m_status, m_statusDuration));
				}

				using (new EditorGUI.DisabledScope(target == null || m_status == EntityStatusEnumID.Unknown))
				{
					if (GUILayout.Button("Sur sa cible", EditorStyles.miniButtonMid))
						Run(() => ComponentTestLabSession.ApplyStatus(target, m_status, m_statusDuration));
				}

				using (new EditorGUI.DisabledScope(selected == null))
				{
					if (GUILayout.Button("Retirer ses statuts", EditorStyles.miniButtonRight))
						Run(() => ComponentTestLabSession.ClearStatuses(selected));
				}
			}
		}
	}

	private static string GetDurationUnit ( EntityStatusEnumID _status )
	{
		return GameAssets.current.game.entityStatus.TryGetValue(_status, out AEntityStatus status) && status != null && status.durationType == AEntityStatus.DurationType.Round
			? "rounds" : "ticks";
	}

	#endregion

	#region Stations

	private void DrawStations ( ComponentTestLabConfig _config )
	{
		ComponentTestLabLayout layout = GetCurrentLayout(_config);
		if (!layout.IsValid)
			return;

		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField("Bancs", EditorStyles.boldLabel);
		foreach (IGrouping<string, ComponentTestLabStation> group in layout.Stations.Where(station => !station.isBench).GroupBy(station => station.Group))
		{
			List<ComponentTestLabStation> testable = group.Where(station => station.testedActions.Count > 0).ToList();
			int done = testable.Count(station => IsStationCovered(_config, station));
			string title = group.Key + "   " + done + "/" + testable.Count + " bancs joués" + (testable.Count < group.Count() ? ", " + (group.Count() - testable.Count) + " passifs" : "");
			bool isOpen = !m_foldouts.TryGetValue(group.Key, out bool storedOpen) || storedOpen;
			isOpen = EditorGUILayout.Foldout(isOpen, title, true);
			m_foldouts[group.Key] = isOpen;
			if (!isOpen)
				continue;

			foreach (ComponentTestLabStation station in group)
				DrawStation(_config, layout, station);
		}

		if (layout.Skipped.Count > 0)
			EditorGUILayout.HelpBox("Sans banc :\n" + string.Join("\n", layout.Skipped), MessageType.Warning);
	}

	private void DrawStation ( ComponentTestLabConfig _config, ComponentTestLabLayout _layout, ComponentTestLabStation _station )
	{
		ComponentTestLabSession.StationRuntime runtime = EditorApplication.isPlaying
			? ComponentTestLabSession.Stations.FirstOrDefault(candidate => candidate.data == _station) : null;

		using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				string mark = _station.testedActions.Count == 0 ? "<color=#8a8a8a>passif</color>"
					: IsStationCovered(_config, _station) ? "<color=#4caf50><b>joué</b></color>" : "<color=#d0a040>à jouer</color>";
				GUILayout.Label(mark, m_richMiniLabel, GUILayout.Width(44));
				GUILayout.Label(new GUIContent(_station.label, _station.id), EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				int dummyCount = _station.dummyTiles.Count;
				GUILayout.Label("cible à " + _station.distance + (dummyCount > 1 ? ", " + dummyCount + " mannequins" : ""), EditorStyles.miniLabel);

				if (runtime != null)
				{
					using (new EditorGUI.DisabledScope(!ComponentTestLabSession.IsInputPhase || !runtime.unit.IsAlive))
					{
						if (GUILayout.Button("Select", EditorStyles.miniButtonLeft, GUILayout.Width(44)))
							Run(() => ComponentTestLabSession.Focus(runtime));
					}

					using (new EditorGUI.DisabledScope(!ComponentTestLabSession.IsInputPhase))
					{
						if (GUILayout.Button(new GUIContent("Réinit.", "Fait réapparaître l'unité et ses mannequins à neuf, et retire les objets posés par l'unité."), EditorStyles.miniButtonRight, GUILayout.Width(50)))
							Run(() => ComponentTestLabSession.ResetStation(runtime));
					}
				}
			}

			if (_station.testedActions.Count > 0)
			{
				string actions = string.Join("   ", _station.testedActions.Select(action =>
				{
					EntityActionData data = ComponentTestLabLayout.GetActionData(action);
					string label = data != null ? ComponentTestLabLayout.GetLabel(data) : action.ToString();
					return IsPerformed(_config, _station, action) ? "<color=#4caf50>" + label + "</color>" : label;
				}));
				GUILayout.Label(actions, m_richMiniLabel);
			}

			if (_station.hasDefaultWeapon && _layout.DefaultWeapon != null)
				GUILayout.Label("+ arme de base : " + ComponentTestLabLayout.GetLabel(_layout.DefaultWeapon), EditorStyles.miniLabel);

			foreach (string warning in _station.warnings)
				GUILayout.Label("<color=#d0a040>" + warning + "</color>", m_richMiniLabel);

			if (runtime == null)
				return;

			string state = GetStationState(runtime);
			if (state != null)
				GUILayout.Label(state, m_richMiniLabel);

			if (_station.coverTile >= 0)
			{
				using (new EditorGUILayout.HorizontalScope())
				using (new EditorGUI.DisabledScope(!ComponentTestLabSession.IsInputPhase))
				{
					GUILayout.Label("Devant la cible", EditorStyles.miniLabel, GUILayout.Width(c_labelWidth));
					for (int i = 0; i < s_coverLabels.Length; i++)
					{
						GUIStyle style = i == 0 ? EditorStyles.miniButtonLeft : i == s_coverLabels.Length - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
						string label = (int)runtime.cover == i ? "[" + s_coverLabels[i] + "]" : s_coverLabels[i];
						ComponentTestLabCoverState coverState = (ComponentTestLabCoverState)i;
						if (GUILayout.Button(label, style))
							Run(() => ComponentTestLabSession.SetCover(runtime, coverState));
					}
				}
			}
		}
	}

	private static string GetStationState ( ComponentTestLabSession.StationRuntime _station )
	{
		List<string> states = new();
		foreach (ComponentTestLabSession.Slot slot in _station.Slots)
		{
			if (slot.hasFailed)
				states.Add("<color=#e05050>" + slot.name + " : réapparition impossible (voir la console)</color>");
			else if (slot.IsWaitingRespawn)
				states.Add(slot.name + " : réapparition…");
		}

		return states.Count == 0 ? null : string.Join("\n", states);
	}

	private static bool IsPerformed ( ComponentTestLabConfig _config, ComponentTestLabStation _station, EntityActionEnumID _action )
	{
		return _config.performedActions.Contains(ComponentTestLabSession.GetCoverageKey(_station, _action));
	}

	private static bool IsStationCovered ( ComponentTestLabConfig _config, ComponentTestLabStation _station )
	{
		return _station.testedActions.Count > 0 && _station.testedActions.All(action => IsPerformed(_config, _station, action));
	}

	#endregion

	#region Bench

	private void DrawBench ( ComponentTestLabConfig _config )
	{
		ComponentTestLabLayout layout = GetCurrentLayout(_config);
		if (!layout.IsValid)
			return;

		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField("Banc libre", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Pour les composants sans action (blindages, occulteurs, cerveaux, membranes, chipsets, réacteurs) : en phase de saisie, changer un emplacement fait réapparaître l'unité ou la cible avec ce composant. Les flèches passent au composant suivant.", EditorStyles.wordWrappedMiniLabel);

		ComponentTestLabStation bench = layout.Stations.FirstOrDefault(station => station.isBench);
		if (bench != null)
			DrawStation(_config, layout, bench);

		Undo.RecordObject(_config, "Banc libre");
		EditorGUILayout.LabelField("Unité", EditorStyles.miniBoldLabel);
		if (DrawLoadout(_config.benchUnit, "base", layout.BaseFrame, layout.BaseReactor, null, layout.BaseMembrane, layout.DefaultWeapon != null ? "arme de base" : "aucune", "aucun"))
			OnBenchChanged(_config, true);

		EditorGUILayout.LabelField("Cible", EditorStyles.miniBoldLabel);
		UnitPreset dummy = layout.DummyPreset;
		if (DrawLoadout(_config.benchDummy, "mannequin", dummy != null ? dummy.frame : null, dummy != null ? dummy.reactor : null, dummy != null ? dummy.brain : null,
			dummy != null ? dummy.neuronalMembrane : null, "celles du mannequin", "ceux du mannequin"))
			OnBenchChanged(_config, false);

		if (ComponentTestLabSession.HasPendingBench)
			EditorGUILayout.HelpBox("Le changement sera appliqué à la prochaine phase de saisie.", MessageType.None);
	}

	private void OnBenchChanged ( ComponentTestLabConfig _config, bool _isUnit )
	{
		EditorUtility.SetDirty(_config);
		m_previewLayout = null;
		if (EditorApplication.isPlaying)
			Run(() => ComponentTestLabSession.ApplyBench(_isUnit));
	}

	private bool DrawLoadout ( ComponentTestLabLoadout _loadout, string _defaultName, EntityEquipmentData _frame, EntityEquipmentData _reactor, EntityEquipmentData _brain,
		EntityEquipmentData _membrane, string _emptyArms, string _emptyOthers )
	{
		bool isChanged = false;
		isChanged |= DrawSlot("Châssis", "frame", ref _loadout.frame, GetDefaultLabel(_defaultName, _frame));
		isChanged |= DrawSlot("Réacteur", "reactor", ref _loadout.reactor, GetDefaultLabel(_defaultName, _reactor));
		isChanged |= DrawSlot("Cerveau", "brain", ref _loadout.brain, _brain != null ? GetDefaultLabel(_defaultName, _brain) : "(le plus précis)");
		isChanged |= DrawSlot("Membrane", "membrane", ref _loadout.membrane, GetDefaultLabel(_defaultName, _membrane));
		isChanged |= DrawList("Bras", "arms", _loadout.arms, _emptyArms);
		isChanged |= DrawList("Auxiliaires", "auxiliary", _loadout.auxiliary, _emptyOthers);
		isChanged |= DrawList("Chipsets", "chipsets", _loadout.chipsets, _emptyOthers);
		return isChanged;
	}

	private static string GetDefaultLabel ( string _defaultName, EntityEquipmentData _component )
	{
		return "(" + _defaultName + (_component != null ? " : " + ComponentTestLabLayout.GetLabel(_component) : "") + ")";
	}

	private bool DrawSlot<T> ( string _label, string _optionKey, ref T _value, string _defaultLabel ) where T : EntityEquipmentData
	{
		ComponentOptions options = m_options[_optionKey];
		int count = options.items.Count;
		int index = _value == null ? 0 : options.items.IndexOf(_value) + 1;
		int newIndex = index;
		using (new EditorGUILayout.HorizontalScope())
		{
			GUILayout.Label(_label, GUILayout.Width(c_labelWidth));
			if (GUILayout.Button("<", EditorStyles.miniButtonLeft, GUILayout.Width(22)) && count > 0)
				newIndex = index <= 1 ? count : index - 1;

			string[] labels = new string[count + 1];
			labels[0] = _defaultLabel;
			Array.Copy(options.labels, 0, labels, 1, count);
			int popupIndex = EditorGUILayout.Popup(index, labels);
			if (popupIndex != index)
				newIndex = popupIndex;

			if (GUILayout.Button(">", EditorStyles.miniButtonRight, GUILayout.Width(22)) && count > 0)
				newIndex = index >= count ? 1 : index + 1;
		}

		if (newIndex == index)
			return false;

		_value = newIndex == 0 ? null : options.items[newIndex - 1] as T;
		return true;
	}

	private bool DrawList<T> ( string _label, string _optionKey, List<T> _list, string _emptyLabel ) where T : EntityEquipmentData
	{
		ComponentOptions options = m_options[_optionKey];
		int count = options.items.Count;
		bool isChanged = false;
		for (int i = 0; i < _list.Count; i++)
		{
			int index = Mathf.Max(0, options.items.IndexOf(_list[i]));
			int newIndex = index;
			bool isRemoved = false;
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label(i == 0 ? _label : "", GUILayout.Width(c_labelWidth));
				if (GUILayout.Button("<", EditorStyles.miniButtonLeft, GUILayout.Width(22)) && count > 0)
					newIndex = (index - 1 + count) % count;
				int popupIndex = EditorGUILayout.Popup(index, options.labels);
				if (popupIndex != index)
					newIndex = popupIndex;
				if (GUILayout.Button(">", EditorStyles.miniButtonMid, GUILayout.Width(22)) && count > 0)
					newIndex = (index + 1) % count;
				if (GUILayout.Button("x", EditorStyles.miniButtonRight, GUILayout.Width(22)))
					isRemoved = true;
			}

			if (isRemoved)
			{
				_list.RemoveAt(i);
				return true;
			}

			if (newIndex != index && count > 0)
			{
				_list[i] = options.items[newIndex] as T;
				isChanged = true;
			}
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			GUILayout.Label(_list.Count == 0 ? _label : "", GUILayout.Width(c_labelWidth));
			if (_list.Count == 0)
				GUILayout.Label("(" + _emptyLabel + ")", EditorStyles.miniLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("+ Ajouter", EditorStyles.miniButton, GUILayout.Width(70)) && count > 0)
			{
				_list.Add(options.items[0] as T);
				isChanged = true;
			}
		}

		return isChanged;
	}

	private void EnsureOptions ()
	{
		GameAssets assets = GameAssets.current;
		int sourceCount = assets != null && assets.equipments != null ? assets.equipments.Count : 0;
		if (m_options != null && sourceCount == m_optionsSourceCount)
			return;

		m_optionsSourceCount = sourceCount;
		List<EntityEquipmentData> components = sourceCount == 0 ? new List<EntityEquipmentData>()
			: assets.equipments.Values.Where(component => component != null).Distinct()
				.OrderBy(component => component.faction).ThenBy(component => component.name, StringComparer.Ordinal).ToList();

		m_options = new Dictionary<string, ComponentOptions>
		{
			["frame"] = BuildOptions(components, component => component is FrameEquipmentData),
			["reactor"] = BuildOptions(components, component => component is ReactorEquipmentData),
			["brain"] = BuildOptions(components, component => component is BrainEquipmentData),
			["membrane"] = BuildOptions(components, component => component is NeuronalMembraneEquipmentData),
			["arms"] = BuildOptions(components, component => component is WeaponEquipmentData || component is ToolEquipmentData),
			["auxiliary"] = BuildOptions(components, component => component is ArmorEquipmentData || component is OccultorEquipmentData),
			["chipsets"] = BuildOptions(components, component => component is ChipsetEquipmentData),
		};
	}

	private static ComponentOptions BuildOptions ( List<EntityEquipmentData> _components, Func<EntityEquipmentData, bool> _filter )
	{
		ComponentOptions options = new();
		options.items.AddRange(_components.Where(_filter));
		options.labels = options.items.Select(component => component.faction + "/" + ComponentTestLabLayout.GetLabel(component) + "  (" + component.name + ")").ToArray();
		return options;
	}

	#endregion

	#region Settings & help

	private void DrawSettings ( ComponentTestLabConfig _config )
	{
		EditorGUILayout.Space(6);
		m_showSettings = EditorGUILayout.Foldout(m_showSettings, "Réglages", true);
		if (!m_showSettings)
			return;

		ComponentTestLabLayout layout = GetCurrentLayout(_config);
		Undo.RecordObject(_config, "Réglages du labo");
		EditorGUI.BeginChangeCheck();
		using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
		{
			_config.scene = (SceneAsset)EditorGUILayout.ObjectField("Scène du labo", _config.scene, typeof(SceneAsset), false);
			_config.map = (GridData)EditorGUILayout.ObjectField("GridData du labo", _config.map, typeof(GridData), false);
			EditorGUILayout.LabelField("Unités des bancs (vide = automatique)", EditorStyles.miniBoldLabel);
			_config.baseFrame = DrawAutoField("Châssis", _config.baseFrame, layout.BaseFrame);
			_config.baseReactor = DrawAutoField("Réacteur", _config.baseReactor, layout.BaseReactor);
			_config.baseBrain = DrawAutoField("Cerveau", _config.baseBrain, null, "le plus précis selon le banc");
			_config.baseMembrane = DrawAutoField("Membrane", _config.baseMembrane, layout.BaseMembrane);
			_config.defaultWeapon = DrawAutoField("Arme de base", _config.defaultWeapon, layout.DefaultWeapon);
			_config.dummyPreset = DrawAutoField("Mannequin", _config.dummyPreset, layout.DummyPreset);
			_config.benchDistance = EditorGUILayout.IntSlider("Distance du banc libre", _config.benchDistance, 1, ComponentTestLabLayout.c_maxDistance);
		}

		_config.respawnDelay = Mathf.Max(0f, EditorGUILayout.FloatField("Réapparition (s)", _config.respawnDelay));
		_config.revealMapOnStart = EditorGUILayout.Toggle("Carte révélée au lancement", _config.revealMapOnStart);
		if (EditorGUI.EndChangeCheck())
		{
			EditorUtility.SetDirty(_config);
			m_previewLayout = null;
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Effacer la progression") && EditorUtility.DisplayDialog("Labo de composants", "Oublier toutes les actions déjà jouées ?", "Oui", "Non"))
			{
				Undo.RecordObject(_config, "Effacer la progression du labo");
				_config.performedActions.Clear();
				EditorUtility.SetDirty(_config);
			}

			using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || !ComponentTestLabMap.IsLabSceneActive(_config)))
			{
				if (GUILayout.Button("Mettre à jour la carte maintenant"))
				{
					ComponentTestLabLayout freshLayout = ComponentTestLabLayout.Build(_config);
					if (!ComponentTestLabMap.Sync(_config, freshLayout, out string error))
						EditorUtility.DisplayDialog("Labo de composants", error, "OK");
					m_previewLayout = null;
				}
			}
		}
	}

	private static T DrawAutoField<T> ( string _label, T _value, UnityEngine.Object _auto, string _autoLabel = null ) where T : UnityEngine.Object
	{
		T result = (T)EditorGUILayout.ObjectField(_label, _value, typeof(T), false);
		if (result == null)
		{
			string autoText = _autoLabel ?? (_auto != null ? _auto.name : "aucun");
			EditorGUILayout.LabelField(" ", "auto : " + autoText, EditorStyles.miniLabel);
		}

		return result;
	}

	private void DrawHelp ()
	{
		EditorGUILayout.Space(6);
		m_showHelp = EditorGUILayout.Foldout(m_showHelp, "Aide", true);
		if (!m_showHelp)
			return;

		EditorGUILayout.HelpBox(
			"Ouvre la scène du labo et fais Play : le jeu démarre, le match se lance seul et ne se termine jamais. La sauvegarde n'est pas touchée.\n\n"
			+ "Chaque banc porte une arme, un outil ou un châssis, face à un mannequin à bonne distance. « Select » sélectionne l'unité et centre la caméra. "
			+ "Donne tes ordres comme en match puis termine la phase.\n\n"
			+ "Un mannequin mort réapparaît au bout de quelques secondes, l'unité de test aussi. « Réinit. » remet le banc à neuf (actions une fois par partie, objets posés).\n\n"
			+ "Rien / Couvert / Mur posent un obstacle devant la cible. Le banc libre sert aux composants sans action.\n\n", MessageType.None);
	}

	#endregion

	#region Layout cache

	private ComponentTestLabLayout GetPreviewLayout ( ComponentTestLabConfig _config )
	{
		if (m_previewLayout == null)
		{
			m_previewLayout = ComponentTestLabLayout.Build(_config);
			m_isPreviewUpToDate = m_previewLayout.IsValid && ComponentTestLabMap.IsLabSceneActive(_config) && ComponentTestLabMap.IsUpToDate(_config, m_previewLayout);
		}

		return m_previewLayout;
	}

	private ComponentTestLabLayout GetCurrentLayout ( ComponentTestLabConfig _config )
	{
		return EditorApplication.isPlaying && ComponentTestLabSession.Layout != null ? ComponentTestLabSession.Layout : GetPreviewLayout(_config);
	}

	#endregion
}
