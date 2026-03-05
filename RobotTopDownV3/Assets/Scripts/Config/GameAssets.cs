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
        public GhostEntity baseGhost;
        public List<FrameEquipmentData> frames = new();

        public WeaponCone weaponCone;

        public SerializableDictionary<EntityActionEnumID, EntityActionData> entityActionsData = new SerializableDictionary<EntityActionEnumID, EntityActionData>();
        public SerializableDictionary<EntityPassiveEffectEnumID, AEntityPassiveEffect> entityEffects = new SerializableDictionary<EntityPassiveEffectEnumID, AEntityPassiveEffect>();
        public SerializableDictionary<EntityStatusEnumID, AEntityStatus> entityStatus = new SerializableDictionary<EntityStatusEnumID, AEntityStatus>();

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
        [Title("Actions")]
        public SerializableDictionary<Entity.EntityState, Material> entityStateMaterials = new();
        public SerializableDictionary<Entity.EntityState, Color> entityStateColors = new();
        public SerializableDictionary<Entity.EntityState, Material> ghostEntityStateMaterials = new();
    }

#if UNITY_EDITOR
    [Button]
	public void ReloadEquipments ()
	{
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EntityEquipmentData");
        List<EntityEquipmentData> fetchedEquipments = new();
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EntityEquipmentData>(path);
            if (asset != null)
                fetchedEquipments.Add(asset);
        }

        foreach (EntityEquipmentData eq in fetchedEquipments)
        {
            if (!equipments.ContainsKey(eq.name))
                equipments.Add(eq.name, eq);
        }
    }
#endif
}
