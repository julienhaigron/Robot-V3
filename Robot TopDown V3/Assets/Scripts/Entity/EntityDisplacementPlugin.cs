using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EntityDisplacementPlugin : EntityPlugin
{
	private TileCoordinates m_coordinate;
	public TileCoordinates Coordinates => m_coordinate;

	[SerializeField] private Transform m_bottomPosition;

	public void Init ( EntityAnchor.Spawn _spawn )
	{
		//MoveToTile(_spawn.coordinates.GetTile(), null);
		Tile spawn = _spawn.coordinates.GetTile();
		transform.position = spawn.transform.position - m_bottomPosition.localPosition;
		spawn.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(spawn.coordinates.X, spawn.coordinates.Z, spawn.coordinates.ID);
	}

	public void MoveToTile( Tile _tile , System.Action onMovementDoneAction)
	{
		m_coordinate.GetTile().SetEntity(null, _isThisTurn: true);

		transform.DOMove(_tile.transform.position - m_bottomPosition.localPosition, GameConfig.current.game.entityMovementSpeed).SetEase(Ease.OutExpo).OnComplete(() => onMovementDoneAction?.Invoke());
		_tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(_tile.coordinates.X, _tile.coordinates.Z, _tile.coordinates.ID);
	}
}
