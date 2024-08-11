using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAnchor : MonoBehaviour
{
    private List<Entity> m_robots = new();
    public List<Entity> Robots => m_robots;

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
			SpawnRobot(robotData);
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

	private void SpawnRobot ( EntityData _robotData )
	{
		Entity robot = Instantiate(GameAssets.current.game.baseRobotEntity, transform);
		robot.Init(_robotData, GetRandomAvailableSpawnPosition());
		m_robots.Add(robot);
	}
}
