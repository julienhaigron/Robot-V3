using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class GridManager : MonoBehaviour
{
	private Tile[] m_tiles;
	public Tile[] Tiles => m_tiles;

	private int m_height;
	public int Height => m_height;
	private int m_width;
	public int Width => m_width;

	public void Awake ()
	{
		GenerateGrid(10, 10);
	}

	[Button("GenerateGrid")]
	public void GenerateGrid ( int _height, int _width )
	{
		m_tiles = new Tile[_height * _width];
		m_height = _height;
		m_width = _width;

		if (m_tiles != null)
		{
			foreach (Tile tile in m_tiles)
			{
				if (tile != null)
					DestroyImmediate(tile.gameObject);
			}
		}

		for (int z = 0, i = 0; z < _height; z++)
		{
			for (int x = 0; x < _width; x++)
			{
				Tile newTile = Instantiate(GameAssets.current.game.baseTile);
				m_tiles[i++] = newTile;

				Vector3 position;
				position.x = (x + z * 0.5f - z / 2) * (Tile.innerRadius * 2f);
				position.y = 0f;
				position.z = z * (Tile.outerRadius * 1.5f);

				newTile.transform.SetParent(transform, false);
				newTile.transform.localPosition = position;
				newTile.SetPosition(x, z);
				newTile.coordinates = TileCoordinates.FromOffsetCoordinates(x, z);
			}
		}
	}
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


	public void FindDistancesTo ( Tile _tile )
	{
		for (int i = 0; i < GameManager.Instance.Grid.Tiles.Length; i++)
		{
			GameManager.Instance.Grid.Tiles[i].Distance = _tile.coordinates.DistanceTo(GameManager.Instance.Grid.Tiles[i].coordinates);
		}
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
		return GetTile().entity;
	}

	/*public void FindPath ( Tile _fromTile, Tile _toTile )
	{
		StopAllCoroutines();
		StartCoroutine(Search(_fromTile, _toTile));
	}

	IEnumerator Search ( Tile _fromTile, Tile _toTile )
	{
		for (int i = 0; i < m_tiles.Length; i++)
		{
			m_tiles[i].Distance = int.MaxValue;
		}

		WaitForSeconds delay = new WaitForSeconds(1 / 60f);
		List<Tile> frontier = new List<Tile>();
		_fromTile.Distance = 0;
		frontier.Add(_fromTile);
	}*/
}