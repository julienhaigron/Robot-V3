using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDisplacementPlugin : RobotPlugin
{
    private TileCoordinates m_coordinate;
    public TileCoordinates Coordinates => m_coordinate;

    [SerializeField] private Transform m_bottomPosition;

    public void Init ( RobotAnchor.Spawn _spawn)
	{
        transform.position = m_bottomPosition.localPosition + _spawn.coordinates.GetTile().transform.position;
	}
}
