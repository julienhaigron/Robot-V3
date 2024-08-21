using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

		transform.DOMove(_tile.transform.position - m_bottomPosition.localPosition, 1.5f).SetEase(Ease.OutExpo).OnComplete(() => onMovementDoneAction?.Invoke());
		_tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(_tile.coordinates.X, _tile.coordinates.Z, _tile.coordinates.ID);

		//transform.position = _tile.transform.position - m_bottomPosition.localPosition;
		//onMovementDoneAction?.Invoke();
	}
}
