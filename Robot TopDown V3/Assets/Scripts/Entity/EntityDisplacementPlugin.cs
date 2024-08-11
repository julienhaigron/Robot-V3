using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityDisplacementPlugin : RobotPlugin
{
	private TileCoordinates m_coordinate;
	public TileCoordinates Coordinates => m_coordinate;

	[SerializeField] private Transform m_bottomPosition;

	public void Init ( RobotAnchor.Spawn _spawn )
	{
		MoveToTile(_spawn.coordinates.GetTile(), null);
	}

	public void MoveToTile( Tile _tile , System.Action onMovementDoneAction)
	{
		m_coordinate.GetTile().SetEntity(null, _isThisTurn: true);

		transform.position = _tile.transform.position - m_bottomPosition.localPosition;
		_tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(_tile.coordinates.X, _tile.coordinates.Z, _tile.coordinates.ID);

		onMovementDoneAction?.Invoke();
	}
}
