using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{

	
	[SerializeField] private RobotAnchor m_robotAnchor;
	public RobotAnchor RobotsAnchor => m_robotAnchor;


}
