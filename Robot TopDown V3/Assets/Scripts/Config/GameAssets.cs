using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GameAssets", menuName = "ScriptableObject/GameAssets")]
public class GameAssets : ScriptableObject
{
    public static GameAssets current => ApplicationManager.assets;


    public Game game;
    public Material material;

    [System.Serializable]
    public class Game
    {
        public Tile baseTile;
        public RobotEntity baseRobotEntity;

        public List<GridData> maps = new();

    }

    [System.Serializable]
    public class Material
	{
        [Title("Tile")]
        public SerializableDictionary<TileGroundType, UnityEngine.Material> tileGroundMaterials = new();
	}

}
