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

        //public List<GridData> maps = new();
        public SerializableDictionary<MissionDataEnumID, MissionData> missions = new();
        public SerializableDictionary<StructureUpgradePopup.StructureType, StructureUpgrade> structureUpgrades = new();
        public ShopStructureUpgrade ShopStructureUpgrade => structureUpgrades[StructureUpgradePopup.StructureType.Shop] as ShopStructureUpgrade;
        public HangarStructureUpgrade HangarStructureUpgrade => structureUpgrades[StructureUpgradePopup.StructureType.Hangar] as HangarStructureUpgrade;
        public RecyclerStructureUpgrade RecyclerStructureUpgrade => structureUpgrades[StructureUpgradePopup.StructureType.Recycler] as RecyclerStructureUpgrade;
        public RepairStationStructureUpgrade RepairStationStructureUpgrade => structureUpgrades[StructureUpgradePopup.StructureType.RepairStation] as RepairStationStructureUpgrade;

        [Title("Tournament")]
        public MissionData[] tournamentMissionsPool;

        [Title("Entity")]
        public Entity defaultEntity;
        public GhostEntity baseGhost;
        public GhostItem baseItem;
        public List<Entity.EntityState> states;

        public WeaponCone weaponCone;

        public SerializableDictionary<EntityActionEnumID, EntityActionData> entityActionsData = new SerializableDictionary<EntityActionEnumID, EntityActionData>();
        public SerializableDictionary<EntityPassiveEffectEnumID, AEntityPassiveEffect> entityEffects = new SerializableDictionary<EntityPassiveEffectEnumID, AEntityPassiveEffect>();
        public SerializableDictionary<EntityStatusEnumID, AEntityStatus> entityStatus = new SerializableDictionary<EntityStatusEnumID, AEntityStatus>();

        [Title("Pools")]
        public PoolData arrowPoolData;
        public PoolData rotationHandlePoolData;
    }

    [System.Serializable]
    public class UI
	{
        public Sprite baseEquipmentSprite;
        public ComponentDisplay baseComponentDisplay;
        public ComponentDisplay shopComponentDisplay;
        public RepareUnitDisplay repareUnitDisplay;
        public LobbyDisplay baseLobbyDisplay;
        public EntityActionDisplay baseEntityActionDisplay;
        [Title("Flying Number")]
        public Material flyingDamageFontAsset;
        public Material flyingDamageCritFontAsset;
        public Sprite critIcon;
        public EntityStatusDisplay statusDisplayPrefab;
        public SerializableDictionary<WeaponEquipmentData.DamageType, Sprite> damageIconPerType = new();

        [Title("Tile")]
        public SerializableDictionary<TileGroundType, Material> tileGroundMaterials = new();
        [Title("Actions")]
        public SerializableDictionary<Entity.EntityState, Material> entityStateMaterials = new();
        public SerializableDictionary<Entity.EntityState, Color> entityStateColors = new();
        public SerializableDictionary<Entity.EntityState, Material> ghostEntityStateMaterials = new();

        [Title("Icons")]
        public SerializableDictionary<EntityEquipmentData.EquipmentType, Sprite> componentIcons = new();
        public SerializableDictionary<EntityEquipmentData.EntityFaction, Sprite> corporationsIcons = new();
        public SerializableDictionary<EntityActionData.MainActionType, Sprite> mainActionTypeIcons = new();
        public Sprite defaultStatSprite;
        public SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, Sprite> statsIcons = new();
        
        [Title("Colors")]
        public SerializableDictionary<EntityEquipmentData.EntityFaction, Color> corporationsColors = new();
        public SerializableDictionary<EntityEquipmentData.EquipmentType, Color> componentColors = new();

        [Title("Missing Target Warning")]
        public Sprite missingTargetIcon;
        public Color missingTargetColor = new(1f, .15f, .15f);

        [Title("Action Range Colors")]
        public Color movementRangeColor = Color.green;
        public Color movementRangePreviewColor = new(.6f, 1f, .6f);
        public Color actionRangeColor = new(0f, .375f, 1f);
        public Color actionRangePreviewColor = new(.55f, .78f, 1f);
        public Color aoePreviewColor = new(1f, .5f, 0f);

        public Color GetActionRangeColor ( EntityActionData.MainActionType _mainType, bool _isPreview )
		{
            if (_mainType == EntityActionData.MainActionType.Movement)
                return _isPreview ? movementRangePreviewColor : movementRangeColor;

            return _isPreview ? actionRangePreviewColor : actionRangeColor;
		}
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

    [Button]
    public void ReloadEffects ()
    {
        string[] guids = AssetDatabase.FindAssets("t:AEntityPassiveEffect");
        List<AEntityPassiveEffect> fetchedActions = new();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<AEntityPassiveEffect>(path);
            if (asset != null)
                fetchedActions.Add(asset);
        }

        foreach (AEntityPassiveEffect action in fetchedActions)
        {
            if (Enum.TryParse(action.name, out EntityPassiveEffectEnumID _result))
            {
                action.enumID = _result;
                if (!game.entityEffects.ContainsKey(action.enumID))
                {
                    game.entityEffects.Add(action.enumID, action);
                }
                else
                {
                    game.entityEffects[action.enumID] = action;
                }
                EditorUtility.SetDirty(action);
            }
        }

        EditorUtility.SetDirty(current);
    }

    [Button]
    public void ReloadStatus ()
    {
        string[] guids = AssetDatabase.FindAssets("t:AEntityStatus");
        List<AEntityStatus> fetchedActions = new();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<AEntityStatus>(path);
            if (asset != null)
                fetchedActions.Add(asset);
        }

        foreach (AEntityStatus action in fetchedActions)
        {
            if (Enum.TryParse(action.name, out EntityStatusEnumID _result))
            {
                action.enumID = _result;
                if (!game.entityStatus.ContainsKey(action.enumID))
                {
                    game.entityStatus.Add(action.enumID, action);
                }
                else
                {
                    game.entityStatus[action.enumID] = action;
                }
                EditorUtility.SetDirty(action);
            }
        }

        EditorUtility.SetDirty(current);
    }

    [Button]
    public void LoadStatTypes ()
    {
        current.ui.statsIcons.Clear();
        for (int i = 0; i < (int)EntityEquipmentData.SecondaryStat.StatType.OccultorSlot; i++)
		{
            current.ui.statsIcons.Add((EntityEquipmentData.SecondaryStat.StatType)i, current.ui.defaultStatSprite);
		}

        EditorUtility.SetDirty(current);
    }

    [Button]
    public void ReloadAll ()
	{
        ReloadEquipments();
        ReloadActions();
        ReloadEffects();
        ReloadStatus();
    }
#endif
}
