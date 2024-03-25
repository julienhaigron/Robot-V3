using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class GridManager : MonoBehaviour
{
	public Action<Tile> onTileSelected;
	public Action<Tile> onTileHovered;

	private Tile[] m_tiles;
	public Tile[] Tiles => m_tiles;

	private int m_height;
	public int Height => m_height;
	private int m_width;
	public int Width => m_width;

	private Coroutine m_searchCoroutine;
	private Tile m_selectedTile;
	private Tile m_hoveredTile;

	public void Awake ()
	{
		GenerateGrid(10, 10);
		onTileSelected += OnTileSelected;
		onTileHovered += OnTileHovered;
	}

	/*public void LoadGrid(GridData _data )
	{

	}*/

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
				CreateTile(x, z ,i++);				
			}
		}
	}

	private void CreateTile(int _x, int _z, int _i )
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
		newTile.coordinates = TileCoordinates.FromOffsetCoordinates(_x, _z);

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

	public void FindDistancesTo ( Tile _tile )
	{
		if (m_searchCoroutine != null)
			StopCoroutine(m_searchCoroutine);

		m_searchCoroutine = StartCoroutine(Search(_tile));
	}

	private IEnumerator Search ( Tile cell )
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
		}

		Queue<Tile> frontier = new Queue<Tile>();
		cell.Distance = 0;
		frontier.Enqueue(cell);

		while (frontier.Count > 0)
		{
			Tile current = frontier.Dequeue();
			for(int i = 0; i < 6; i++)
			{
				yield return new WaitForSeconds(1 / 60f);
				Tile neighbor = current.GetNeighbor((HexDirection)i);
				if (neighbor == null || neighbor.Distance != int.MaxValue)
				{
					continue;
				}

				//obstacle
				if (neighbor.IsObstacle())
				{
					continue;
				}

				neighbor.Distance = current.Distance + 1;
				frontier.Enqueue(neighbor);
				
			}
		}
	}

	#region Callbacks

	private void OnTileSelected ( Tile _tile )
	{
		if(m_selectedTile != _tile)
		{
			m_selectedTile = _tile;
			FindDistancesTo(m_selectedTile);
		}

		/*if (m_selectedTile == null)
		{
			m_selectedTile = _tile;

			//display available cell in reach
			m_selectedTile.UI.EnableOutline(Color.blue);
		}
		else if (m_selectedTile == _tile)
		{
			//deactivate reachable tile display
			m_selectedTile.UI.EnableOutline(Color.black);
			m_selectedTile = null;
		}
		else
		{
			//move to
			//FindDistancesTo(currentCell);
		}*/
	}

	private void OnTileHovered ( Tile _tile )
	{
		if (m_selectedTile == null)
			return;

		if (_tile != m_selectedTile && _tile != m_hoveredTile)
		{
			m_hoveredTile = _tile;
			//FindDistancesTo(m_selectedTile);
		}
	}

	#endregion

}

[System.Serializable]
public struct TileCoordinates
{

	[SerializeField] private int m_x;
	[SerializeField] private int m_z;

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

	public TileCoordinates ( int x, int z )
	{
		m_x = x;
		m_z = z;
	}

	public static TileCoordinates FromOffsetCoordinates ( int x, int z )
	{
		return new TileCoordinates(x - z / 2, z);
	}

	public Tile GetTile ()
	{
		return GameManager.Instance.Grid.Tiles[Z * GameManager.Instance.Grid.Width + X];
	}

	public int DistanceTo ( TileCoordinates other )
	{
		return
		((m_x < other.m_x ? other.m_x - m_x : m_x - other.m_x) +
		(Y < other.Y ? other.Y - Y : Y - other.Y) +
		(m_z < other.m_z ? other.m_z - m_z : m_z - other.m_z)) / 2;
	}

	public override string ToString ()
	{
		return "(" +
			X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public string ToStringOnSeparateLines ()
	{
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}

	public static TileCoordinates FromPosition ( Vector3 position )
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
	}

	public RobotEntity IsOccupied ()
	{
		return GetTile().Entity;
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
	Door
}

public static class HexDirectionExtensions
{

	public static HexDirection Opposite ( this HexDirection direction )
	{
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}
}