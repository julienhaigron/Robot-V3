using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObject/GameConfig")]
public partial class GameConfig : ScriptableObject
{
	public static GameConfig current => ApplicationManager.config;

	public GameSettings game = new GameSettings();
	public Input input = new Input();
	public Meta meta = new Meta();

	[System.Serializable]
	public partial class GameSettings
	{
		public float entityMovementSpeed = 1.5f;

	}

	[System.Serializable]
	public class Meta
	{
		public SerializableDictionary<LogConsole.LogEventType, Color> colorsPerType = new();
	}

	[System.Serializable]
	public class Input
	{
		public float interactionRayCastLength = 100f;
		public LayerMask interactionRayCastLayer;
		public LayerMask tileInternRayCastLayer;

	}
}
