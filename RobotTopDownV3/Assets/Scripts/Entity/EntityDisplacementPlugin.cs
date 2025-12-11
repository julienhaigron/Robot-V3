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

	private EntityAnchor.Spawn m_spawn;
	public EntityAnchor.Spawn Spawn => m_spawn;

	public void SetSpawn ( EntityAnchor.Spawn _spawn )
	{
		//MoveToTile(_spawn.coordinates.GetTile(), null);
		Tile spawn = _spawn.coordinates.GetTile();
		transform.position = spawn.transform.position - m_bottomPosition.localPosition;

		if (!_spawn.isFirstSide)
			Rotate(180f, true);

		spawn.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(spawn.coordinates.X, spawn.coordinates.Z, spawn.coordinates.ID);

		onAnyEntitySpawn.Invoke(m_linkedEntity);
	}

	public void MoveToTile( int _tileID , System.Action onMovementDoneAction)
	{
		if(m_coordinate.GetTile().GetEntity(true) == m_linkedEntity)
			m_coordinate.GetTile().SetEntity(null, _isThisTurn: true);
		Tile tile = GridManager.Instance.Tiles[_tileID];
		Rotate(tile, false);

		transform.DOMove(tile.transform.position - m_bottomPosition.localPosition, GameConfig.current.game.actionDuration).SetEase(Ease.Linear).OnComplete(() => onMovementDoneAction?.Invoke());
		tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(tile.coordinates.X, tile.coordinates.Z, tile.coordinates.ID);

		//refresh fow
		onAnyEntityMovement?.Invoke(m_linkedEntity);
	}

	public void Rotate(Tile _towards, bool _isInstant )
	{
		//TODO : cleaner rotation here
		m_linkedEntity.SkinParent.transform.LookAt(_towards.transform, Vector3.up);
	}

	public void Rotate(float _direction, bool _isInstant )
	{
		m_linkedEntity.SkinParent.transform.localRotation = Quaternion.Euler(0, _direction, 0);
	}
}
