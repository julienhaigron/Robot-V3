using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{

	[SerializeField] private EntityAnchor m_playerRobotAnchor;
	public EntityAnchor PlayerRobotsAnchor => m_playerRobotAnchor;
	
	[SerializeField] private EntityAnchor m_ennemiRobotAnchor;
	public EntityAnchor EnnemiRobotsAnchor => m_ennemiRobotAnchor;

	[SerializeField] private GridData m_map;
	[SerializeField] private List<EntityData> m_playerRobotDatas;
	[SerializeField] private List<EntityData> m_ennemiRobotDatas;

	private void Start ()
	{
		if (m_map != null)
			GridManager.Instance.LoadGrid(m_map);
		else
			GridManager.Instance.GenerateGrid(10, 10);


		m_playerRobotAnchor.Init(m_playerRobotDatas);
		m_ennemiRobotAnchor.Init(m_ennemiRobotDatas);

		TurnManager.Instance.StartInputPhase();
	}

}
