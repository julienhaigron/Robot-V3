using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

public class Tile : MonoBehaviour
{
	public static float outerRadius = 1f;
	public static float innerRadius = outerRadius * 0.866025404f;

	[Title("Depedencies")]
	[SerializeField] private TileUIPlugin m_ui;
	public TileUIPlugin UI => m_ui;
	

	public TileCoordinates coordinates;
	[SerializeField, ReadOnly] Tile[] m_neighbors;
	public Tile[] Neighbors => m_neighbors;

	//ground
	private TileGroundType m_groundType;
	public TileGroundType GroundType => m_groundType;

	//Content on tile
	private RobotEntity m_entity;
	public RobotEntity Entity => m_entity;

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

	private void Awake ()
	{
		m_neighbors = new Tile[6];
	}

	public void Init(int _x, int _y, TileGroundType _groundType = TileGroundType.Empty)
	{
		m_ui.SetPosition(_x, _y);
		m_groundType = _groundType;
	}

	public void SetEntity(RobotEntity _entity )
	{
		m_entity = _entity;
	}

	public void SetGroundType (TileGroundType _groundType)
	{
		m_groundType = _groundType;
		m_ui.UpdateGroundMaterial();
	}

	public Tile GetNeighbor ( HexDirection _direction )
	{
		return m_neighbors[(int)_direction];
	}
	
	public void SetNeighbor ( HexDirection _direction, Tile _tile )
	{
		m_neighbors[(int)_direction] = _tile;
		_tile.Neighbors[(int)_direction.Opposite()] = this;
	}

	public bool IsObstacle ()
	{
		if (m_groundType != TileGroundType.Empty || m_entity != null)
			return true;

		return false;
	}

	public bool CanSeeThrough ()
	{
		if (m_groundType == TileGroundType.Wall)
			return false;

		return true;
	}
}
