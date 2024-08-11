using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{

	[SerializeField] private RobotAnchor m_robotAnchor;
	public RobotAnchor RobotsAnchor => m_robotAnchor;

	[SerializeField] private GridData m_map;
	[SerializeField] private List<EntityData> m_robots;


	private void Start ()
	{
		if (m_map != null)
			GridManager.Instance.LoadGrid(m_map);
		else
			GridManager.Instance.GenerateGrid(10, 10);


		m_robotAnchor.Init(m_robots);

		TurnManager.Instance.StartInputPhase();
	}

}
