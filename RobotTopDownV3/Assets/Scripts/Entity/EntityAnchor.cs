using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnchor : MonoBehaviour
{
	public static System.Action<Entity> onEntityAdded;

	private List<Entity> m_entities = new();
    public List<Entity> Entities => m_entities;
	private readonly Dictionary<int, Entity> m_entitiesByID = new();
	private int m_issuedEntityIDCount;
	private Entity m_king;
    public Entity King => m_king;
	public bool IsKingDead => m_king != null && m_king.Equipment.IsDead;

	[SerializeField] private List<Spawn> m_staticSpawnCoordinates = new();
	[SerializeField] private List<Spawn> m_dynamicSpawnCoordinates = new();
	private List<Spawn> m_allSpawnCoordinates = new();
	public List<Spawn> AllSpawns => m_allSpawnCoordinates;
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
		m_allSpawnCoordinates.Add(newSpawn);
		if(_isStatic)
			m_staticSpawnCoordinates.Add(newSpawn);
		else
			m_dynamicSpawnCoordinates.Add(newSpawn);
	}

	public void Clear ()
	{
		m_staticSpawnCoordinates.Clear();
		m_dynamicSpawnCoordinates.Clear();
		m_allSpawnCoordinates.Clear();
		m_zones.Clear();
		m_structures.Clear();
		m_king = null;
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

	public bool TryGetFreeSpawn ( out Spawn _spawn )
	{
		List<Spawn> freeSpawns = m_allSpawnCoordinates.FindAll(spawn => spawn.coordinates.IsOccupied(true) == null);
		_spawn = freeSpawns.Count > 0 ? freeSpawns[Random.Range(0, freeSpawns.Count)] : default;
		return freeSpawns.Count > 0;
	}

	public Entity SpawnEntity ( EntitySavedData _entityData, int _playerID)
	{
		Spawn spawn = GetRandomAvailableSpawnPosition();
		if (spawn.initializationState == Spawn.InitializationState.Failure)
			Debug.LogWarning("No free spawn tile left for " + _entityData.name + " (player " + _playerID + "): it spawns on tile 0.", this);

		return SpawnEntity(_entityData, _playerID, spawn);
	}

	public int ReserveEntityIDs ( int _playerID, int _count = 1 )
	{
		m_issuedEntityIDCount = Mathf.Max(m_issuedEntityIDCount, m_entities.Count);
		int firstID = (100 * _playerID) + m_issuedEntityIDCount; //0 - 99 id slots for units per player
		m_issuedEntityIDCount += _count;
		return firstID;
	}

	public Entity SpawnEntity ( EntitySavedData _entityData, int _playerID, Spawn _spawn )
	{
		int entityID = ReserveEntityIDs(_playerID);
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		Register(entity, entityID);
		entity.Init(_entityData, _spawn, entityID, _playerID);
		onEntityAdded?.Invoke(entity);
		return entity;
	}

	private void Register ( Entity _entity, int _entityID )
	{
		m_entities.Add(_entity);
		m_entitiesByID[_entityID] = _entity;
	}

	public bool TryGetEntity ( int _entityID, out Entity _entity )
	{
		return m_entitiesByID.TryGetValue(_entityID, out _entity);
	}

	public void DestroyEntities ()
	{
		foreach (Entity entity in m_entities)
		{
			if (entity != null)
				Destroy(entity.gameObject);
		}

		m_entities.Clear();
		m_entitiesByID.Clear();
	}

	public void DesignateRandomKing ()
	{
		List<Entity> candidates = new();
		foreach (Entity entity in m_entities)
		{
			if (!entity.Equipment.IsDead && !entity.IsImmortal)
				candidates.Add(entity);
		}

		if (m_king != null)
			m_king.UI.SetKingMarker(false);

		m_king = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;

		if (m_king != null)
			m_king.UI.SetKingMarker(true);
	}

	public void SpawnEntityDuringPlay ( EntitySavedData _entityData, int _entityID, int _playerID, int _tileID, System.Action _onEndSpawn = null )
	{
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		Register(entity, _entityID);
		entity.Init(_entityData, new Spawn(GridManager.Instance.Tiles[_tileID].coordinates, Spawn.InitializationState.Success, true, true), _entityID, _playerID);
		onEntityAdded?.Invoke(entity);

		TurnManager.Instance.AddEntityMidGame(entity, _onEndSpawn);
	}
}
