using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnchor : MonoBehaviour
{
    private List<Entity> m_entities = new();
    public List<Entity> Entities => m_entities;

	[SerializeField] private List<Spawn> m_spawnCoordinates = new();

	[System.Serializable]
	public struct Spawn
	{
		public enum InitializationState { Success, Failure}

		public Spawn(TileCoordinates _coordinates)
		{
			coordinates = _coordinates;
			initializationState = InitializationState.Success;
		}

		public Spawn(TileCoordinates _coordinates, InitializationState _state )
		{
			coordinates = _coordinates;
			initializationState = _state;
		}

		public TileCoordinates coordinates;
		public InitializationState initializationState;
	}

	public void AddSpawn( TileCoordinates _coordinates )
	{
		Spawn newSpawn = new Spawn(_coordinates);
		m_spawnCoordinates.Add(newSpawn);
	}

	public void Init (List<EntityData> _robots)
	{
		foreach(EntityData robotData in _robots)
		{
			SpawnEntity(robotData);
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

		return new Spawn(new TileCoordinates(0, 0, 0), Spawn.InitializationState.Failure);
	}

	private void SpawnEntity ( EntityData _entityData )
	{
		Entity entity = Instantiate(GameAssets.current.game.baseEntity, transform);
		entity.Init(_entityData, GetRandomAvailableSpawnPosition());
		m_entities.Add(entity);
	}
}
