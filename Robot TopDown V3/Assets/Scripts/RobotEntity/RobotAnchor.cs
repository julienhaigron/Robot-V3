using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotAnchor : MonoBehaviour
{
    private List<RobotEntity> m_robots = new();
    public List<RobotEntity> Robots => m_robots;

	public RobotEntity SelectedRobot;

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

	private void Start ()
	{
		SpawnRobot();
	}

	private Spawn GetRandomAvailableSpawnPosition ()
	{
		//TODO : make it random
		foreach(Spawn spawn in m_spawnCoordinates)
		{
			if (spawn.coordinates.IsOccupied() == null)
				return spawn;
		}

		return new Spawn(new TileCoordinates(0, 0), Spawn.InitializationState.Failure);
	}

	private void SpawnRobot ()
	{
		RobotEntity robot = Instantiate(GameAssets.current.game.baseRobotEntity, transform);
		m_robots.Add(robot);

		robot.Displacement.Init(GetRandomAvailableSpawnPosition());
	}
}
