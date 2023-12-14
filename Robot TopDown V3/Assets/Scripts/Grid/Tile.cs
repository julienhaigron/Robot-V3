using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Tile : MonoBehaviour
{
	public static float outerRadius = 1f;
	public static float innerRadius = outerRadius * 0.866025404f;

	[SerializeField] private TileUIPlugin m_ui;
	public TileUIPlugin UI => m_ui;
	public TileCoordinates coordinates;

	public RobotEntity entity;

	#region Pathfinding params
	private int m_distance;
	public int Distance
	{
		get
		{
			return m_distance;
		}
		set
		{
			m_distance = value;
			m_ui.UpdateDistanceLabel(value);
		}
	}

	#endregion

	public void SetPosition(int _x, int _y )
	{
		m_ui.SetPosition(_x, _y);
	}
}
