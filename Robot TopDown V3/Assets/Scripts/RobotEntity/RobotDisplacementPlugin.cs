using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotDisplacementPlugin : RobotPlugin
{
	private TileCoordinates m_coordinate;
	public TileCoordinates Coordinates => m_coordinate;

	[SerializeField] private Transform m_bottomPosition;

	public void Init ( RobotAnchor.Spawn _spawn )
	{
		MoveToTile(_spawn.coordinates.GetTile());
	}

	public void MoveToTile( Tile _tile )
	{
		m_coordinate.GetTile().SetEntity(null);

		transform.position = _tile.transform.position - m_bottomPosition.localPosition;
		_tile.SetEntity(m_linkedEntity);
		m_coordinate.SetCoordinate(_tile.coordinates.X, _tile.coordinates.Z, _tile.coordinates.ID);
	}
}
