using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridManager : Singleton<GridManager>
{
	[SerializeField, HideInInspector] private Tile[] m_tiles;
	public Tile[] Tiles => m_tiles;

	[SerializeField] private int m_height;
	public int Height => m_height;
	[SerializeField] private int m_width;
	public int Width => m_width;

	/*public Texture2D fogTexture;
	[SerializeField] private MeshRenderer m_fowRenderer;*/

	private struct PlayerVisionRangeInfo
	{
		public Dictionary<Entity, List<Tile>> entitiesVisionRange;

		public PlayerVisionRangeInfo ( Dictionary<Entity, List<Tile>> _entitiesVisionRange = null )
		{
			entitiesVisionRange = _entitiesVisionRange;
		}
	}
	private Dictionary<int, PlayerVisionRangeInfo> m_entitiesVisions = new();

	private static readonly Vector2Int[] HexDirectionOffsets =
	{
		new Vector2Int( 0,  1), // 0
		new Vector2Int( 1,  0), // 1
		new Vector2Int( 1, -1), // 2

		new Vector2Int( 0, -1), // 3
		new Vector2Int(-1,  0), // 4
		new Vector2Int(-1,  1), // 5
	};

	#region Editor
#if UNITY_EDITOR

	public bool isGroundBrushSelected = false;
	public TileGroundType currentGroundBrushSelected;
	public GridData gridData;

#endif
	#endregion

	public override void Awake ()
	{
		base.Awake();
		//InputManager.onTileSelected += OnTileSelected;
		EntityDisplacementPlugin.onAnyEntityMovement += OnEntityMovement;
		EntityDisplacementPlugin.onAnyEntitySpawn += OnNewEntity;
		EntityEquipmentPlugin.onAnyEntityDeath += OnEntityDeath;
	}

	private void OnDestroy ()
	{
		//InputManager.onTileSelected -= OnTileSelected;
		EntityDisplacementPlugin.onAnyEntityMovement -= OnEntityMovement;
		EntityDisplacementPlugin.onAnyEntitySpawn -= OnNewEntity;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnEntityDeath;
	}

	#region Creation

	public void LoadGrid ( GridData _data, bool _isEditorMode = false )
	{
		//GenerateGrid(_data.height, _data.width);

		m_entitiesVisions.Clear();
		m_entitiesVisions.Add(0, new(new Dictionary<Entity, List<Tile>>()));
		m_entitiesVisions.Add(1, new(new Dictionary<Entity, List<Tile>>()));

		for (int i = 0; i < m_tiles.Length; i++)
		{
			TileGroundType groundType = _data.tiles[i].groundType;

			if (_isEditorMode)
			{
				m_tiles[i].SetGroundType(groundType);
				if (groundType == TileGroundType.Wall)
				{
					m_tiles[i].Wall.SetWallType(_data.tiles[i].wallType);
					m_tiles[i].Wall.Rotate(_data.tiles[i].orientation);
				}
			}
			else
			{
				m_tiles[i].SetActiveFOW(true, true);

				if (groundType == TileGroundType.PlayerSpawn)
					GameManager.Instance.PlayersEntityAnchor[0].AddSpawn(m_tiles[i].coordinates, true);
				else if (groundType == TileGroundType.EnemySpawn)
					GameManager.Instance.PlayersEntityAnchor[1].AddSpawn(m_tiles[i].coordinates, false);
			}

		}
	}

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

		UnityEditor.EditorUtility.SetDirty(this);
		//InitFOW();
	}

	private void CreateTile ( int _x, int _z, int _i )
	{
		Vector3 position;
		position.x = (_x + _z * 0.5f - _z / 2) * (Tile.innerRadius * 2f);
		position.y = 0f;
		position.z = _z * (Tile.outerRadius * 1.5f);

		Tile newTile = Instantiate(GameAssets.current.game.baseTile);
		m_tiles[_i] = newTile;

		newTile.gameObject.name = "Tile " + _x + "." + _z;
		newTile.transform.SetParent(transform, false);
		newTile.transform.localPosition = position;
		newTile.Init(_x, _z);
		newTile.coordinates = TileCoordinates.FromOffsetCoordinates(_x, _z, _i);

		/*#if UNITY_EDITOR
				if (!Application.isPlaying)
					return;
		#endif*/
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

		UnityEditor.EditorUtility.SetDirty(newTile);
	}

	#endregion

	#region Utils

	public Tile GetTile(int _x, int _z )
	{
		if (_x < 0 || _x >= m_width || _z < 0 || _z >= m_height)
			return null;

		int index = _z * m_width + _x;
		return m_tiles[index];
	}

	public bool TryGetTile ( int _x, int _z, out Tile tile )
	{
		tile = null;

		if (_x < 0 || _x >= m_width || _z < 0 || _z >= m_height)
			return false;

		tile = m_tiles[_z * m_width + _x];
		return true;
	}

	public Tile GetTileAtOrientation ( Tile _from, int _orientation )
	{
		/*Vector2Int offset = HexDirectionOffsets[_orientation];

		int x = _from.coordinates.X + offset.x;
		int z = _from.coordinates.Z + offset.y;

		return GetTile(x, z);*/

		return _from.Neighbors[_orientation];
	}

	public List<Tile> GetPath ( Tile _from, Tile _to, bool _isThisTurn )
	{
		BFS(_from, _to: _to, _isThisTurn: _isThisTurn);

		if (_to.Distance == int.MaxValue)
			return null;

		List<Tile> path = new();
		path.Add(_to);
		Queue<Tile> search = new Queue<Tile>();
		search.Enqueue(_to);

		int currentDistance = _to.Distance;
		while (search.Count > 0)
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

	public List<Entity> GetEntitiesInRange ( Tile _from, int _maxDist, bool _isThisTurn )
	{
		List<Entity> entitiesInRange = new();

		List<Tile> tilesInRange = GetTilesInVisionRange(_from, _maxDist, _isThisTurn);
		foreach (Tile tile in tilesInRange)
		{
			Entity entity = tile.GetEntity(_isThisTurn);
			if (entity != null && !entitiesInRange.Contains(entity))
				entitiesInRange.Add(entity);
		}

		return entitiesInRange;
	}
	/*public List<Entity> GetEntitiesInRange(Tile _from, int _maxDist, bool _isThisTurn )
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
	}*/

	public List<Tile> GetTilesInRange ( Tile _from, int _maxDist, bool _isThisTurn )
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

	public List<Tile> GetTilesInVisionRange ( Tile _from, int _maxDist, bool _isThisTurn )
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

	public bool IsVisionLineClear ( Tile _from, Tile _to, bool _isThisTurn )
	{
		//Soltion to test:
		//1) from one point (FromTileCenter), several ray as a rectangle, validate vision if at least one ray can see without interuption
		//2) from two point (from left and right feets suposing an entity would be looking toward target tile

		int rayAmount = 7;
		float distBetweenRay = Tile.innerRadius / (float)rayAmount;
		Vector3 ab = (_to.transform.position - _from.transform.position).normalized;
		Vector3 perp = Vector3.Cross(ab, Vector3.up).normalized;
		for (int i = 0; i < rayAmount; i++)
		{
			float offset = (i - (rayAmount - 1) * 0.5f) * distBetweenRay;
			Vector3 rayOrigin = _from.transform.position + perp * offset;

			Vector3 direction = (_to.transform.position - rayOrigin).normalized;
			float distance = Vector3.Distance(rayOrigin, _to.transform.position);
			RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, distance, GameConfig.current.input.wallRayCastLayer);
			if (hits == null || hits.Length == 0)
				return true;
			else
			{
				foreach (RaycastHit hit in hits)
				{
					if (hit.collider.transform.parent.parent.TryGetComponent(out Tile tile) && tile != _to)
						return false;
				}

				return true;
			}
		}

		return false;
	}

	public bool IsThereCoverBeween ( Entity _attacker, Entity _target )
	{
		// Orientation depuis l’attaquant vers la cible
		int attackOrientation = GetClosestOrientation(_attacker.Displacement.Coordinates.GetTile(), _target.Displacement.Coordinates.GetTile());

		// Tile potentielle de couvert
		Tile potentialCover = GetTileAtOrientation(_attacker.Displacement.Coordinates.GetTile(), attackOrientation);

		return potentialCover != null && potentialCover.GroundType == TileGroundType.Cover;
	}

	public Tile.TileDirectionType GetHitTileSide ( Entity _from, Entity _to )
	{
		int hitOrientation = GetClosestOrientation(_to.Displacement.Coordinates.GetTile(), _from.Displacement.Coordinates.GetTile());
		int targetOrientation = _to.Displacement.CurrentOrientation;
		int delta = (hitOrientation - targetOrientation + 6) % 6;

		switch (delta)
		{
			case 0:
				return Tile.TileDirectionType.Front;

			case 1:
			case 5:
				return Tile.TileDirectionType.ForwardSide;

			case 2:
			case 4:
				return Tile.TileDirectionType.BackSide;

			case 3:
				return Tile.TileDirectionType.Back;

			default:
				// Ne devrait jamais arriver
				return Tile.TileDirectionType.Front;
		}
	}

	public int GetClosestOrientation ( Tile _from, Tile _to )
	{
		Vector2 origin = new Vector2(_from.transform.position.x, _from.transform.position.z);
		Vector2 destination = new Vector2(_to.transform.position.x, _to.transform.position.z);
		float angle = GetAngleFrom(origin, destination);

		return (int)((angle - 30f) / 60f);
	}

	public void ClearTileOutile ()
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].UI.ResetOutline();
		}
	}

	public void BFS ( Tile cell, int _maxDistance = -1, Tile _to = null, bool _isThisTurn = false )
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
				if (_maxDistance != -1 && current.Distance + 1 > _maxDistance)
				{
					continue;
				}

				//obstacle
				if (neighbor.IsObstacle() || (neighbor.GetEntity(_isThisTurn) != null && neighbor != _to))
				{
					continue;
				}

				neighbor.Distance = current.Distance + 1;
				frontier.Enqueue(neighbor);
			}
		}
	}

	public float GetAngleFrom ( Vector2 origin, Vector2 destination )
	{
		/*int deltaX = origin.x - destination.x;
		int deltaY = origin.y - destination.y;
		return Mathf.Atan2(deltaY, deltaX) * 180f / Mathf.PI;*/
		float x1 = origin.x + 1 - origin.x; //Vector 1 - x
		float y1 = origin.y - origin.y; //Vector 1 - y

		float x2 = destination.x - origin.x; //Vector 2 - x
		float y2 = destination.y - origin.y; //Vector 2 - y

		float angle = Mathf.Atan2(y1, x1) - Mathf.Atan2(y2, x2);
		angle = angle * 360 / (2 * Mathf.PI);

		if (angle < 0)
		{
			angle += 360;
		}

		return angle;
	}

	public int GetDistanceBetween ( Tile _from, Tile _to, bool _isThisTurn = false )
	{
		BFS(_from, _to: _to, _isThisTurn: _isThisTurn);

		return _to.Distance;
	}

	#endregion

	#region Turn sys

	public void StartNewPhase ()
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].NewPhase();
		}
	}

	#endregion

	#region FOW

	/*public void InitFOW ( )
	{
		fogTexture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false);
		fogTexture.filterMode = FilterMode.Point;

		// Tout noir = full fog
		Color[] colors = new Color[m_width * m_height];
		for (int i = 0; i < colors.Length; i++)
			colors[i] = Color.black;

		fogTexture.SetPixels(colors);
		fogTexture.Apply();
		//Shader.SetGlobalTexture("_FogTex", fogTexture);
		m_fowRenderer.material.SetTexture("_FogTex", fogTexture);
	}*/

	public void OnNewEntity ( Entity _entity )
	{
		int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
		if (!_entity.IsAlliedTo(playerId))
		{
			_entity.SetVisibility(false);
			return;
		}

		Tile from = _entity.Displacement.Coordinates.GetTile();
		from.SetActiveFOW(false, true);
		List<Tile> tileInEntityRange = GetTilesInVisionRange(from, _entity.Data.FrameData.visibilityRange, true);

		foreach (Tile tile in tileInEntityRange)
		{
			tile.SetActiveFOW(false, true);
		}

		m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Add(_entity, tileInEntityRange);

		//m_fowRenderer.material.SetTexture("_FogTex", fogTexture);
	}

	public void OnEntityDeath ( Entity _entity )
	{
		int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
		if (!_entity.IsAlliedTo(playerId))
			return;

		foreach (Tile tile in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[_entity])
		{
			bool isInAnotherEntityVisionRange = false;
			foreach (Entity otherEntities in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Keys)
			{
				if (m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[otherEntities].Contains(tile))
				{
					isInAnotherEntityVisionRange = true;
					break;
				}
			}

			if (!isInAnotherEntityVisionRange)
				tile.SetActiveFOW(false, false);
		}

		m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Remove(_entity);

		//m_fowRenderer.material.SetTexture("_FogTex", fogTexture);
	}

	public void OnEntityMovement ( Entity _entity )
	{
		int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
		if (!_entity.IsAlliedTo(playerId))
		{
			_entity.SetVisibility(_entity.Displacement.Coordinates.GetTile().IsVisible);
			return;
		}

		List<Tile> previousTilesInRangeList = new(m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[_entity]);
		List<Tile> newTilesInRangeList = GetTilesInVisionRange(_entity.Displacement.Coordinates.GetTile(), _entity.Data.FrameData.visibilityRange, true);
		m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[_entity] = new(newTilesInRangeList);

		foreach (Tile tile in newTilesInRangeList)
		{
			if (!previousTilesInRangeList.Contains(tile))
			{
				bool isInAnotherEntityVisionRange = false;
				foreach (Entity entity in m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange.Keys)
				{
					if (entity == _entity) continue;

					if (m_entitiesVisions[_entity.PlayerOwnerID].entitiesVisionRange[entity].Contains(tile))
					{
						isInAnotherEntityVisionRange = true;
						break;
					}
				}

				if (!isInAnotherEntityVisionRange)
					tile.SetActiveFOW(false, false);
			}
		}

		foreach (Tile previousTile in previousTilesInRangeList)
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

			if (!isInAnotherEntityVisionRange)
				previousTile.SetActiveFOW(true, false);
		}

		//m_fowRenderer.material.SetTexture("_FogTex", fogTexture);
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

	public void SetCoordinate ( int x, int z, int id )
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

	public Entity IsOccupied ( bool _isThisTurn )
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
	EnemySpawn,
	Cover
}

public static class HexDirectionExtensions
{

	public static HexDirection Opposite ( this HexDirection direction )
	{
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}
}