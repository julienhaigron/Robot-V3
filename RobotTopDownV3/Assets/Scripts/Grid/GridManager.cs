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

	private Tile m_lastBFSOriginTile;
	public Tile LastBFSOriginTile => m_lastBFSOriginTile;
	private int m_lastBFSMaxDistance;
	public int LastBFSMaxDistance => m_lastBFSMaxDistance;

	#region Editor
#if UNITY_EDITOR

	//public bool isGroundBrushSelected = false;
	public TileGroundType currentGroundBrushSelected;
	[SerializeField] private GridData m_gridData;
	public GridData GridData
	{
		get
		{
			if(m_gridData == null)
			{
				Debug.LogError("Missing GridData ScriptableObject in the GridManager of this scene");
				return null;
			}
			else
				return m_gridData;
		}
	}

	public void LoadGrid ( bool _isEditorMode = false )
	{
		//GenerateGrid(_data.height, _data.width);

		m_entitiesVisions.Clear();
		m_entitiesVisions.Add(0, new(new Dictionary<Entity, List<Tile>>()));
		m_entitiesVisions.Add(1, new(new Dictionary<Entity, List<Tile>>()));

		for (int i = 0; i < m_tiles.Length; i++)
		{
			TileGroundType groundType = GridData.tiles[i].groundType;

			if (_isEditorMode)
			{
				m_tiles[i].SetGroundType(groundType);
				if (groundType == TileGroundType.Wall)
				{
					m_tiles[i].SetupWall(GridData.tiles[i].wallType, GridData.tiles[i].orientation);
				}
				else if (m_tiles[i].Wall != null && groundType == TileGroundType.Wall)
				{
					m_tiles[i].RemoveWall();
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

	public void GenerateGrid ()
	{
		m_tiles = new Tile[GridData.height * GridData.width];
		m_height = GridData.height;
		m_width = GridData.width;

		for (int i = transform.childCount; i > 0; --i)
			DestroyImmediate(transform.GetChild(0).gameObject);

		for (int z = 0, i = 0; z < m_gridData.height; z++)
		{
			for (int x = 0; x < m_gridData.width; x++)
			{
				CreateTile(x, z, i, GridData.tiles.Length > i ? GridData.tiles[i] : null);
				i++;
			}
		}

		UnityEditor.EditorUtility.SetDirty(this);
		//InitFOW();
	}

	private void CreateTile ( int _x, int _z, int _i, GridData.TileData _data = null )
	{
		Vector3 position;
		position.x = (_x + _z * 0.5f - _z / 2) * (Tile.innerRadius * 2f);
		position.y = 0f;
		position.z = _z * (Tile.outerRadius * 1.5f);

		Tile newTile = PrefabUtility.InstantiatePrefab(GameAssets.current.game.baseTile) as Tile;
		m_tiles[_i] = newTile;

		newTile.gameObject.name = "Tile " + _x + "." + _z;
		newTile.transform.SetParent(transform, false);
		newTile.transform.localPosition = position;
		newTile.Init(_x, _z, _data);
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

	public void SaveGrid ()
	{
		GridData.height = m_height;
		GridData.width = m_width;
		GridData.tiles = new GridData.TileData[m_tiles.Length];

		for (int i = 0; i < m_tiles.Length; i++)
		{
			GridData.tiles[i] = new GridData.TileData
				(m_tiles[i].GroundType,
				m_tiles[i].GroundType == TileGroundType.Wall ? m_tiles[i].Wall.Type : Wall.WallType.VerticalStrait,
				m_tiles[i].GroundType == TileGroundType.Wall ? m_tiles[i].Wall.Orientation : 0);
		}

		EditorUtility.SetDirty(m_gridData);
	}

	[Button]
	public void FixTiles ()
	{
		int counter = 0;
		foreach (Tile tile in m_tiles)
		{
			GridData.TileData tileData = m_gridData.tiles[counter++];

			if (tile.GroundType != TileGroundType.Wall)
			{
				tile.RemoveWall();
			}
			else if (tile.GroundType == TileGroundType.Wall)
			{
				if (tile.Wall != null && tile.WallPartsParent.childCount > 0)
				{
					for (int i = tile.WallPartsParent.childCount - 1; i >= 0; i--)
					{
						DestroyImmediate(tile.WallPartsParent.GetChild(i).gameObject);
					}
				}
				tile.SetupWall(tileData.wallType, tileData.orientation);
			}

			EditorUtility.SetDirty(tile);
		}
	}

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

	#region Utils

	public Tile GetTileAtOrientation ( Tile _from, int _orientation )
	{
		return _from.Neighbors[_orientation];
	}

	public List<Tile> GetPath ( Tile _from, Tile _to, bool _isThisTurn, bool _ignoreObstacles = false )
	{
		BFS(_from, _to: _to, _isThisTurn: _isThisTurn, _ignoreObstacles: _ignoreObstacles);

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

	public List<Tile> GetLine ( Tile _from, Tile _to )
	{
		List<Tile> results = new();

		TileCoordinates a = _from.coordinates;
		TileCoordinates b = _to.coordinates;

		int N = a.DistanceTo(b);

		for (int i = 0; i <= N; i++)
		{
			float t = N == 0 ? 0f : (float)i / N;

			CubeF lerp = CubeLerp(a, b, t);
			TileCoordinates coord = CubeRound(lerp);

			Tile tile = coord.GetTile();
			if (tile != null && !results.Contains(tile))
				results.Add(tile);
		}

		return results;
	}
	public CubeF CubeLerp ( TileCoordinates a, TileCoordinates b, float t )
	{
		return new CubeF(
			Mathf.Lerp(a.X, b.X, t),
			Mathf.Lerp(a.Y, b.Y, t),
			Mathf.Lerp(a.Z, b.Z, t)
		);
	}

	public TileCoordinates CubeRound ( CubeF f )
	{
		int rx = Mathf.RoundToInt(f.x);
		int ry = Mathf.RoundToInt(f.y);
		int rz = Mathf.RoundToInt(f.z);

		float dx = Mathf.Abs(rx - f.x);
		float dy = Mathf.Abs(ry - f.y);
		float dz = Mathf.Abs(rz - f.z);

		if (dx > dy && dx > dz)
			rx = -ry - rz;
		else if (dy > dz)
			ry = -rx - rz;
		else
			rz = -rx - ry;

		return new TileCoordinates(rx, rz, -1);
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

	public List<Tile> GetTilesInRange ( Tile _from, int _maxDist, bool _ignoreObsacles, bool _isThisTurn )
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
				if (_ignoreObsacles || neighbor.CanSeeThrough())
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

	public List<Tile> GetTilesInRay ( Tile _from, Tile _to, bool _isThisTurn )
	{
		List<Tile> tilesInRange = new();

		TileCoordinates a = _from.coordinates;
		TileCoordinates b = _to.coordinates;
		Vector2 origin = new Vector2(_from.transform.position.x, _from.transform.position.z);
		Vector2 destination = new Vector2(_to.transform.position.x, _to.transform.position.z);
		float angle = GetAngleFrom(origin, destination);
		bool isInSplitLine = (angle - 30f) % 60f == 0f;

		int N = a.DistanceTo(b);

		for (int i = 0; i <= N; i++)
		{
			float t = N == 0 ? 0f : (float)i / N;

			CubeF f = CubeLerp(a, b, t);
			TileCoordinates c = CubeRound(f);

			Tile mainTile = c.GetTile();
			if (mainTile == _from)
				continue;

			if (mainTile == null || mainTile.IsObstacle(_isThisTurn))
				break;

			TryAdd(c, tilesInRange, _isThisTurn);

			if (!isInSplitLine)
				continue;

			float dx = f.x - c.X;
			float dy = f.y - c.Y;
			float dz = f.z - c.Z;
			if (dx == 0 && dy == 0 && dz == 0)
				continue;
			else if (dx == 0f || dy == 0f || dz == 0f)
			{
				int dir = 0;
				if (dx == 0f)
				{
					if (dy > 0)
						dir = dx > 0 ? 5 : 2;
					else
						dir = dx < 0 ? 2 : 5;
				}
				else if (dy == 0f)
				{
					if (dx > 0)
						dir = dz > 0 ? 4 : 1;
					else
						dir = dz < 0 ? 1 : 4;
				}
				else if (dz == 0f)
				{
					if (dz > 0)
						dir = dy > 0 ? 3 : 0;
					else
						dir = dy < 0 ? 0 : 3;
				}

				TileCoordinates adj = CubeNeighbor(c, dir);
				TryAdd(adj, tilesInRange, _isThisTurn);
			}

			/*for (int dir = 0; dir < 6; dir++)
			{
				TileCoordinates neighbor = CubeNeighbor(c, dir);
				TryAdd(neighbor, tilesInRange);
			}*/

		}

		return tilesInRange;
	}

	private static readonly (int x, int y, int z)[] CubeDirections = new (int, int, int)[]
	{
		(1, -1, 0), (1, 0, -1), (0, 1, -1),
		(-1, 1, 0), (-1, 0, 1), (0, -1, 1)
	};

	private TileCoordinates CubeNeighbor ( TileCoordinates c, int direction )
	{
		var dir = CubeDirections[direction];
		return new TileCoordinates(c.X + dir.x, c.Z + dir.z, -1);
	}

	void TryAdd ( TileCoordinates c, List<Tile> set, bool _isThisTurn )
	{
		Tile t = c.GetTile();
		if (t != null && !t.IsObstacle(_isThisTurn))
			set.Add(t);
	}

	public List<Tile> GetTilesInCone ( Tile _from, int _distance, int _orientation, EntityActionData.ConeType _type, bool _isThisTurn )
	{
		List<Tile> tilesInRange = new();

		Tile origin = _from.Neighbors[_orientation];
		if (origin == null || !IsVisionLineClear(_from, origin, _isThisTurn))
			return tilesInRange;

		List<Tile> previousLine = new();
		previousLine.Add(origin);
		tilesInRange.Add(origin);
		int leftOrientation = _orientation - 1;
		if (leftOrientation < 0)
			leftOrientation += 6;
		int rightOrientation = _orientation + 1;
		if (rightOrientation > 5)
			rightOrientation -= 6;

		int cursor = 0;
		/*if(_type == EntityActionData.ConeType.Thin)
		{
			if (_distance <= 1 || !IsVisionLineClear(_from, origin.Neighbors[_orientation], _isThisTurn))
				return tilesInRange;

			previousLine.Clear();
			previousLine.Add(origin.Neighbors[_orientation]);
			tilesInRange.Add(origin.Neighbors[_orientation]);
			cursor++;
		}*/

		Tile leftestTile = previousLine[0];
		Tile rightestTile = previousLine[^1];
		for (; cursor < _distance - 1; cursor++)
		{
			List<Tile> newLine = new();

			if (_type == EntityActionData.ConeType.Thin)
			{
				if (cursor % 2 == 0)
				{
					if (leftestTile.Neighbors[_orientation] != null && IsVisionLineClear(origin, leftestTile.Neighbors[_orientation], _isThisTurn))
						leftestTile = leftestTile.Neighbors[_orientation];
					if (rightestTile.Neighbors[_orientation] != null && IsVisionLineClear(origin, rightestTile.Neighbors[_orientation], _isThisTurn))
						rightestTile = rightestTile.Neighbors[_orientation];
					foreach (Tile tile in previousLine)
					{
						if (tile.Neighbors[_orientation] != null && IsVisionLineClear(origin, tile.Neighbors[_orientation], _isThisTurn))
							newLine.Add(tile.Neighbors[_orientation]);
					}
				}
				else
				{
					if (leftestTile.Neighbors[leftOrientation] != null && IsVisionLineClear(origin, leftestTile.Neighbors[leftOrientation], _isThisTurn))
					{
						leftestTile = leftestTile.Neighbors[leftOrientation];
						newLine.Add(leftestTile);
					}
					if (rightestTile.Neighbors[rightOrientation] != null && IsVisionLineClear(origin, rightestTile.Neighbors[rightOrientation], _isThisTurn))
					{
						rightestTile = rightestTile.Neighbors[rightOrientation];
						newLine.Add(rightestTile);
					}
					for (int j = 0; j < previousLine.Count; j++)
					{
						if (previousLine[j].Neighbors[_orientation] != null && IsVisionLineClear(origin, previousLine[j].Neighbors[_orientation], _isThisTurn))
						{
							newLine.Add(previousLine[j].Neighbors[_orientation]);
						}
					}
				}
			}
			else
			{
				if (cursor == 0)
				{
					if (previousLine[0].Neighbors[leftOrientation] != null && IsVisionLineClear(origin, previousLine[0].Neighbors[leftOrientation], _isThisTurn))
					{
						newLine.Add(previousLine[0].Neighbors[leftOrientation]);
						leftestTile = previousLine[0].Neighbors[leftOrientation];
					}
					if (previousLine[0].Neighbors[_orientation] != null && IsVisionLineClear(origin, previousLine[0].Neighbors[_orientation], _isThisTurn))
						newLine.Add(previousLine[0].Neighbors[_orientation]);
					if (previousLine[0].Neighbors[rightOrientation] != null && IsVisionLineClear(origin, previousLine[0].Neighbors[rightOrientation], _isThisTurn))
					{
						newLine.Add(previousLine[0].Neighbors[rightOrientation]);
						rightestTile = previousLine[0].Neighbors[rightOrientation];
					}
				}
				else
				{
					Tile leftAnchor = leftestTile.Neighbors[leftOrientation];
					if (leftAnchor != null && IsVisionLineClear(origin, previousLine[0].Neighbors[leftOrientation], _isThisTurn))
					{
						newLine.Add(leftAnchor);
						if (leftAnchor.Neighbors[leftOrientation] != null && IsVisionLineClear(origin, leftAnchor.Neighbors[leftOrientation], _isThisTurn))
						{
							leftestTile = leftAnchor.Neighbors[leftOrientation];
							newLine.Add(leftestTile);
						}
						if (leftAnchor.Neighbors[_orientation] != null && IsVisionLineClear(origin, leftAnchor.Neighbors[_orientation], _isThisTurn))
							newLine.Add(leftAnchor.Neighbors[_orientation]);
					}
					Tile rightAnchor = rightestTile.Neighbors[rightOrientation];
					if (rightAnchor != null && IsVisionLineClear(origin, previousLine[0].Neighbors[rightOrientation], _isThisTurn))
					{
						newLine.Add(rightAnchor);
						if (rightAnchor.Neighbors[rightOrientation] != null && IsVisionLineClear(origin, rightAnchor.Neighbors[rightOrientation], _isThisTurn))
						{
							rightestTile = rightAnchor.Neighbors[rightOrientation];
							newLine.Add(rightestTile);
						}
						if (rightAnchor.Neighbors[_orientation] != null && IsVisionLineClear(origin, rightAnchor.Neighbors[_orientation], _isThisTurn))
							newLine.Add(rightAnchor.Neighbors[_orientation]);
					}

					foreach (Tile tile in previousLine)
					{
						if (tile.Neighbors[_orientation] != null && IsVisionLineClear(origin, tile.Neighbors[_orientation], _isThisTurn)
							&& !tilesInRange.Contains(tile.Neighbors[_orientation]) && !newLine.Contains(tile.Neighbors[_orientation]))
							newLine.Add(tile.Neighbors[_orientation]);
					}
				}

			}

			tilesInRange.AddRange(newLine);
			previousLine = new(newLine);
		}

		return tilesInRange;
	}

	public bool IsVisionLineClear ( Tile _from, Tile _to, bool _isThisTurn )
	{
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
			if (hits != null)
			{
				foreach (RaycastHit hit in hits)
				{
					if (hit.collider.transform.parent.parent.TryGetComponent(out Tile tile) && tile != _to)
						return false;
				}

			}
		}

		return true;
	}

	public bool IsThereCoverBeween ( Entity _attacker, Entity _target, bool _didAttackerWinPFC )
	{
		// Orientation depuis l’attaquant vers la cible
		int attackerPosition = _didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_attacker.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_attacker.ID);
		int defenderPosition = !_didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_target.ID);
		int attackOrientation = GetClosestOrientation(m_tiles[attackerPosition], m_tiles[defenderPosition]);

		// Tile potentielle de couvert
		Tile potentialCover = GetTileAtOrientation(m_tiles[attackerPosition], attackOrientation);

		return potentialCover != null && potentialCover.GroundType == TileGroundType.Wall;
	}

	public Tile.TileDirectionType GetHitTileSide ( Entity _from, Entity _to, bool _didAttackerWinPFC )
	{
		int attackerPosition = _didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_from.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_from.ID);
		int defenderPosition = !_didAttackerWinPFC ? TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_to.ID) : TurnManager.Instance.GetPositionOfEntityAtEndOfRound(_to.ID);
		int hitOrientation = GetClosestOrientation(m_tiles[attackerPosition], m_tiles[defenderPosition]);
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
		angle += 90f;
		if (angle > 360)
			angle -= 360f;
		if (angle < 0)
			angle += 360f;

		return (int)((angle - 30f) / 60f);
	}

	public float FromOrientationToAngle ( int _orientation )
	{
		float angle = 30f + _orientation * 60f;
		angle -= 90f;
		//float angle = (_orientation * 60f) + 60f;

		/*angle -= 90f;

		if (angle < 0f)
			angle += 360f;
		if (angle >= 360f)
			angle -= 360f;*/

		return angle;
	}

	public void ClearTileOutile ()
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].UI.ResetOutline();
		}
	}

	public void BFS ( Tile _from, int _maxDistance = -1, Tile _to = null, bool _isThisTurn = false, bool _ignoreObstacles = false )
	{
		m_lastBFSOriginTile = _from;
		m_lastBFSMaxDistance = _maxDistance;
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
		}

		Queue<Tile> frontier = new Queue<Tile>();
		_from.Distance = 0;
		frontier.Enqueue(_from);
		bool isDestinationReached = false;
		bool isDestinationNotNull = _to != null;

		while (frontier.Count > 0 && isDestinationReached == false)
		{
			Tile current = frontier.Dequeue();
			for (int i = 0; i < 6; i++)
			{
				Tile neighbor = current.GetNeighbor((HexDirection)i);

				if (neighbor == null || neighbor.Distance != int.MaxValue)
				{
					continue;
				}

				//destination reached
				if (isDestinationNotNull && neighbor.coordinates.ID == _to.coordinates.ID)
					isDestinationReached = true;

				//max distance
				if (_maxDistance != -1 && current.Distance + 1 > _maxDistance)
				{
					continue;
				}

				//obstacle
				if (!_ignoreObstacles && (neighbor.IsObstacle(_isThisTurn) || (isDestinationNotNull && neighbor.GetEntity(_isThisTurn) != null && neighbor.coordinates.ID != _to.coordinates.ID)))
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

	public int GetDistanceBetween ( Tile _from, Tile _to, int _maxDistance, bool _isThisTurn = false )
	{
		//record last BFS done and avaid doing one if last one is valid
		if (m_lastBFSOriginTile == _from && m_lastBFSMaxDistance >= _maxDistance)
			return _to.Distance;

		BFS(_from, _maxDistance, _to: _to, _isThisTurn: _isThisTurn);

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
		List<Tile> tileInEntityRange = GetTilesInVisionRange(from, _entity.Data.NeuronalMembraneData.visionRange, true);

		foreach (Tile tile in tileInEntityRange)
		{
			tile.SetActiveFOW(false, true);
		}

		m_entitiesVisions[_entity.OwnerID].entitiesVisionRange.Add(_entity, tileInEntityRange);

		FogOfWarRenderer.Instance.MarkDirty();
	}

	public void OnEntityDeath ( Entity _entity )
	{
		int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
		if (!_entity.IsAlliedTo(playerId) || !m_entitiesVisions[_entity.OwnerID].entitiesVisionRange.ContainsKey(_entity))
			return;

		foreach (Tile tile in m_entitiesVisions[_entity.OwnerID].entitiesVisionRange[_entity])
		{
			bool isInAnotherEntityVisionRange = false;
			foreach (Entity otherEntities in m_entitiesVisions[_entity.OwnerID].entitiesVisionRange.Keys)
			{
				if (m_entitiesVisions[_entity.OwnerID].entitiesVisionRange[otherEntities].Contains(tile))
				{
					isInAnotherEntityVisionRange = true;
					break;
				}
			}

			if (!isInAnotherEntityVisionRange)
				tile.SetActiveFOW(false, false);
		}

		m_entitiesVisions[_entity.OwnerID].entitiesVisionRange.Remove(_entity);

		FogOfWarRenderer.Instance.MarkDirty();
	}

	public void OnEntityMovement ( Entity _entity )
	{
		int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
		if (!_entity.IsAlliedTo(playerId))
		{
			_entity.SetVisibility(_entity.Displacement.Coordinates.GetTile().IsVisible);
			return;
		}

		List<Tile> previousTilesInRangeList = new(m_entitiesVisions[_entity.OwnerID].entitiesVisionRange[_entity]);
		List<Tile> newTilesInRangeList = GetTilesInVisionRange(_entity.Displacement.Coordinates.GetTile(), _entity.Data.NeuronalMembraneData.visionRange, true);
		m_entitiesVisions[_entity.OwnerID].entitiesVisionRange[_entity] = new(newTilesInRangeList);

		foreach (Tile tile in newTilesInRangeList)
		{
			if (!previousTilesInRangeList.Contains(tile))
			{
				bool isInAnotherEntityVisionRange = false;
				foreach (Entity entity in m_entitiesVisions[_entity.OwnerID].entitiesVisionRange.Keys)
				{
					if (entity == _entity) continue;

					if (m_entitiesVisions[_entity.OwnerID].entitiesVisionRange[entity].Contains(tile))
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
			foreach (Entity entity in m_entitiesVisions[_entity.OwnerID].entitiesVisionRange.Keys)
			{
				if (m_entitiesVisions[_entity.OwnerID].entitiesVisionRange[entity].Contains(previousTile))
				{
					isInAnotherEntityVisionRange = true;
					break;
				}
			}

			if (!isInAnotherEntityVisionRange)
				previousTile.SetActiveFOW(true, false);
		}

		FogOfWarRenderer.Instance.MarkDirty();
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
	public int OffsetX => X + (Z - (Z & 1)) / 2;
	public int OffsetZ => Z;

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
		if (ID != -1)
			return GridManager.Instance.Tiles[ID];

		int ox = OffsetX;
		int oz = OffsetZ;

		int width = GridManager.Instance.GridData.width;
		int height = GridManager.Instance.GridData.height;

		if (ox < 0 || oz < 0 || ox >= width || oz >= height)
			return null;

		int index = ox + oz * width;
		return GridManager.Instance.Tiles[index];
	}

	public int DistanceTo ( TileCoordinates other )
	{
		return (Mathf.Abs(X - other.X)
			  + Mathf.Abs(Y - other.Y)
			  + Mathf.Abs(Z - other.Z)) / 2;
	}

	public override string ToString ()
	{
		return "(" +
			X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public Entity IsOccupied ( bool _isThisTurn )
	{
		return GetTile().GetEntity(_isThisTurn: _isThisTurn);
	}

}

public struct CubeF
{
	public float x, y, z;

	public CubeF ( float x, float y, float z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
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
	Void
}

public static class HexDirectionExtensions
{

	public static HexDirection Opposite ( this HexDirection direction )
	{
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}
}