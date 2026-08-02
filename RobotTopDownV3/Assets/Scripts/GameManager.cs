using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistant<GameManager>
{
	public static Action onStartLevel;

	[SerializeField] private Canvas m_fogCanvas;

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
	[SerializeField] private GridData m_onlineMap;
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

	private void Start ()
	{
		m_fogCanvas.gameObject.SetActive(false);
	}

	public void LoadSaveAndGoToHub ( int _saveID )
	{
		GameDatas.current.game.lastPlayerSaveSelectedID = _saveID;
		m_currentGameMode = GameMode.Offline;

		FTUEManager.Instance.InitFTUE();

		bool doesTuto = !GameDatas.current.currentPlayerSave.DidFirstIntroLevel;
#if UNITY_EDITOR
		if (GameConfig.current.debug.skipFTUE)
			doesTuto = false;
#endif

		if (!doesTuto)
		{
			LoadingManager.Instance.LoadScene(GameConfig.current.game.hubSceneName
			, () =>
			{
				UIManager.Instance.ClosePanel<StartMenuPanel>(true);
			}
			, () =>
			{
				UIManager.Instance.ShowTopCanvas<HubTopCanvas>();

				if (!GameDatas.current.currentPlayerSave.cycleData.didSelectMissions)
					UIManager.Instance.OpenPanel<SelectMissionPanel>();
				else if (m_returnFromMatch)
				{
					if (GameDatas.current.currentPlayerSave.didUnlockRetuurnToHubPopup)
						UIManager.Instance.OpenPopup<ReturnToHubPopup>().Init();
					UIManager.Instance.OpenPanel<SoloHubPanel>();
				}
				else
					UIManager.Instance.OpenPanel<SoloHubPanel>();
			});
		}
		else
		{
			//TODO : play introduction video/animation before throwing player into gameplay

			if (!GameDatas.current.currentPlayerSave.didStartTuto)
			{
				foreach (UnitPreset unitPreset in FTUEManager.Instance.playerStartingSquadUnits)
				{
					unitPreset.AddToUnits();

				}
				GameDatas.current.currentPlayerSave.didStartTuto = true;
				for (int i = 0; i < FTUEManager.Instance.Cycle1MatchMissions.Length; i++)
					GameDatas.current.currentPlayerSave.cycleData.selectedMissionsIds.Add(FTUEManager.Instance.Cycle1MatchMissions[i].enumID);
			}

			SetupLevel(FTUEManager.Instance.Cycle1MatchMissions[0]);
		}
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

		LoadingManager.Instance.LoadScene(_mission.map.name
		, () =>
		{
			if (UIManager.Instance.currentPanel != null && UIManager.Instance.currentPanel is StartMenuPanel)
				UIManager.Instance.ClosePanel<StartMenuPanel>(true);
		}
		, () =>
		{
			StartGame();
		});
	}

	public void GoBackToHub ()
	{
		m_returnFromMatch = true;
		m_currentMission = null;
		foreach (EntityAnchor anchor in m_playersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				Destroy(entity.gameObject);
			}
			anchor.Entities.Clear();
		}

		UIManager.Instance.ClosePanel<InGamePanel>();
		LoadSaveAndGoToHub(GameDatas.current.game.lastPlayerSaveSelectedID);
	}

	public void GoToStartScreen ()
	{
		LoadingManager.Instance.LoadScene(GameConfig.current.game.startScreenSceneName
		, () =>
		{
			UIManager.Instance.HideTopCanvas<HubTopCanvas>();
			UIManager.Instance.ClosePanel<SoloHubPanel>(true);
		}
		, () =>
		{
			UIManager.Instance.OpenPanel<StartMenuPanel>();
		});
	}


	public void StartGame ()
	{
		TurnManager.Instance.Init();
		GridManager.Instance.LoadGrid();
		UIManager.Instance.HideTopCanvas<HubTopCanvas>();

		if (m_currentGameMode == GameMode.Offline)
		{
			LogConsole.AddLog("Start OfflineGame", LogConsole.LogEventType.DebugSys);
			m_playersEntityAnchor[0].Init(GameDatas.current.currentPlayerSave.squadUnits, 0);
			List<EntitySavedData> ennemies = new();
			foreach (UnitPreset ennemi in m_currentMission.enemies)
			{
				ennemies.Add(ennemi.GetSavedData());
			}
			m_playersEntityAnchor[1].Init(ennemies, 1);
		}
		else if (m_currentGameMode == GameMode.Online)
		{
			LogConsole.AddLog("Start OnlineGame", LogConsole.LogEventType.DebugSys);
			//TODO send player info if online
			m_playersEntityAnchor[0].Init(m_playerOneEntityDatas, 0);
			m_playersEntityAnchor[1].Init(m_playerTwoEntityDatas, 1);
		}

		UIManager.Instance.OpenPanel<InGamePanel>().Init();
		onStartLevel?.Invoke();

		m_fogCanvas.gameObject.SetActive(true);
		TurnManager.Instance.StartInputPhase();
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
			foreach (Entity entity in anchor.Entities)
			{
				if (entity.ID == _entityID)
					return entity;
			}
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

	public Item PreSpawnItem ( AItemData _itemData, Entity _caster, Tool _invocatorTool, TileCoordinates _coordinate )
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

	public void EndGame ( EndLevelPopup.GameResult _gameResult )
	{
		GameDatas.current.currentPlayerSave.NewDay();
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
		bool isValid = true;

		foreach (EntitySavedData entityData in GameDatas.current.currentPlayerSave.squadUnits)
		{
			if (!entityData.IsUnitValid())
				isValid = false;
		}

		return isValid;
	}

}
