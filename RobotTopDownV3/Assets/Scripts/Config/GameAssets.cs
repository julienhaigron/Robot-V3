using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GameAssets", menuName = "ScriptableObject/GameAssets")]
public class GameAssets : ScriptableObject
{
    public static GameAssets current => ApplicationManager.assets;


    public Game game;
    public UI ui;

    public SerializableDictionary<string, EntityEquipmentData> equipments = new SerializableDictionary<string, EntityEquipmentData>();

    [System.Serializable]
    public class Game
    {
        public Tile baseTile;
        public SerializableDictionary<Wall.WallType, GameObject> baseWallVisualPerType = new();

        public List<GridData> maps = new();
        public List<LevelData> levels = new();

        [Title("Entity")]
        public SerializableDictionary<string, Entity> entityPrefabPerFrameDictionary = new();
        public List<FrameEquipmentData> frames = new();

        public WeaponCone weaponCone;
        public SerializableDictionary<string, Weapon> weapons = new SerializableDictionary<string, Weapon>();
        public List<EntityEquipmentData> equipmentDatas = new();

        public SerializableDictionary<EntityActionEnumID, EntityActionData> entityActionsData = new SerializableDictionary<EntityActionEnumID, EntityActionData>();

        [Title("Pools")]
        public PoolData arrowPoolData;
    }

    [System.Serializable]
    public class UI
	{
        public LobbyDisplay baseLobbyDisplay;
        public EntityActionDisplay baseEntityActionDisplay;
        [Title("Flying Number")]
        public Material flyingDamageFontAsset;
        public Material flyingDamageCritFontAsset;
        public Sprite critIcon;

        [Title("Tile")]
        public SerializableDictionary<TileGroundType, Material> tileGroundMaterials = new();
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
