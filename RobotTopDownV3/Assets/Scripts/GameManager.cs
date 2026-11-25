using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-20000)]
public class GameManager : SingletonPersistant<GameManager>
{
	[SerializeField] private Canvas m_fogCanvas;
	[SerializeField] private LevelReflectionProbe m_levelReflectionProbe;

	[SerializeField] private EntityAnchor[] m_playersEntityAnchor;
	public EntityAnchor[] PlayersEntityAnchor => m_playersEntityAnchor;

	[SerializeField] private LobbyManager m_lobby;
	public LobbyManager Lobby => m_lobby;

	[Title("Offline")]
	[SerializeField] private MissionData m_currentMission;
	public MissionData CurrentMission => m_currentMission;
	/*[SerializeField] private List<EntitySavedData> m_playerEntityDatas;
	[SerializeField] private List<EntitySavedData> m_ennemiEntityDatas;*/

	[Title("Online")]
	[SerializeField] private List<EntitySavedData> m_playerOneEntityDatas;
	[SerializeField] private List<EntitySavedData> m_playerTwoEntityDatas;
	public enum GameMode { Offline, Online }

	private GameMode m_currentGameMode;
	public GameMode CurrentGameMode { get { return m_currentGameMode; } set { m_currentGameMode = value; } }
	public bool IsOnline => m_currentGameMode == GameMode.Online;

	private int m_playerNumber = 0;
	public int PlayerID
	{
		get
		{
			if (m_currentGameMode == GameMode.Offline)
				return 0;
			else
				return m_playerNumber;
		}
		set
		{
			m_playerNumber = value;
		}
	}

	private bool m_returnFromMatch = false;

	private List<Item> m_items = new();
	public List<Item> Items => m_items;
	private Dictionary<int, int> m_playersPoints = new();
	public static Action onPlayersPointsChanged;

	public static Action onMatchUnitRespawned;
	private class RespawningUnit
	{
		public UnitPreset preset;
		public int playerID;
		public Entity entity;
	}
	private List<RespawningUnit> m_respawningUnits = new();

	private void Start ()
	{
		m_fogCanvas.gameObject.SetActive(false);
	}

	public void LoadSaveAndGoToHub ( int _saveID )
	{
		GameDatas.current.game.lastPlayerSaveSelectedID = _saveID;
		m_currentGameMode = GameMode.Offline;

		bool doesTuto = !GameDatas.current.currentPlayerSave.DidFirstIntroLevel;
#if UNITY_EDITOR
		if (GameConfig.current.debug.skipFTUE)
			doesTuto = false;
#endif

		if (doesTuto)
			FTUEManager.Instance.RestartIntroTutorial();

		FTUEManager.Instance.InitFTUE();

		if (!doesTuto)
		{
			LoadHubScene();
			return;
		}

		//Only the very first time: the squad and the mission list must not be handed out again.
		if (!GameDatas.current.currentPlayerSave.didStartTuto)
		{
			//TODO : play introduction video/animation before throwing player into gameplay

			foreach (UnitPreset unitPreset in FTUEManager.Instance.playerStartingSquadUnits)
				unitPreset.AddToUnits(true);

			GameDatas.current.currentPlayerSave.dayCount = -1;
			GameDatas.current.currentPlayerSave.didStartTuto = true;
			for (int i = 0; i < FTUEManager.Instance.Cycle1MatchMissions.Length; i++)
				GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds.Add(FTUEManager.Instance.Cycle1MatchMissions[i].enumID);
		}

		//Reached whether the intro level is being started or resumed. It used to sit inside the branch above, so
		//a save that had started the tuto without finishing it matched no branch at all and loading did nothing.
		SetupLevel(FTUEManager.Instance.Day0MissionData);
	}

	private void LoadHubScene ( Action _onCovered = null, ILoadingScreen _screen = null )
	{
		LoadingManager.Instance.LoadScene(GameConfig.current.game.hubSceneName
			, () =>
			{
				_onCovered?.Invoke();
				UIManager.Instance.ClosePanel<StartMenuPanel>(true);
			}
			, CameraManager.Instance.ShowHub
			, () =>
			{
				UIManager.Instance.ShowTopCanvas<HubTopCanvas>();

				string dayReport = GameDatas.current.currentPlayerSave.ProcessRecycledComponents(out List<GameDatas.PlayerSave.DayData.RecyclingComponentData> _recycledComponent);

				if (m_returnFromMatch)
				{
					if (GameDatas.current.currentPlayerSave.didUnlockReturnToHubPopup)
						UIManager.Instance.OpenPopup<ReturnToHubPopup>().Init(dayReport, _recycledComponent);
					UIManager.Instance.OpenPanel<SoloHubPanel>();
				}
				else
				{
					if (!GameDatas.current.currentPlayerSave.cycleData.didSelectMissions)
						UIManager.Instance.OpenPanel<SelectMissionPanel>();
					else
						UIManager.Instance.OpenPanel<SoloHubPanel>();
				}

				ApplicationManager.Instance.SaveApplication();
			}
			, _screen);
	}

	[Button]
	public void SetupLevel ( MissionData _mission )
	{
		m_currentMission = _mission;
		m_playerTwoEntityDatas = new();
		foreach (UnitPreset ennemi in _mission.enemies)
		{
			m_playerTwoEntityDatas.Add(ennemi.GetSavedData());
		}

		MatchBriefing briefing = UIManager.Instance.MatchBriefing;
		if (briefing != null)
			briefing.Prepare(_mission);

		LoadingManager.Instance.LoadScene(_mission.map.name
		, () =>
		{
			if (UIManager.Instance.currentPanel != null)
			{
				if (UIManager.Instance.GetTopCanvas<HubTopCanvas>().Visible)
					UIManager.Instance.HideTopCanvas<HubTopCanvas>();

				if (UIManager.Instance.currentPanel is StartMenuPanel)
					UIManager.Instance.ClosePanel<StartMenuPanel>(true);
				else if (UIManager.Instance.currentPanel is MissionPanel)
					UIManager.Instance.ClosePanel<MissionPanel>(true);
				else if (UIManager.Instance.currentPanel is TournamentPanel)
					UIManager.Instance.ClosePanel<TournamentPanel>(true);
			}

			//StartGame();
		}
		, StartGame
		, () =>
		{
			UIManager.Instance.OpenPanel<InGamePanel>().Init();
			TurnManager.Instance.StartInputPhase();
		}
		, briefing);
	}

	public void GoBackToHub ()
	{
		m_returnFromMatch = true;
		m_currentMission = null;
		UIManager.Instance.ClosePanel<InGamePanel>(true);
		LoadHubScene(ClearMatchEntities, UIManager.Instance.MatchExtraction);
	}

	private void ClearMatchEntities ()
	{
		foreach (EntityAnchor anchor in m_playersEntityAnchor)
			anchor.DestroyEntities();
	}

	public void GoToStartScreen ()
	{
		ApplicationManager.Instance.SaveApplication();

		LoadingManager.Instance.LoadScene(GameConfig.current.game.startScreenSceneName
		, () =>
		{
			UIManager.Instance.HideTopCanvas<HubTopCanvas>();
			UIManager.Instance.ClosePanel<SoloHubPanel>(true);
		}
		, null
		, () =>
		{
			UIManager.Instance.OpenPanel<StartMenuPanel>();
		});
	}


	public void StartGame ()
	{
		TurnManager.Instance.Init();
		GridManager.Instance.LoadGrid();
		if (GridManager.Instance.TryGetTileBounds(out Bounds levelArea))
			m_levelReflectionProbe.CaptureArea(levelArea, GridManager.Instance.gameObject.scene);
		m_playersPoints.Clear();
		m_items.Clear();
		m_respawningUnits.Clear();

		if (m_currentMission != null && m_currentMission.type == MissionData.MissionType.Construction)
		{
			for (int playerID = 0; playerID < m_playersEntityAnchor.Length; playerID++)
			{
				int commPointCount = GridManager.Instance.GetCommPoints(playerID).Count;
				if (commPointCount != 2)
					Debug.LogWarning("Construction match: player " + playerID + " has " + commPointCount + " communication points instead of 2.");
			}
		}

		if (m_currentMission != null && m_currentMission.type == MissionData.MissionType.Sabotage)
		{
			if (GameAssets.current.game.sabotagePointVisual == null)
				Debug.LogWarning("GameAssets.game.sabotagePointVisual is not set: sabotage points work but are invisible.");

			for (int playerID = 0; playerID < m_playersEntityAnchor.Length; playerID++)
			{
				if (GetSabotagePointCount(playerID, false) == 0)
					Debug.LogWarning("Sabotage match without any sabotage point for player " + playerID + ": that side cannot lose by sabotage.");
			}
		}

		if (m_currentMission != null && m_currentMission.type == MissionData.MissionType.DefenseDeZone && m_playersEntityAnchor[0].Zones.Count == 0)
			Debug.LogWarning("DefenseDeZone match on a map without any Zone tile: only extermination can end it.");
		if (m_currentMission != null && m_currentMission.type == MissionData.MissionType.ControleDePoint && GridManager.Instance.ControlPoints.Count == 0)
			Debug.LogWarning("ControleDePoint match on a map without any ControlPoint tile: only extermination can end it.");

		if (m_currentGameMode == GameMode.Offline)
		{
			LogConsole.AddLog("Start OfflineGame", LogConsole.LogEventType.DebugSys);
			m_playersEntityAnchor[0].Init(GameDatas.current.currentPlayerSave.GetSquadEntitiesData(), 0);
			List<EntitySavedData> ennemies = new();
			foreach (UnitPreset ennemi in m_currentMission.enemies)
			{
				ennemies.Add(ennemi.GetSavedData());
			}
			m_playersEntityAnchor[1].Init(ennemies, 1);
			if (m_currentMission.type == MissionData.MissionType.Assassinat)
			{
				m_playersEntityAnchor[0].DesignateRandomKing();
				m_playersEntityAnchor[1].DesignateRandomKing();
			}
			if (m_currentMission.type == MissionData.MissionType.Construction || m_currentMission.type == MissionData.MissionType.Sabotage)
			{
				foreach (UnitPreset preset in m_currentMission.allies)
				{
					for (int playerID = 0; playerID < m_playersEntityAnchor.Length; playerID++)
					{
						Entity ally = m_playersEntityAnchor[playerID].SpawnEntity(preset.GetSavedData(), playerID);
						if (m_currentMission.type == MissionData.MissionType.Sabotage)
							ally.SetMatchRole(Entity.MatchRoleType.Demolisher);
						else
						{
							ally.SetMatchRole(Entity.MatchRoleType.Technician);
							m_respawningUnits.Add(new RespawningUnit() { preset = preset, playerID = playerID, entity = ally });
						}
					}
				}
			}
		}
		else if (m_currentGameMode == GameMode.Online)
		{
			LogConsole.AddLog("Start OnlineGame", LogConsole.LogEventType.DebugSys);
			//TODO send player info if online
			m_playersEntityAnchor[0].Init(m_playerOneEntityDatas, 0);
			m_playersEntityAnchor[1].Init(m_playerTwoEntityDatas, 1);
		}
		m_fogCanvas.gameObject.SetActive(true);
		CameraManager.Instance.TeleportCameraTo(m_playersEntityAnchor[PlayerID].Entities[0].transform.position, Quaternion.identity);
		PlayerController.Instance.ClearGhosts();
		PlayerController.Instance.ResetZoom();
		PlayerController.Instance.ResetRotation();
		PrewarmMatchPools();

		/*UIManager.Instance.OpenPanel<InGamePanel>().Init();
		TurnManager.Instance.StartInputPhase();*/
	}

	private void PrewarmMatchPools ()
	{
		HashSet<PoolData> pools = new() { GameAssets.current.game.deathVFXPoolData, GameAssets.current.game.doomDeathVFXPoolData };
		foreach (EntityAnchor anchor in m_playersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity == null)
					continue;

				foreach (MonoBehaviour behaviour in entity.GetComponentsInChildren<MonoBehaviour>(true))
					ObjectsPooling.CollectPools(behaviour, pools);
				CollectActionPools(entity.KnownedActions, pools);
				CollectActionPools(entity.KnownedModActions, pools);
			}
		}

		ObjectsPooling.Instance.Prewarm(pools);
	}

	private static void CollectActionPools ( List<EntityActionEnumID> _actionIDs, HashSet<PoolData> _pools )
	{
		if (_actionIDs == null)
			return;

		foreach (EntityActionEnumID actionID in _actionIDs)
		{
			if (!GameAssets.current.game.entityActionsData.TryGetValue(actionID, out EntityActionData data) || data == null)
				continue;

			ObjectsPooling.CollectPools(data, _pools);
			ObjectsPooling.CollectPools(data.invocatedItem, _pools);
		}
	}

	public bool GetEntityFromID ( out Entity _entity, int _entityID )
	{
		_entity = GetEntityFromID(_entityID);
		return _entity != null;
	}

	public Entity GetEntityFromID ( int _entityID )
	{
		foreach (EntityAnchor anchor in m_playersEntityAnchor)
		{
			if (anchor.TryGetEntity(_entityID, out Entity entity))
				return entity;
		}

		return null;
	}

	public Item GetItemFromID ( int _itemID )
	{
		foreach (Item item in m_items)
		{
			if (item.ID == _itemID)
				return item;
		}

		return null;
	}

	public Item PreSpawnItem ( AItemData _itemData, Entity _caster, ToolEquipment _invocatorTool, TileCoordinates _coordinate )
	{
		Tile spawnTile = _coordinate.GetTile();
		Item newItem = Instantiate(_itemData.itemPrefab, spawnTile.transform.position + (Vector3.down * 5f), Quaternion.identity);
		if (!_caster.Equipment.ItemsLinkedDataDictionary.ContainsKey(_invocatorTool.ID))
			_caster.Equipment.ItemsLinkedDataDictionary.Add(_invocatorTool.ID, _itemData.GetNewLinkedData());
		newItem.Init(m_items.Count, _itemData, _caster.Equipment.ItemsLinkedDataDictionary[_invocatorTool.ID], _caster, spawnTile);
		m_items.Add(newItem);
		//spawnTile.SetItem(newItem, true);

		_itemData.OnInvokeItem(_invocatorTool, newItem);

		return newItem;
	}

	public void RespawnDeadMatchUnits ()
	{
		bool didRespawn = false;
		foreach (RespawningUnit unit in m_respawningUnits)
		{
			if (unit.entity != null && !unit.entity.Equipment.IsDead)
				continue;

			if (!m_playersEntityAnchor[unit.playerID].TryGetFreeSpawn(out EntityAnchor.Spawn spawn))
			{
				Debug.LogWarning("No free spawn tile to bring back " + unit.preset.name + " for player " + unit.playerID + " this round.");
				continue;
			}

			unit.entity = m_playersEntityAnchor[unit.playerID].SpawnEntity(unit.preset.GetSavedData(), unit.playerID, spawn);
			unit.entity.SetMatchRole(Entity.MatchRoleType.Technician);
			didRespawn = true;

			if (unit.playerID == PlayerID)
				LogConsole.AddLog(LocalizationManager.Instance.Get(LocalizationKey.log_technician_respawn), LogConsole.LogEventType.Round);
		}

		if (didRespawn)
			onMatchUnitRespawned?.Invoke();
	}

	public int GetSabotagePointCount ( int _ownerID, bool _isAliveOnly )
	{
		int count = 0;
		foreach (Tile tile in m_playersEntityAnchor[_ownerID].Structures)
		{
			if (tile.IsSabotagePoint && (!_isAliveOnly || tile.Wall.Health > 0))
				count++;
		}

		return count;
	}

	public int GetPlayerPoints ( int _playerID )
	{
		return m_playersPoints.TryGetValue(_playerID, out int points) ? points : 0;
	}

	public void AddPointsToPlayer ( int _playerID, int _amount )
	{
		SetPlayerPoints(_playerID, GetPlayerPoints(_playerID) + _amount);
	}

	public void SetPlayerPoints ( int _playerID, int _points )
	{
		m_playersPoints[_playerID] = _points;
		onPlayersPointsChanged?.Invoke();
	}

	public void EndGame ( EndLevelPopup.GameResult _gameResult )
	{
		//SaveMacroChanges();

		if (_gameResult == EndLevelPopup.GameResult.Win)
			LogConsole.AddLog("Victory", LogConsole.LogEventType.DebugSys);
		else if (_gameResult == EndLevelPopup.GameResult.Loose)
			LogConsole.AddLog("Defeat", LogConsole.LogEventType.DebugSys);
		else
			LogConsole.AddLog("Draw", LogConsole.LogEventType.DebugSys);

		UIManager.Instance.ClosePanel<InGamePanel>(true);
		UIManager.Instance.OpenPopup<EndLevelPopup>().Init(_gameResult, m_currentMission);
		m_fogCanvas.gameObject.SetActive(false);
	}

	public void ApplyMatchProgression ( EndLevelPopup.GameResult _gameResult )
	{
		GameDatas.PlayerSave save = GameDatas.current.currentPlayerSave;
		bool wasTournamentMatch = save.IsInTournament;
		bool wasFinal = save.TournamentRound >= 2;
		string challengerId = save.cycleData.tournamentChallengerId;
		int cycleBeforeMatch = save.cycleCount;
		save.NewDay();

		if (!wasTournamentMatch)
			return;

		if (_gameResult == EndLevelPopup.GameResult.Win)
		{
			if (wasFinal)
				save.MarkChallengerBeaten(challengerId);
		}
		else if (save.cycleCount == cycleBeforeMatch)
			save.NewCycle();
	}

	/*private void SaveMacroChanges ()
	{
#if UNITY_EDITOR
		if (!GameConfig.current.debug.saveEntityDeathAndDamages)
			return;
#endif
		//units hp and death
		foreach(Entity entity in m_playersEntityAnchor[0].Entities)
		{
			int index = GameDatas.current.currentPlayerSave.squadUnits.FindIndex(d => d == entity.Data);
			if (index == -1)
			{
				//Debug.LogError("Unit not find in squad in game datas", entity.gameObject);
				continue;
			}

			if (entity.Equipment.IsDead)
				GameDatas.current.currentPlayerSave.squadUnits.Remove(entity.Data);
			else
				GameDatas.current.currentPlayerSave.squadUnits[index].currentHp = entity.Equipment.CurrentHealth;
		}

		//
	}*/

	//hub
	public bool SquadValidityPredicate ()
	{
		if (!GameDatas.current.currentPlayerSave.IsSquadEnergyValid)
			return false;

		foreach (EntitySavedData entityData in GameDatas.current.currentPlayerSave.GetSquadEntitiesData())
		{
			if (!entityData.IsUnitValid())
				return false;
		}

		return true;
	}

}
