using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{

	[SerializeField] private EntityAnchor m_playerEntityAnchor;
	public EntityAnchor PlayerEntitiesAnchor => m_playerEntityAnchor;
	
	[SerializeField] private EntityAnchor m_ennemiEntityAnchor;
	public EntityAnchor EnnemiEntityAnchor => m_ennemiEntityAnchor;

	/*private LobbyManager m_lobby;
	public LobbyManager Lobby => m_lobby;

	private PlayerConnector m_connector;
	public PlayerConnector Connector => m_connector;*/

	[SerializeField] private GridData m_map;
	[SerializeField] private List<EntityData> m_playerEntityDatas;
	[SerializeField] private List<EntityData> m_ennemiEntityDatas;

	private void Start ()
	{
		if (m_map != null)
			GridManager.Instance.LoadGrid(m_map);
		else
			GridManager.Instance.GenerateGrid(10, 10);


		m_playerEntityAnchor.Init(m_playerEntityDatas);
		m_ennemiEntityAnchor.Init(m_ennemiEntityDatas);

		TurnManager.Instance.Init();
		TurnManager.Instance.StartInputPhase();
	}

	public void LevelCompletionCheck(out bool areAllEnemyDead, out bool areAllPlayerEntityDead )
	{
		areAllEnemyDead = true;
		areAllPlayerEntityDead = true;
		foreach(Entity enemy in m_ennemiEntityAnchor.Entities)
		{
			if (enemy.Equipment.IsDead == false)
				areAllEnemyDead = false;
		}

		foreach (Entity ally in m_playerEntityAnchor.Entities)
		{
			if (ally.Equipment.IsDead == false)
				areAllPlayerEntityDead = false;
		}
	}

}
