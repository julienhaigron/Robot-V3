using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EntityDisplacementPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityMovement;
	public static System.Action<Entity> onAnyEntitySpawn;

	private TileCoordinates m_coordinate;
	public TileCoordinates Coordinates => m_coordinate;

	[SerializeField] private Transform m_bottomPosition;

	public void SetSpawn ( EntityAnchor.Spawn _spawn )
	{
		//MoveToTile(_spawn.coordinates.GetTile(), null);
		Tile spawn = _spawn.coordinates.GetTile();
		transform.position = spawn.transform.position - m_bottomPosition.localPosition;
		spawn.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(spawn.coordinates.X, spawn.coordinates.Z, spawn.coordinates.ID);

		onAnyEntitySpawn.Invoke(m_linkedEntity);
	}

	public void MoveToTile( int _tileID , System.Action onMovementDoneAction)
	{
		if(m_coordinate.GetTile().GetEntity(true) == m_linkedEntity)
			m_coordinate.GetTile().SetEntity(null, _isThisTurn: true);
		Tile tile = GridManager.Instance.Tiles[_tileID];

		//TODO : cleaner rotation here
		m_linkedEntity.SkinParent.transform.LookAt(tile.transform, Vector3.up);

		transform.DOMove(tile.transform.position - m_bottomPosition.localPosition, GameConfig.current.game.actionDuration).SetEase(Ease.Linear).OnComplete(() => onMovementDoneAction?.Invoke());
		tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(tile.coordinates.X, tile.coordinates.Z, tile.coordinates.ID);

		//refresh fow
		onAnyEntityMovement?.Invoke(m_linkedEntity);
	}
}
