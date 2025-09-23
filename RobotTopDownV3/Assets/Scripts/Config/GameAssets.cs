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
    public UI ui;

    public Dictionary<string, EntityEquipmentData> equipments = new Dictionary<string, EntityEquipmentData>();

    [System.Serializable]
    public class Game
    {
        public Tile baseTile;

        public List<GridData> maps = new();
        public List<Entity> entityPrefabs = new();

        public List<LevelData> levels = new();

        public WeaponData defaultWeapon;
        public SerializableDictionary<string, Weapon> weapons = new SerializableDictionary<string, Weapon>();
        public List<EntityEquipmentData> equipmentDatas = new();

        public SerializableDictionary<EntityActionType, EntityActionData> entityActionsData = new SerializableDictionary<EntityActionType, EntityActionData>();

        [Title("Pools")]
        public PoolData arrowPoolData;
    }

    [System.Serializable]
    public class Material
	{
        [Title("Tile")]
        public SerializableDictionary<TileGroundType, UnityEngine.Material> tileGroundMaterials = new();
	}

    [System.Serializable]
    public class UI
	{
        public LobbyDisplay baseLobbyDisplay;
    }


	public void Initialize ()
	{
        equipments.Clear();
        foreach (EntityEquipmentData item in game.equipmentDatas)
        {
            equipments.Add(item.ID, item);
        }

    }
}
