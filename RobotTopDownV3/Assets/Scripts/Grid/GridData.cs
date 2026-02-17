using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(fileName = "GridData", menuName = "ScriptableObject/GridData", order = 1)]
public class GridData : ScriptableObject
{
	public int width;
	public int height;
	public TileData[] tiles;

	[System.Serializable]
	public class TileData
	{
		public TileGroundType groundType;

		public Wall.WallType wallType;
		public int orientation; //between 0-5

		public TileData(TileGroundType _groundType, Wall.WallType _wallType, int _orientation )
		{
			groundType = _groundType;
			wallType = _wallType;
			orientation = _orientation;
		}
	}

	//[Button]
	public void ResizeTileList ()
	{
		if(tiles.Length < width * height)
		{
			List<TileData> list = tiles.ToList();
			for(int i = tiles.Length; i < width * height; i++)
			{
				list.Add(new TileData(TileGroundType.Empty, Wall.WallType.VerticalStrait, 0));
			}
			tiles = list.ToArray();
		}
		else if (tiles.Length > width * height)
		{
			List<TileData> list = tiles.ToList();
			for (int i = tiles.Length; i > width * height; i--)
			{
				list.RemoveAt(list.Count - 1);
			}
			tiles = list.ToArray();
		}
	}
}
