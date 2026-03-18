using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnchor : MonoBehaviour
{
	public static System.Action<Entity> onEntityAdded;

	private List<Entity> m_entities = new();
    public List<Entity> Entities => m_entities;

	[SerializeField] private List<Spawn> m_spawnCoordinates = new();

	[System.Serializable]
	public struct Spawn
	{
		public enum InitializationState { Success, Failure}

		public Spawn(TileCoordinates _coordinates, bool _isFirstSide )
		{
			isFirstSide = _isFirstSide;
			coordinates = _coordinates;
			initializationState = InitializationState.Success;
		}

		public Spawn(TileCoordinates _coordinates, InitializationState _state, bool _isFirstSide )
		{
			isFirstSide = _isFirstSide;
			coordinates = _coordinates;
			initializationState = _state;
		}

		public bool isFirstSide;
		public TileCoordinates coordinates;
		public InitializationState initializationState;
	}

	public void AddSpawn( TileCoordinates _coordinates, bool _isFirstSide )
	{
		Spawn newSpawn = new Spawn(_coordinates, _isFirstSide);
		m_spawnCoordinates.Add(newSpawn);
	}

	public void Init (List<EntitySavedData> _robots, int _playerID)
	{
		int unitCount = 0;
		foreach(EntitySavedData robotData in _robots)
		{
			SpawnEntity(robotData, (100 * _playerID) + unitCount++, _playerID); //0 - 99 id slots for units per player
		}
	}

	private Spawn GetRandomAvailableSpawnPosition ()
	{
		//TODO : make it random
		foreach(Spawn spawn in m_spawnCoordinates)
		{
			if (spawn.coordinates.IsOccupied(true) == null)
				return spawn;
		}

		return new Spawn(new TileCoordinates(0, 0, 0), Spawn.InitializationState.Failure, true);
	}

	private void SpawnEntity ( EntitySavedData _entityData, int _entityID, int _playerID)
	{
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		m_entities.Add(entity);
		entity.Init(_entityData, GetRandomAvailableSpawnPosition(), _entityID, _playerID);
		onEntityAdded?.Invoke(entity);
	}

	public void SpawnEntityDuringPlay ( EntitySavedData _entityData, int _entityID, int _playerID, int _tileID, System.Action _onEndSpawn = null )
	{
		Entity entity = Instantiate(_entityData.FrameData.prefab, transform);
		m_entities.Add(entity);
		entity.Init(_entityData, new Spawn(GridManager.Instance.Tiles[_tileID].coordinates, Spawn.InitializationState.Success, true), _entityID, _playerID);
		onEntityAdded?.Invoke(entity);

		TurnManager.Instance.AddEntityMidGame(entity, _onEndSpawn);
	}
}
