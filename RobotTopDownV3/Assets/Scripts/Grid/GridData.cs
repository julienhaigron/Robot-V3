using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		public TileData(TileGroundType _groundType )
		{
			groundType = _groundType;
		}
	}

}
