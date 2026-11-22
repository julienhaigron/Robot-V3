using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ComponentTestLabSession
{
	public enum Step { Off, WaitingBoot, Launching, Running, Failed }

	public class Slot
	{
		public StationRuntime station;
		public bool isUnit;
		public int playerID;
		public int tileID;
		public string name;
		public UnitPreset preset;
		public Entity entity;
		public double respawnTime = -1;
		public bool isRespawnImmediate;
		public bool isReselectNeeded;
		public bool hasFailed;

		public bool IsAlive => entity != null && !entity.Equipment.IsDead;
		public bool IsWaitingRespawn => respawnTime >= 0;
	}

	public class StationRuntime
	{
		public ComponentTestLabStation data;
		public Slot unit;
		public readonly List<Slot> dummies = new();
		public ComponentTestLabCoverState cover;

		public IEnumerable<Slot> Slots
		{
			get
			{
				yield return unit;
				foreach (Slot dummy in dummies)
					yield return dummy;
			}
		}
	}

	private const double c_bootTimeout = 60d;
	private const double c_bootSettleDelay = .3d;
	private const double c_launchTimeout = 90d;
	private const double c_retryDelay = .5d;
	private const int c_playerID = 0;
	private const int c_enemyID = 1;

	private static readonly List<StationRuntime> m_stations = new();
	private static readonly Dictionary<int, Slot> m_slotsByTile = new();
	private static double m_stepStartTime;
	private static double m_bootReadyTime = -1d;
	private static bool m_hasLaunched;
	private static int m_announcedEntityCount;
	private static int m_attachedOnSpawnCount;
	private static bool m_isBenchUnitPending;
	private static bool m_isBenchDummyPending;

	public static Step CurrentStep { get; private set; }
	public static string Message { get; private set; }
	public static ComponentTestLabConfig Config { get; private set; }
	public static ComponentTestLabLayout Layout { get; private set; }
	public static IReadOnlyList<StationRuntime> Stations => m_stations;
	public static bool IsRevealed { get; private set; }
	public static bool IsRunning => CurrentStep == Step.Running && EditorApplication.isPlaying;
	public static bool IsInputPhase => IsRunning && TurnManager.Instance != null && TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Recording;
	public static bool HasPendingBench => m_isBenchUnitPending || m_isBenchDummyPending;

	#region Lifecycle

	public static void Begin ()
	{
		End();
		Config = ComponentTestLabConfig.LoadOrCreate();
		Layout = ComponentTestLabLayout.Build(Config);
		if (!Layout.IsValid)
		{
			Fail(string.Join("\n", Layout.Errors));
			return;
		}

		SetStep(Step.WaitingBoot, "Démarrage du jeu…");
		EditorApplication.update += Update;
	}

	public static void End ()
	{
		EditorApplication.update -= Update;
		EntityAnchor.onEntityAdded -= OnEntityAdded;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnAnyEntityDeath;
		TurnManager.onStartInputPhase -= OnStartInputPhase;

		if (Config != null)
			AssetDatabase.SaveAssetIfDirty(Config);

		TurnManager.isMatchEndDisabled = false;
		m_stations.Clear();
		m_slotsByTile.Clear();
		m_bootReadyTime = -1d;
		m_hasLaunched = false;
		m_isBenchUnitPending = false;
		m_isBenchDummyPending = false;
		IsRevealed = false;
		CurrentStep = Step.Off;
		Message = null;
	}

	private static void Update ()
	{
		if (!EditorApplication.isPlaying)
			return;

		try
		{
			switch (CurrentStep)
			{
				case Step.WaitingBoot:
					if (!IsGameBooted())
					{
						m_bootReadyTime = -1d;
						if (Elapsed > c_bootTimeout)
							Fail("Le jeu n'atteint pas le menu de départ.");
					}
					else if (m_bootReadyTime < 0d)
						m_bootReadyTime = EditorApplication.timeSinceStartup;
					else if (EditorApplication.timeSinceStartup - m_bootReadyTime >= c_bootSettleDelay)
						Launch();
					break;
				case Step.Launching:
					ClickBriefingStart();
					if (Elapsed > c_launchTimeout)
						Fail("Le match du labo ne démarre pas : le chargement ou le briefing est bloqué (voir la console).");
					break;
				case Step.Running:
					ProcessRespawns();
					break;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Fail(exception.Message);
		}
	}

	private static double Elapsed => EditorApplication.timeSinceStartup - m_stepStartTime;

	private static void SetStep ( Step _step, string _message )
	{
		CurrentStep = _step;
		Message = _message;
		m_stepStartTime = EditorApplication.timeSinceStartup;
	}

	private static void Fail ( string _message )
	{
		SetStep(Step.Failed, _message);
		Debug.LogWarning("Labo de composants : " + _message);
	}

	private static bool IsGameBooted ()
	{
		if (m_hasLaunched || GameManager.Instance == null || UIManager.Instance == null || LoadingManager.Instance == null || TurnManager.Instance == null)
			return false;

		CanvasGroup loadingScreen = UIManager.Instance.LoadingScreenCanvasGroup;
		return !LoadingManager.Instance.IsLoading && UIManager.Instance.currentPanel is StartMenuPanel
			&& (loadingScreen == null || loadingScreen.alpha <= 0f);
	}

	#endregion

	#region Launch

	private static void Launch ()
	{
		m_hasLaunched = true;
		m_announcedEntityCount = 0;
		m_attachedOnSpawnCount = 0;
		GameDatas datas = GameDatas.current;
		if (datas == null || datas == Resources.Load<GameDatas>("GameDatas"))
		{
			Fail("La sauvegarde n'est pas sur une copie en mémoire : le labo s'arrête pour ne pas la modifier.");
			return;
		}

		GridData map = Config.Map;
		if (map == null || map.width != Layout.Width || map.height != Layout.Height)
		{
			Fail("La carte du labo ne correspond pas aux bancs : relance le Play depuis la scène du labo.");
			return;
		}

		BuildStations();
		TurnManager.isMatchEndDisabled = true;

		GameDatas.PlayerSave save = datas.currentPlayerSave;
		save.allBuiltUnits.Clear();
		save.squadUnitsIndex.Clear();
		foreach (Slot slot in GetSlots().Where(slot => slot.isUnit).OrderBy(slot => slot.tileID))
		{
			EntitySavedData unit = slot.preset.GetSavedData();
			unit.index = save.allBuiltUnits.Count;
			save.allBuiltUnits.Add(unit);
			save.squadUnitsIndex.Add(unit.index);
		}

		MissionData mission = ScriptableObject.CreateInstance<MissionData>();
		mission.name = "ComponentTestLab";
		mission.missionName = "Labo de composants";
		mission.type = MissionData.MissionType.Extermination;
		mission.map = map;
		mission.enemies = GetSlots().Where(slot => !slot.isUnit).OrderBy(slot => slot.tileID).Select(slot => slot.preset).ToArray();
		mission.allies = Array.Empty<UnitPreset>();
		mission.currencyRewards = Array.Empty<MissionData.CurrencyReward>();
		mission.equipmentRewards = new List<EntityEquipmentData>();
		mission.unitReward = new List<UnitPreset>();

		EntityAnchor.onEntityAdded += OnEntityAdded;
		EntityEquipmentPlugin.onAnyEntityDeath += OnAnyEntityDeath;
		TurnManager.onStartInputPhase += OnStartInputPhase;

		MatchBriefing briefing = UIManager.Instance.MatchBriefing;
		if (briefing != null && !briefing.gameObject.activeSelf)
		{
			briefing.gameObject.SetActive(true);
		}

		SetStep(Step.Launching, "Lancement du match…");
		GameManager.Instance.SetupLevel(mission);
	}

	private static void BuildStations ()
	{
		m_stations.Clear();
		m_slotsByTile.Clear();
		foreach (ComponentTestLabStation data in Layout.Stations)
		{
			StationRuntime station = new() { data = data };
			string unitName = GetUnitName(data);
			station.unit = CreateSlot(station, true, c_playerID, data.unitTile, unitName, data.unit);
			for (int i = 0; i < data.dummyTiles.Count; i++)
			{
				string dummyName = (data.isBench ? "Cible du banc" : "Cible " + unitName) + (i > 0 ? " " + (i + 1) : "");
				station.dummies.Add(CreateSlot(station, false, c_enemyID, data.dummyTiles[i], dummyName, data.dummy));
			}

			m_stations.Add(station);
		}
	}

	private static Slot CreateSlot ( StationRuntime _station, bool _isUnit, int _playerID, int _tileID, string _name, ComponentTestLabUnitSpec _spec )
	{
		Slot slot = new()
		{
			station = _station,
			isUnit = _isUnit,
			playerID = _playerID,
			tileID = _tileID,
			name = _name,
			preset = _spec.CreatePreset(_name),
		};
		m_slotsByTile[_tileID] = slot;
		return slot;
	}

	private static string GetUnitName ( ComponentTestLabStation _station )
	{
		if (_station.isBench)
			return _station.label;

		try
		{
			if (LocalizationManager.Instance != null)
			{
				string localizedName = _station.component.GetLocalizedName();
				if (!string.IsNullOrWhiteSpace(localizedName))
					return localizedName;
			}
		}
		catch (Exception)
		{
		}

		return _station.label;
	}

	private static void ClickBriefingStart ()
	{
		MatchBriefing briefing = UIManager.Instance != null ? UIManager.Instance.MatchBriefing : null;
		if (briefing == null)
			return;

		foreach (BaseButton button in briefing.GetComponentsInChildren<BaseButton>(false))
			button.onClick?.Invoke();
	}

	private static void OnMatchStarted ()
	{
		int attachedByScan = AttachUnmappedEntities();
		List<Slot> missing = GetSlots().Where(slot => !slot.IsAlive).ToList();
		SetStep(Step.Running, missing.Count == 0 ? "Labo prêt." : "Labo prêt : " + missing.Count + " unités manquantes recréées.");

		if (attachedByScan > 0 || missing.Count > 0)
		{
			Debug.LogWarning("Labo de composants : " + m_announcedEntityCount + " entités apparues, " + m_attachedOnSpawnCount + " rattachées à leur apparition, "
				+ attachedByScan + " rattachées en relisant la carte, " + missing.Count + " absentes et recréées. " + DescribeAnchors()
				+ (missing.Count > 0 ? " Absentes : " + string.Join(", ", missing.Take(8).Select(slot => slot.name + " (case " + slot.tileID + ")")) : ""));

			double now = EditorApplication.timeSinceStartup;
			foreach (Slot slot in missing)
				slot.respawnTime = now;
		}

		if (Config.revealMapOnStart)
			SetRevealed(true);
	}

	private static string DescribeAnchors ()
	{
		List<string> parts = new();
		EntityAnchor[] anchors = GameManager.Instance.PlayersEntityAnchor;
		for (int i = 0; i < anchors.Length; i++)
		{
			List<Entity> entities = anchors[i].Entities.Where(entity => entity != null).ToList();
			List<Entity> alive = entities.Where(entity => !entity.Equipment.IsDead).ToList();
			parts.Add("Camp " + i + " : " + entities.Count + " entités dont " + alive.Count + " vivantes (cases "
				+ string.Join(",", alive.Take(6).Select(entity => entity.Displacement.Coordinates.ID)) + (alive.Count > 6 ? ",…" : "") + ").");
		}

		return string.Join(" ", parts);
	}

	#endregion

	#region Entities

	private static IEnumerable<Slot> GetSlots ()
	{
		return m_stations.SelectMany(station => station.Slots);
	}

	private static void OnEntityAdded ( Entity _entity )
	{
		m_announcedEntityCount++;
		if (_entity == null || !m_slotsByTile.TryGetValue(_entity.Displacement.Coordinates.ID, out Slot slot) || slot.playerID != _entity.OwnerID)
			return;

		if (slot.entity == _entity || slot.IsAlive)
			return;

		Attach(slot, _entity);
		m_attachedOnSpawnCount++;
	}

	private static int AttachUnmappedEntities ()
	{
		HashSet<Entity> attached = new(GetSlots().Where(slot => slot.IsAlive).Select(slot => slot.entity));
		int count = 0;
		foreach (Slot slot in GetSlots())
		{
			if (slot.IsAlive)
				continue;

			foreach (Entity entity in GameManager.Instance.PlayersEntityAnchor[slot.playerID].Entities)
			{
				if (entity == null || entity.Equipment.IsDead || attached.Contains(entity) || entity.Displacement.Coordinates.ID != slot.tileID)
					continue;

				Attach(slot, entity);
				attached.Add(entity);
				count++;
				break;
			}
		}

		return count;
	}

	private static void Attach ( Slot _slot, Entity _entity )
	{
		_slot.entity = _entity;
		if (_slot.isUnit)
			_entity.onStartPerformAction += _action => OnActionPerformed(_slot, _action);
	}

	private static void OnAnyEntityDeath ( Entity _entity )
	{
		Slot slot = GetSlots().FirstOrDefault(candidate => candidate.entity == _entity);
		if (slot == null)
			return;

		slot.entity = null;
		slot.hasFailed = false;
		slot.respawnTime = EditorApplication.timeSinceStartup + (slot.isRespawnImmediate ? 0d : Config.respawnDelay);
		slot.isRespawnImmediate = false;
	}

	private static void OnStartInputPhase ()
	{
		if (CurrentStep != Step.Launching && CurrentStep != Step.Running)
			return;

		if (CurrentStep == Step.Launching)
		{
			if (GameManager.Instance.PlayersEntityAnchor[c_playerID].Entities.Count == 0)
			{
				Debug.LogWarning("Labo de composants : une phase de saisie a démarré avant l'apparition des unités, elle est ignorée.");
				return;
			}

			OnMatchStarted();
		}
		else
			AttachUnmappedEntities();

		foreach (Slot slot in GetSlots())
		{
			if (!slot.isUnit && slot.IsAlive)
				TurnManager.Instance.RemainingActionToken[slot.entity.ID] = 0;
		}

		if (m_isBenchUnitPending)
			ApplyBench(true);
		if (m_isBenchDummyPending)
			ApplyBench(false);
	}

	private static void ProcessRespawns ()
	{
		double now = EditorApplication.timeSinceStartup;
		foreach (Slot slot in GetSlots().ToList())
		{
			if (!slot.IsWaitingRespawn || now < slot.respawnTime)
				continue;

			try
			{
				slot.respawnTime = TrySpawn(slot) ? -1d : now + c_retryDelay;
			}
			catch (Exception exception)
			{
				slot.respawnTime = -1d;
				slot.hasFailed = true;
				Debug.LogException(exception);
			}
		}
	}

	private static bool TrySpawn ( Slot _slot )
	{
		TurnManager turnManager = TurnManager.Instance;
		if (GridManager.Instance == null || GameManager.Instance == null || turnManager == null || turnManager.currentPhase == TurnManager.TurnPhase.Off)
			return false;

		Tile tile = GridManager.Instance.Tiles[_slot.tileID];
		if (IsTileTaken(tile))
			return false;

		EntityAnchor anchor = GameManager.Instance.PlayersEntityAnchor[_slot.playerID];
		Entity entity = anchor.SpawnEntity(_slot.preset.GetSavedData(), _slot.playerID, new EntityAnchor.Spawn(tile.coordinates, _slot.playerID == c_playerID, true));
		bool isInputPhase = turnManager.currentPhase == TurnManager.TurnPhase.Recording;
		turnManager.RemainingActionToken[entity.ID] = isInputPhase && _slot.isUnit ? GameConfig.current.game.actionTokenPerRound : 0;
		if (!isInputPhase && _slot.isUnit)
			turnManager.AddEntityMidGame(entity);

		if (_slot.entity != entity)
			Attach(_slot, entity);

		if (_slot.isReselectNeeded && turnManager.currentPhase == TurnManager.TurnPhase.Recording && PlayerController.Instance != null)
			PlayerController.Instance.SelectEntity(entity);
		_slot.isReselectNeeded = false;
		return true;
	}

	private static bool IsTileTaken ( Tile _tile )
	{
		Entity occupant = _tile.GetCurrentEntity();
		if (occupant != null && !occupant.Equipment.IsDead)
			return true;

		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity != null && !entity.Equipment.IsDead && entity.Displacement.Coordinates.ID == _tile.coordinates.ID)
					return true;
			}
		}

		return false;
	}

	private static void Remove ( Slot _slot )
	{
		if (_slot.IsWaitingRespawn)
		{
			_slot.respawnTime = EditorApplication.timeSinceStartup;
			return;
		}

		Entity entity = _slot.entity;
		if (entity == null || entity.Equipment.IsDead)
		{
			_slot.respawnTime = EditorApplication.timeSinceStartup;
			return;
		}

		_slot.isRespawnImmediate = true;
		_slot.isReselectNeeded = PlayerController.Instance != null && PlayerController.Instance.SelectedEntity == entity;
		foreach (Item item in GameManager.Instance.Items.ToList())
		{
			if (item != null && item.Invocator == entity)
				item.Kill();
		}

		entity.AllPassiveEffects.Clear();
		entity.Equipment.InstantDeath();
	}

	#endregion

	#region Tools

	public static void Focus ( StationRuntime _station )
	{
		Entity entity = _station.unit.entity;
		if (!IsInputPhase || entity == null || entity.Equipment.IsDead || PlayerController.Instance == null)
			return;

		if (!TurnManager.Instance.RemainingActionToken.ContainsKey(entity.ID))
		{
			Message = _station.unit.name + " n'a pas encore de tokens : attends la prochaine phase de saisie.";
			return;
		}

		if (PlayerController.Instance.SelectedEntity != entity)
			PlayerController.Instance.SelectEntity(entity);
	}

	public static void ResetStation ( StationRuntime _station )
	{
		if (!IsInputPhase)
			return;

		foreach (Slot slot in _station.Slots)
			Remove(slot);

		ProcessRespawns();
	}

	public static void ApplyBench ( bool _isUnit )
	{
		StationRuntime bench = m_stations.FirstOrDefault(station => station.data.isBench);
		if (bench == null || !IsRunning)
			return;

		if (!IsInputPhase)
		{
			if (_isUnit)
				m_isBenchUnitPending = true;
			else
				m_isBenchDummyPending = true;
			return;
		}

		if (_isUnit)
			m_isBenchUnitPending = false;
		else
			m_isBenchDummyPending = false;

		ComponentTestLabUnitSpec spec = _isUnit ? Layout.CreateBenchUnit(Config.benchUnit) : Layout.CreateBenchDummy(Config.benchDummy);
		if (!spec.IsValid)
		{
			Message = "Banc libre : il manque un composant principal.";
			return;
		}

		Slot slot = _isUnit ? bench.unit : bench.dummies[0];
		slot.preset = spec.CreatePreset(slot.name);
		Remove(slot);
		ProcessRespawns();
	}

	public static bool SetCover ( StationRuntime _station, ComponentTestLabCoverState _state )
	{
		if (!IsInputPhase || _station.data.coverTile < 0)
			return false;

		Tile tile = GridManager.Instance.Tiles[_station.data.coverTile];
		if (IsTileTaken(tile))
		{
			Message = "La case du couvert est occupée.";
			return false;
		}

		tile.RemoveWall();
		if (_state == ComponentTestLabCoverState.None)
			tile.SetGroundType(TileGroundType.Empty);
		else
		{
			tile.SetGroundType(_state == ComponentTestLabCoverState.Cover ? TileGroundType.Cover : TileGroundType.Wall);
			tile.SetupWall(Wall.WallType.VerticalStrait, (tile.coordinates.OffsetZ & 1) == 1 ? 3 : 0, _state == ComponentTestLabCoverState.Cover);
		}

		GridManager.Instance.MarkLineOfSightDirty();
		RefreshVisionsAround(tile);
		_station.cover = _state;
		return true;
	}

	private static void RefreshVisionsAround ( Tile _tile )
	{
		int range = GameConfig.current.game.rangePerVisionType.Values.DefaultIfEmpty(0).Max() + 1;
		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities.ToList())
			{
				if (entity != null && !entity.Equipment.IsDead && entity.Displacement.Coordinates.DistanceTo(_tile.coordinates) <= range)
					GridManager.Instance.OnEntityMovement(entity);
			}
		}
	}

	public static void SetRevealed ( bool _isRevealed )
	{
		if (!IsRunning || _isRevealed == IsRevealed || GridManager.Instance == null)
			return;

		foreach (Tile tile in GridManager.Instance.Tiles)
			tile.SetActiveFOW(NeuronalMembraneEquipmentData.VisionTypes.Optic, !_isRevealed, true);

		IsRevealed = _isRevealed;
		if (FogOfWarRenderer.Instance != null)
			FogOfWarRenderer.Instance.MarkDirty();
	}

	public static void ApplyStatus ( Entity _entity, EntityStatusEnumID _status, int _duration )
	{
		if (!IsInputPhase || _entity == null || _entity.Equipment.IsDead || _status == EntityStatusEnumID.Unknown)
			return;

		_entity.AddStatus(_status, Mathf.Max(1, _duration));
	}

	public static void ClearStatuses ( Entity _entity )
	{
		if (!IsInputPhase || _entity == null || _entity.Equipment.IsDead)
			return;

		foreach (EntityStatusEnumID status in _entity.Status.ToArray())
			_entity.RemoveStatus(status);
	}

	public static StationRuntime GetStationOf ( Entity _entity )
	{
		return _entity == null ? null : m_stations.FirstOrDefault(station => station.Slots.Any(slot => slot.entity == _entity));
	}

	#endregion

	#region Coverage

	public static string GetCoverageKey ( ComponentTestLabStation _station, EntityActionEnumID _action )
	{
		return _station.id + "/" + _action;
	}

	private static void OnActionPerformed ( Slot _slot, AEntityAction _action )
	{
		if (_action == null || Config == null || !_slot.station.data.testedActions.Contains(_action.enumID))
			return;

		string key = GetCoverageKey(_slot.station.data, _action.enumID);
		if (Config.performedActions.Contains(key))
			return;

		Config.performedActions.Add(key);
		EditorUtility.SetDirty(Config);
	}

	#endregion
}
