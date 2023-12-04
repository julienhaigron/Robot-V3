using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObject/GameConfig")]
public partial class GameConfig : ScriptableObject
{
    public static GameConfig current => ApplicationManager.config;

	public GameSettings game = new GameSettings();

	[System.Serializable]
    public partial class GameSettings
    {
        public Input input = new Input();

        [System.Serializable]
        public class Input
		{
            public float interactionRayCastLength = 100f;
            public LayerMask interactionRayCastLayer;

		}
    }
}
