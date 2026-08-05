using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnchor : MonoBehaviour
{
	public static System.Action<Entity> onEntityAdded;

	private List<Entity> m_entities = new();
    public List<Entity> Entities => m_entities;
	private Entity m_king;
    public Entity King => m_king;

	[SerializeField] private List<Spawn> m_staticSpawnCoordinates = new();
	[SerializeField] private List<Spawn> m_dynamicSpawnCoordinates = new(); 
	private HashSet<Tile> m_zones = new();
	public HashSet<Tile> Zones => m_zones;
	private HashSet<Tile> m_structures = new();
	public HashSet<Tile> Structures => m_structures;

	[System.Serializable]
	public struct Spawn
	{
		public enum InitializationState { Success, Failure}

		public Spawn(TileCoordinates _coordinates, bool _isFirstSide, bool _isStatic )
		{
			isFirstSide = _isFirstSide;
			coordinates = _coordinates;
			isStatic = _isStatic;
			initializationState = InitializationState.Success;
		}

		public Spawn(TileCoordinates _coordinates, InitializationState _state, bool _isFirstSide, bool _isStatic )
		{
			isFirstSide = _isFirstSide;
			coordinates = _coordinates;
			isStatic = _isStatic;
			initializationState = _state;
		}

		public bool isFirstSide;
		public TileCoordinates coordinates;
		public bool isStatic;
		public InitializationState initializationState;
	}

	public void AddSpawn( TileCoordinates _coordinates, bool _isFirstSide, bool _isStatic )
	{
		Spawn newSpawn = new Spawn(_coordinates, _isFirstSide, _isStatic);
		if(_isStatic)
			m_staticSpawnCoordinates.Add(newSpawn);
		else
			m_dynamicSpawnCoordinates.Add(newSpawn);
	}

	public void Clear ()
	{
		m_staticSpawnCoordinates.Clear();
		m_dynamicSpawnCoordinates.Clear();
		m_zones.Clear();
		m_structures.Clear();
	}

	public void Init (List<EntitySavedData> _robots, int _playerID)
	{
		foreach(EntitySavedData robotData in _robots)
		{
			SpawnEntity(robotData, _playerID);
		}
	}

	public void AddStructure ( Tile _tile )
	{
		m_structures.Add(_tile);
	}

	public void AddZone ( Tile _tile )
	{
		m_zones.Add(_tile);
	}

	private Spawn GetRandomAvailableSpawnPosition ()
	{
		foreach(Spawn spawn in m_staticSpawnCoordinates)
		{
			if (spawn.coordinates.IsOccupied(true) == null)
				return spawn;
		}

		if(m_dynamicSpawnCoordinates != null && m_dynamicSpawnCoordinates.Count > 0)
		{
			Spawn randomSpawn = m_dynamicSpawnCoordinates.RandomElement();
			m_dynamicSpawnCoordinates.Remove(randomSpawn);
			return randomSpawn;
		}

		return new Spawn(new TileCoordinates(0, 0, 0), Spawn.InitializationState.Failure, true, false);
	}

	public void SpawnEntity ( EntitySavedData _entityData, int _playerID)
	{
		int entityID = (100 * _playerID) + m_entities.Count; //0 - 99 id slots for units per player
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		m_entities.Add(entity);
		entity.Init(_entityData, GetRandomAvailableSpawnPosition(), entityID, _playerID);
		onEntityAdded?.Invoke(entity);
	}

	private void RegisterEntityAsKing(Entity _kingEntity )
	{
		m_king = _kingEntity;
	}

	public void SpawnEntityDuringPlay ( EntitySavedData _entityData, int _entityID, int _playerID, int _tileID, System.Action _onEndSpawn = null )
	{
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		m_entities.Add(entity);
		entity.Init(_entityData, new Spawn(GridManager.Instance.Tiles[_tileID].coordinates, Spawn.InitializationState.Success, true, true), _entityID, _playerID);
		onEntityAdded?.Invoke(entity);

		TurnManager.Instance.AddEntityMidGame(entity, _onEndSpawn);
	}
}
