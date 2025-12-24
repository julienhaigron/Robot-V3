using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
	[SerializeField] private List<GameObject> m_wallParts;

	//saved infos
    [SerializeField] private Tile m_linkedTile;
	public Tile LinkedTile { get { return m_linkedTile; } set { m_linkedTile = value; } }
	
	[SerializeField] private bool m_isDestructible = true;
	public bool IsDestructible { get { return m_isDestructible; } set { m_isDestructible = value; } }
	
	[SerializeField] private int m_health;
	public int Health { get { return m_health; } set { m_health = value; } }
	
	[SerializeField] private int m_orientation; //between 0-5
	public int Orientation { get { return m_orientation; } set { m_orientation = value; } }
	
	[SerializeField] private WallType m_type;
	public WallType Type { get { return m_type; } set { m_type = value; } }

	public enum WallType
	{
		Strait,
		LAngle,
		ReverseLAngle,
		XAngle
	}
	//TODO :
	// => Destructible feature
	// => cover feature

	//EDITOR :
	// - lier le visuel d'un mur facilement à une tile
    // - déterminé + visualiser la "coverability" d'un mur, angle
	// - tourner un mur

	public void LinkWithTile ( Tile m_tile )
	{
		m_linkedTile = m_tile;
	}

	public void TakeDamage(int _amount )
	{
		m_health -= _amount;
		if (m_health <= 0)
			Destroy();
	}

	private void Destroy ()
	{
		
	}

}
