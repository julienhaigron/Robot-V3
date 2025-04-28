using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class NetworkedTurnSystem : MonoBehaviour
{
	public static Dictionary<int, Entity> Entities = new();

	#region Fncts

	public void AddEntity(Entity _entity )
	{
		Entities.Add(_entity.ID, _entity);
	}

	#endregion

	#region Converters

	#region Tile
	public static int ConvertTileToInt (Tile _tile)
	{
		int tile = _tile.coordinates.ID;

		return tile;
	}

	public static Tile ConvertIntToTile (int _int)
	{
		Tile tile = GridManager.Instance.Tiles[_int];

		return tile;
	}

	#endregion

	#region Entity

	public static int ConvertEntityToInt ( Entity _entity )
	{
		int entity = _entity.ID;

		return entity;
	}

	public static Entity ConvertIntToEntity ( int _int )
	{
		Entity entity = Entities[_int];

		return entity;
	}

	#endregion

	#endregion
}
