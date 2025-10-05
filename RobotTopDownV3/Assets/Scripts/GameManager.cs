using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

public class GameManager : Singleton<GameManager>
{
	[SerializeField] private EntityAnchor[] m_playersEntityAnchor;
	public EntityAnchor[] PlayersEntityAnchor => m_playersEntityAnchor;

	[SerializeField] private LobbyManager m_lobby;
	public LobbyManager Lobby => m_lobby;

	[Title("Offline")]
	[SerializeField] private GridData m_map;
	[SerializeField] private List<FrameEquipmentData> m_playerEntityDatas;
	[SerializeField] private List<FrameEquipmentData> m_ennemiEntityDatas;

	[Title("Online")]
	[SerializeField] private GridData m_onlineMap;
	[SerializeField] private List<FrameEquipmentData> m_playerOneEntityDatas;
	[SerializeField] private List<FrameEquipmentData> m_playerTwoEntityDatas;
	public enum GameMode { Offline, Online }

	private GameMode m_currentGameMode;
	public GameMode CurrentGameMode { get { return m_currentGameMode; } set { m_currentGameMode = value; } }
	public bool IsOnline => m_currentGameMode == GameMode.Online;

	private void Start ()
	{
		if (m_map != null)
			GridManager.Instance.LoadGrid(m_map);
		else
			GridManager.Instance.GenerateGrid(10, 10);

		/*if(m_currentGameMode == GameMode.Offline)
			StartGame();*/
	}

	public void SetPlayerUnit(List<FrameEquipmentData> _playerUnits )
	{

	}

	public void SetupLevel(LevelData _level )
	{
		m_map = _level.map;
		m_playerTwoEntityDatas = _level.enemies;
	}

	public void StartGame ()
	{
		if (m_currentGameMode == GameMode.Offline)
		{
			LogConsole.AddLog("Start OfflineGame", LogConsole.LogEventType.Main);
			m_playersEntityAnchor[0].Init(m_playerEntityDatas, 0);
			m_playersEntityAnchor[1].Init(m_ennemiEntityDatas, 1);
		}
		else if (m_currentGameMode == GameMode.Online)
		{
			LogConsole.AddLog("Start OnlineGame", LogConsole.LogEventType.Main);
			//TODO send player info if online
			m_playersEntityAnchor[0].Init(m_playerOneEntityDatas, 0);
			m_playersEntityAnchor[1].Init(m_playerTwoEntityDatas, 1);
		}

		TurnManager.Instance.Init();
		TurnManager.Instance.StartInputPhase();
	}

	public Entity GetEntityFromID (int _entityID)
	{
		foreach(EntityAnchor anchor in m_playersEntityAnchor)
		{
			foreach (Entity entity in anchor.Entities)
			{
				if (entity.ID == _entityID)
					return entity;
			}
		}

		return null;
	}

	public void LevelCompletionCheck(out bool _isPlayerOneDead, out bool _isPlayerTwoDead )
	{
		_isPlayerOneDead = true;
		_isPlayerTwoDead = true;
		foreach(Entity enemy in m_playersEntityAnchor[0].Entities)
		{
			if (enemy.Equipment.IsDead == false)
				_isPlayerOneDead = false;
		}

		foreach (Entity ally in m_playersEntityAnchor[1].Entities)
		{
			if (ally.Equipment.IsDead == false)
				_isPlayerTwoDead = false;
		}
	}

}
