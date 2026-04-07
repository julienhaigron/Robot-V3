using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "GameAssets", menuName = "ScriptableObject/GameAssets")]
public class GameAssets : ScriptableObject
{
    public static GameAssets current => ApplicationManager.assets;

    public Game game;
    public UI ui;

    public SerializableDictionary<CurrencyType, Currency> currencies = new SerializableDictionary<CurrencyType, Currency>();
    public SerializableDictionary<string, EntityEquipmentData> equipments = new SerializableDictionary<string, EntityEquipmentData>();
    public List<UpgradeAsset> upgrades = new List<UpgradeAsset>();

    [System.Serializable]
    public class Game
    {
        public Tile baseTile;
        public SerializableDictionary<Wall.WallType, GameObject> baseWallVisualPerType = new();

        public List<GridData> maps = new();
        public List<LevelData> levels = new();
        public SerializableDictionary<StructureUpgradePopup.StructureType, StructureUpgrade> structureUpgrades = new();

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
        public ComponentDisplay baseComponentDisplay;
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

        [Title("Icons")]
        public SerializableDictionary<EntityEquipmentData.EquipmentType, Sprite> componentIcons = new();
        public SerializableDictionary<EntityEquipmentData.EntityFaction, Sprite> corporationsIcons = new();
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

#if UNITY_EDITOR
    [Button]
    public void ReloadActions ()
    {
        string[] guids = AssetDatabase.FindAssets("t:EntityActionData");
        List<EntityActionData> fetchedActions = new();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<EntityActionData>(path);
            if (asset != null)
                fetchedActions.Add(asset);
        }

        foreach (EntityActionData action in fetchedActions)
        {
            if (Enum.TryParse(action.name, out EntityActionEnumID _result))
            {
                action.enumID = _result;
                if (!game.entityActionsData.ContainsKey(action.enumID))
                {
                    game.entityActionsData.Add(action.enumID, action);
                }
                else
                {
                    game.entityActionsData[action.enumID] = action;
                }
                EditorUtility.SetDirty(action);
            }
        }

        EditorUtility.SetDirty(current);
    }
#endif
}
