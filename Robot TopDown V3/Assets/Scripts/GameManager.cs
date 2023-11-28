using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{

	[SerializeField] private GridManager m_gridManager;
	public GridManager Grid => m_gridManager;
	
	[SerializeField] private RobotAnchor m_robotAnchor;
	public RobotAnchor RobotsAnchor => m_robotAnchor;


}
