using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnchor : MonoBehaviour
{
	public static System.Action<Entity> onEntityAdded;

	private List<Entity> m_entities = new();
    public List<Entity> Entities => m_entities;
	private readonly Dictionary<int, Entity> m_entitiesByID = new();
	private readonly List<Entity> m_pendingInvocations = new();
	private readonly Stack<int> m_releasedEntityIDs = new();
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
		if (_count == 1 && m_releasedEntityIDs.Count > 0)
			return m_releasedEntityIDs.Pop();

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

		foreach (Entity entity in m_pendingInvocations)
		{
			if (entity != null)
				Destroy(entity.gameObject);
		}

		m_entities.Clear();
		m_entitiesByID.Clear();
		m_pendingInvocations.Clear();
		m_releasedEntityIDs.Clear();
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

	public Entity SpawnInvocation ( EntitySavedData _entityData, int _entityID, Entity _invoker, string _componentID, int _tileID, int _pendingUntilTick = -1 )
	{
		bool isPending = _pendingUntilTick >= 0;
		Entity entity = CreateOnTile(_entityData, _entityID, _invoker.OwnerID, _tileID, isPending);
		entity.BecomeInvocationOf(_invoker, _componentID, isPending);
		onEntityAdded?.Invoke(entity);

		if (isPending)
			TurnManager.Instance.ReservePendingTicks(entity, _pendingUntilTick);
		else
			TurnManager.Instance.AddEntityMidGame(entity);

		return entity;
	}

	public void MaterializeInvocation ( Entity _entity )
	{
		m_pendingInvocations.Remove(_entity);
		m_entities.Add(_entity);
		_entity.Materialize();
	}

	public void DiscardPendingInvocation ( Entity _entity )
	{
		m_pendingInvocations.Remove(_entity);
		TurnManager.Instance.DiscardEntity(_entity.ID);
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
		{
			_entity.Equipment.Dismiss();
			return;
		}

		if (PlayerController.Instance.SelectedEntity == _entity)
			PlayerController.Instance.SelectEntity(null);
		m_entitiesByID.Remove(_entity.ID);
		m_releasedEntityIDs.Push(_entity.ID);
		Destroy(_entity.gameObject);
	}

	public void DiscardPendingInvocations ()
	{
		foreach (Entity pending in m_pendingInvocations.ToArray())
			DiscardPendingInvocation(pending);
	}

	public bool TryGetPendingInvocationOn ( Tile _tile, out Entity _entity )
	{
		_entity = _tile == null ? null : m_pendingInvocations.Find(pending => pending != null && !pending.Equipment.IsDead && pending.Displacement.Coordinates.ID == _tile.coordinates.ID);
		return _entity != null;
	}

	public void SpawnEntityDuringPlay ( EntitySavedData _entityData, int _entityID, int _playerID, int _tileID, System.Action _onEndSpawn = null )
	{
		Entity entity = CreateOnTile(_entityData, _entityID, _playerID, _tileID, false);
		onEntityAdded?.Invoke(entity);
		TurnManager.Instance.AddEntityMidGame(entity, _onEndSpawn);
	}

	private Entity CreateOnTile ( EntitySavedData _entityData, int _entityID, int _playerID, int _tileID, bool _isPending )
	{
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		if (_isPending)
		{
			m_entitiesByID[_entityID] = entity;
			m_pendingInvocations.Add(entity);
		}
		else
			Register(entity, _entityID);

		entity.Init(_entityData, new Spawn(GridManager.Instance.Tiles[_tileID].coordinates, Spawn.InitializationState.Success, true, true), _entityID, _playerID, !_isPending);
		return entity;
	}
}
