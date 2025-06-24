using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;

public class GridManager : Singleton<GridManager>
{
	private Tile[] m_tiles;
	public Tile[] Tiles => m_tiles;

	private int m_height;
	public int Height => m_height;
	private int m_width;
	public int Width => m_width;

	private struct PlayerVisionRangeInfo
	{
		public Dictionary<Entity, List<Tile>> entitiesVisionRange;

		public PlayerVisionRangeInfo ( Dictionary<Entity, List<Tile>> _entitiesVisionRange = null)
		{
			entitiesVisionRange = _entitiesVisionRange;
		}
	}
	private Dictionary<int, PlayerVisionRangeInfo> m_entitiesVisions = new();

	#region Editor
#if UNITY_EDITOR

	[HideInInspector] public bool isGroundBrushSelected = false;
	[HideInInspector] public TileGroundType currentGroundBrushSelected;

#endif
	#endregion

	public override void Awake ()
	{
		base.Awake();
		InputManager.onTileSelected += OnTileSelected;
		EntityDisplacementPlugin.onAnyEntityMovement += OnEntityMovement;
		EntityDisplacementPlugin.onAnyEntitySpawn += OnNewEntity;
		EntityEquipmentPlugin.onAnyEntityDeath += OnEntityDeath;
	}

	private void OnDestroy ()
	{
		InputManager.onTileSelected -= OnTileSelected;
		EntityDisplacementPlugin.onAnyEntityMovement -= OnEntityMovement;
		EntityDisplacementPlugin.onAnyEntitySpawn -= OnNewEntity;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnEntityDeath;
	}

	#region Creation

	[Button("LoadGrid")]
	public void LoadGrid ( GridData _data )
	{
		GenerateGrid(_data.height, _data.width);

		m_entitiesVisions.Add(0, new(new Dictionary<Entity, List<Tile>>()));
		m_entitiesVisions.Add(1, new(new Dictionary<Entity, List<Tile>>()));

		for (int i = 0; i < m_tiles.Length; i++)
		{
			TileGroundType groundType = _data.tiles[i].groundType;
			m_tiles[i].SetGroundType(groundType);

			if (groundType == TileGroundType.PlayerSpawn)
				GameManager.Instance.PlayersEntityAnchor[0].AddSpawn(m_tiles[i].coordinates);
			else if(groundType == TileGroundType.EnemySpawn)
				GameManager.Instance.PlayersEntityAnchor[1].AddSpawn(m_tiles[i].coordinates);
		}
	}

	[Button("GenerateGrid")]
	public void GenerateGrid ( int _height, int _width )
	{
		m_tiles = new Tile[_height * _width];
		m_height = _height;
		m_width = _width;

		for (int i = transform.childCount; i > 0; --i)
			DestroyImmediate(transform.GetChild(0).gameObject);

		for (int z = 0, i = 0; z < _height; z++)
		{
			for (int x = 0; x < _width; x++)
			{
				CreateTile(x, z, i++);
			}
		}
	}

	private void CreateTile ( int _x, int _z, int _i )
	{
		Vector3 position;
		position.x = (_x + _z * 0.5f - _z / 2) * (Tile.innerRadius * 2f);
		position.y = 0f;
		position.z = _z * (Tile.outerRadius * 1.5f);

		Tile newTile = Instantiate(GameAssets.current.game.baseTile);
		m_tiles[_i] = newTile;

		newTile.transform.SetParent(transform, false);
		newTile.transform.localPosition = position;
		newTile.Init(_x, _z);
		newTile.coordinates = TileCoordinates.FromOffsetCoordinates(_x, _z, _i);

		if (_x > 0)
		{
			newTile.SetNeighbor(HexDirection.W, m_tiles[_i - 1]);
		}
		if (_z > 0)
		{
			if ((_z & 1) == 0)
			{
				newTile.SetNeighbor(HexDirection.SE, m_tiles[_i - m_width]);
				if (_x > 0)
				{
					newTile.SetNeighbor(HexDirection.SW, m_tiles[_i - m_width - 1]);
				}
			}
			else
			{
				newTile.SetNeighbor(HexDirection.SW, m_tiles[_i - m_width]);
				if (_x < m_width - 1)
				{
					newTile.SetNeighbor(HexDirection.SE, m_tiles[_i - m_width + 1]);
				}
			}
		}
	}

	#endregion

	#region Utils

	public List<Tile> GetPath ( Tile _from, Tile _to , bool _isThisTurn)
	{
		BFS(_from, _to: _to, _isThisTurn: _isThisTurn);

		if (_to.Distance == int.MaxValue)
			return null;

		List<Tile> path = new();
		path.Add(_to);
		Queue<Tile> search = new Queue<Tile>();
		search.Enqueue(_to);

		int currentDistance = _to.Distance;
		while(search.Count > 0)
		{
			Tile current = search.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				Tile neighbor = current.GetNeighbor((HexDirection)i);
				if (neighbor == null || neighbor.Distance >= currentDistance)
				{
					continue;
				}

				currentDistance = neighbor.Distance;
				search.Enqueue(neighbor);
				path.Add(neighbor);
			}
		}

		return path;
	}

	public List<Entity> GetEntitiesInRange(Tile _from, int _maxDist, bool _isThisTurn )
	{
		List<Entity> entitiesInRange = new();

		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
			//m_tiles[i].UI.ResetOutline();
		}

		Queue<Tile> frontier = new Queue<Tile>();
		_from.Distance = 0;
		frontier.Enqueue(_from);

		while (frontier.Count > 0)
		{
			Tile current = frontier.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				//yield return new WaitForSeconds(1 / 60f);
				Tile neighbor = current.GetNeighbor((HexDirection)i);

				if (neighbor == null || neighbor.Distance != int.MaxValue)
				{
					continue;
				}

				//max distance
				if (current.Distance + 1 > _maxDist)
				{
					continue;
				}

				//obstacle
				if (neighbor.GetEntity(_isThisTurn) != null)
				{
					entitiesInRange.Add(neighbor.GetEntity(_isThisTurn));
				}

				neighbor.Distance = current.Distance + 1;
				frontier.Enqueue(neighbor);
			}
		}


		return entitiesInRange;
	}

	public List<Tile> GetTilesInRange(Tile _from, int _maxDist, bool _isThisTurn )
	{
		List<Tile> tilesInRange = new();

		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
			//m_tiles[i].UI.ResetOutline();
		}

		Queue<Tile> frontier = new Queue<Tile>();
		_from.Distance = 0;
		frontier.Enqueue(_from);

		while (frontier.Count > 0)
		{
			Tile current = frontier.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				//yield return new WaitForSeconds(1 / 60f);
				Tile neighbor = current.GetNeighbor((HexDirection)i);

				if (neighbor == null || neighbor.Distance != int.MaxValue)
				{
					continue;
				}

				//max distance
				if (current.Distance + 1 > _maxDist)
				{
					continue;
				}

				//obstacle
				if (neighbor.CanSeeThrough())
				{
					tilesInRange.Add(neighbor);
				}

				neighbor.Distance = current.Distance + 1;
				frontier.Enqueue(neighbor);
			}
		}

		return tilesInRange;
	}

	public List<Tile> GetTilesInVisionRange(Tile _from, int _maxDist, bool _isThisTurn )
	{
		List<Tile> tilesInRange = new();
		tilesInRange.Add(_from);

		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
			//m_tiles[i].UI.ResetOutline();
		}

		Queue<Tile> frontier = new Queue<Tile>();
		_from.Distance = 0;
		frontier.Enqueue(_from);

		while (frontier.Count > 0)
		{
			Tile current = frontier.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				//yield return new WaitForSeconds(1 / 60f);
				Tile neighbor = current.GetNeighbor((HexDirection)i);

				if (neighbor == null || neighbor.Distance != int.MaxValue)
				{
					continue;
				}

				//max distance
				if (current.Distance + 1 > _maxDist)
				{
					continue;
				}

				//obstacle
				if (IsVisionLineClear(_from, neighbor, _isThisTurn))
				{
					tilesInRange.Add(neighbor);
				}

				neighbor.Distance = current.Distance + 1;
				frontier.Enqueue(neighbor);
			}
		}

		return tilesInRange;
	}

	public bool IsVisionLineClear(Tile _from, Tile _to, bool _isThisTurn )
	{
		List<Tile> tilesInLine = new();

		int nbOfRayPer = 3;
		float distBetweenRay = .2f;
		float distance = Vector3.Distance(_from.transform.position, _to.transform.position);
		Vector3 direction = (_to.transform.position - _from.transform.position).normalized;
		Vector3 perp = Vector3.Cross(direction, Vector3.up).normalized;
		for (int i = 0; i < nbOfRayPer; i++)
		{
			Vector3 from = _from.transform.position + perp * (( i - 1) * distBetweenRay);
			RaycastHit[] hits = Physics.RaycastAll(from, direction, distance, GameConfig.current.input.tileInternRayCastLayer);
			foreach (RaycastHit hitInfo in hits)
			{
				if (hitInfo.transform.TryGetComponent(out Tile tile) && !tilesInLine.Contains(tile))
				{
					tilesInLine.Add(tile);
				}
			}
		}

		foreach(Tile tile in tilesInLine)
		{
			if (tile != _to && !tile.CanSeeThrough())
				return false;
		}

		return true;
	}

	public void ClearTileOutile ()
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].UI.ResetOutline();
		}
	}

	public void BFS ( Tile cell , int _maxDistance = -1, Tile _to = null, bool _isThisTurn = false)
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
			//m_tiles[i].UI.ResetOutline();
		}

		Queue<Tile> frontier = new Queue<Tile>();
		cell.Distance = 0;
		frontier.Enqueue(cell);
		bool isDestinationReached = false;

		while (frontier.Count > 0 && isDestinationReached == false)
		{
			Tile current = frontier.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				//yield return new WaitForSeconds(1 / 60f);
				Tile neighbor = current.GetNeighbor((HexDirection)i);

				//destination reached
				if (_to != null && neighbor == _to)
					isDestinationReached = true;

				if (neighbor == null || neighbor.Distance != int.MaxValue)
				{
					continue;
				}

				//max distance
				if(_maxDistance != -1 && current.Distance + 1 > _maxDistance)
				{
					continue;
				}

				//obstacle
				if (neighbor.IsObstacle() ||(neighbor.GetEntity(_isThisTurn) != null && neighbor != _to))
				{
					continue;
				}

				neighbor.Distance = current.Distance + 1;
				frontier.Enqueue(neighbor);
			}
		}
	}

	public float GetAngleFrom ( Vector2Int origin, Vector2Int destination )
	{
		/*int deltaX = origin.x - destination.x;
		int deltaY = origin.y - destination.y;
		return Mathf.Atan2(deltaY, deltaX) * 180f / Mathf.PI;*/
		int x1 = origin.x + 1 - origin.x; //Vector 1 - x
		int y1 = origin.y - origin.y; //Vector 1 - y

		int x2 = destination.x - origin.x; //Vector 2 - x
		int y2 = destination.y - origin.y; //Vector 2 - y

		float angle = Mathf.Atan2(y1, x1) - Mathf.Atan2(y2, x2);
		angle = angle * 360 / (2 * Mathf.PI);

		if (angle < 0)
		{
			angle += 360;
		}

		return angle;
	}

	public int GetDistanceBetween(Tile _from, Tile _to, bool _isThisTurn = false )
	{
		BFS(_from, _to: _to, _isThisTurn: _isThisTurn);

		return _to.Distance;
	}

	#endregion

	#region Turn sys

	public void StartNewPhase ()
	{
		for(int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].NewPhase();
		}
	}

	#endregion

	#region FOW

	public void OnNewEntity(Entity _entity )
	{
		if (!_entity.IsOwn())
			return;

		List<Tile> tileInEntityRange = GetTilesInVisionRange(_entity.Displacement.Coordinates.GetTile(), _entity.Data.visibilityRange, true);

		foreach (Tile tile in tileInEntityRange)
		{
			tile.UI.SetActiveFOW(false, true);
		}

		m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Add(_entity, tileInEntityRange);
	}

	public void OnEntityDeath(Entity _entity )
	{
		if (!_entity.IsOwn())
			return;

		foreach (Tile tile in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[_entity])
		{
			bool isInAnotherEntityVisionRange = false;
			foreach(Entity otherEntities in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Keys)
			{
				if (m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[otherEntities].Contains(tile))
				{
					isInAnotherEntityVisionRange = true;
					break;
				}
			}

			if (!isInAnotherEntityVisionRange)
				tile.UI.SetActiveFOW(false, false);
		}

		m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Remove(_entity);
	}

	public void OnEntityMovement(Entity _entity )
	{
		if (!_entity.IsOwn())
			return;

		List<Tile> previousTilesInRangeList = new(m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[_entity]);
		List<Tile> newTilesInRangeList = GetTilesInVisionRange(_entity.Displacement.Coordinates.GetTile(), _entity.Data.visibilityRange, true);
		m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[_entity] = new(newTilesInRangeList);

		foreach (Tile tile in newTilesInRangeList)
		{
			if (!previousTilesInRangeList.Contains(tile))
			{
				bool isInAnotherEntityVisionRange = false;
				foreach(Entity entity in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Keys)
				{
					if (entity == _entity) continue;

					if (m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[entity].Contains(tile))
					{
						isInAnotherEntityVisionRange = true;
						break;
					}
				}

				if(!isInAnotherEntityVisionRange)
					tile.UI.SetActiveFOW(false, false);
			}
		}

		foreach(Tile previousTile in previousTilesInRangeList)
		{
			bool isInAnotherEntityVisionRange = false;
			foreach (Entity entity in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Keys)
			{
				if (m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[entity].Contains(previousTile))
				{
					isInAnotherEntityVisionRange = true;
					break;
				}
			}

			if(!isInAnotherEntityVisionRange)
				previousTile.UI.SetActiveFOW(true, false);
		}
	}

	#endregion

	#region Editor Callbacks

	private void OnTileSelected ( Tile _tile )
	{

#if UNITY_EDITOR
		if (isGroundBrushSelected)
		{
			_tile.SetGroundType(currentGroundBrushSelected);
			return;
		}

#endif

	}

	#endregion

}

[System.Serializable]
public struct TileCoordinates
{

	[SerializeField] private int m_x;
	[SerializeField] private int m_z;
	[SerializeField] private int m_id;

	public int X
	{
		get
		{
			return m_x;
		}
	}
	public int Z
	{
		get
		{
			return m_z;
		}
	}
	public int Y
	{
		get
		{
			return -X - Z;
		}
	}

	public int ID => m_id;

	public TileCoordinates ( int x, int z, int id )
	{
		m_x = x;
		m_z = z;
		m_id = id;
	}

	public void SetCoordinate(int x, int z, int id )
	{
		m_x = x;
		m_z = z;
		m_id = id;
	}

	public static TileCoordinates FromOffsetCoordinates ( int x, int z, int id )
	{
		return new TileCoordinates(x - z / 2, z, id);
	}

	public Tile GetTile ()
	{
		Tile tile = GridManager.Instance.Tiles[ID];
		//Debug.Log("this :" + X + "," + Z + " getTile: " + tile.coordinates.X +"," + tile.coordinates.Z);
		return tile;
	}

	/*public int DistanceTo ( TileCoordinates other )
	{
		return
		((m_x < other.m_x ? other.m_x - m_x : m_x - other.m_x) +
		(Y < other.Y ? other.Y - Y : Y - other.Y) +
		(m_z < other.m_z ? other.m_z - m_z : m_z - other.m_z)) / 2;
	}*/

	public override string ToString ()
	{
		return "(" +
			X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	/*public string ToStringOnSeparateLines ()
	{
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}*/

	/*public static TileCoordinates FromPosition ( Vector3 position )
	{
		float x = position.x / (Tile.innerRadius * 2f);
		float y = -x;
		float offset = position.z / (Tile.outerRadius * 3f);
		x -= offset;
		y -= offset;
		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x - y);

		if (iX + iY + iZ != 0)
		{
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x - y - iZ);

			if (dX > dY && dX > dZ)
			{
				iX = -iY - iZ;
			}
			else if (dZ > dY)
			{
				iZ = -iX - iY;
			}
		}

		return new TileCoordinates(iX, iZ);
	}*/

	public Entity IsOccupied (bool _isThisTurn)
	{
		return GetTile().GetEntity(_isThisTurn: _isThisTurn);
	}

}

public enum HexDirection
{
	NE, E, SE, SW, W, NW
}

public enum TileGroundType
{
	Empty,
	Wall,
	Door,
	PlayerSpawn,
	EnemySpawn
}

public static class HexDirectionExtensions
{

	public static HexDirection Opposite ( this HexDirection direction )
	{
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}
}