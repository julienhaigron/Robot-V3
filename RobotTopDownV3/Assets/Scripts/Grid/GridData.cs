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
	[Tooltip("Les unités apparaissent tournées vers le spawn adverse. Décoché : premier camp vers l'est, second camp vers l'ouest.")]
	public bool unitsFaceEnemySpawn = true;

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
}
