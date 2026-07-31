using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObject/GameConfig")]
public partial class GameConfig : ScriptableObject
{
	public static GameConfig current => ApplicationManager.config;

	public DebugSettings debug = new DebugSettings();
	public GameSettings game = new GameSettings();
	public Input input = new Input();
	public Meta meta = new Meta();
	public UI ui = new UI();
	public DatasConfigs datas = new DatasConfigs();
	public Parsing parsing = new Parsing();

	[System.Serializable]
	public class DebugSettings
	{
		public bool saveEntityDeathAndDamages = true;
		public bool skipFTUE = false;
	}

	[System.Serializable]
	public partial class GameSettings
	{
		[Title("Macro")]
		public int nbOfDayInCycle = 7;
		public int maxInventoryCapacity = 80;

		[Title("Actions")]
		public EntityActionData defaultStartAction;
		public float actionDuration = 1f;
		public float entityRotationDuration = .5f;
		public int actionTokenPerRound = 10;

		[Title("Camera")]
		public float cameraMovementSpeed = 15f;
		public float cameraRotationDuration = .5f;
		public float cameraRotationStep = 90f;
		public Vector2 cameraMovementBoundsOffset = new Vector2(7f, 1.75f);
		public float cameraZoomSpeed;
		public Vector2 cameraZoomBounds;

		[Title("Entity")]
		public SerializableDictionary<Tile.TileDirectionType, float> entityFlankRatio = new();
		public float entityMovementEvasionBonus = 2;
		public float entityCoverBonus = 2;
		//public int SerializableDictionary<EntityActionData., > maxSlotAmountPerType
		public SerializableDictionary<NeuronalMembraneEquipmentData.VisionTypes, int> rangePerVisionType;
		public SerializableDictionary<WeaponEquipmentData.DistanceType, float> distanceTypeSpreadEvaluation;
		public SerializableDictionary<WeaponEquipmentData.DamageType, WeaponEquipmentData.DamageCategory> damageCategoryPerDamageType;
		public SerializableDictionary<WeaponEquipmentData.DamageCategory, EntityEquipmentData.SecondaryStat.StatType> statTypePerDamageCategory;
		public SerializableDictionary<WeaponEquipmentData.DamageCategory, EntityEquipmentData.SecondaryStat.StatType> statTypePerResistanceCategory;
		public SerializableDictionary<WeaponEquipmentData.DamageType, EntityEquipmentData.SecondaryStat.StatType> statTypePerDamageType;
		public SerializableDictionary<WeaponEquipmentData.DamageType, EntityEquipmentData.SecondaryStat.StatType> statTypePerDamageResistanceType;

		[Title("Hub")]
		public string hubSceneName;
		public string startScreenSceneName;
		public int missionAmountInMissionSelectionPanel = 12;
		public int selectableMissionAmount = 8;

	}

	[System.Serializable]
	public class Meta
	{
		public SerializableDictionary<LogConsole.LogEventType, Color> colorsPerType = new();
		public SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.SecondaryStat.StatTypeFormat> formatPerStartTypeDictionary = new();
	}
	
	[System.Serializable]
	public class UI
	{
		public LayerMask wallLayerMask;
		public float doubleClickDelay = 0.25f;
		public EntityEquipmentData.SecondaryStat.StatType[] statsDisplayOrder;
	}

	[System.Serializable]
	public partial class DatasConfigs
	{
		public bool editorLoadDatasFromGamedatasFile = true;
	}

	[System.Serializable]
	public class Input
	{
		public float interactionRayCastLength = 100f;
		public LayerMask uiLayer;
		public LayerMask entityLayer;
		public LayerMask interactionRayCastLayer;
		public LayerMask tileInternRayCastLayer;
		public LayerMask wallRayCastLayer;

	}
	
	[System.Serializable]
	public class Parsing
	{
		public SerializableDictionary<string, AParsableScriptableObject> baseParsableScriptablePerType = new();
		public string componentsSpreadSheetID = "1AeQujaBf6YdyVQRD2gBNWazoosesi46DpAoY5b6hrt8";
		public SerializableDictionary<EntityEquipmentData.EquipmentType, string> componentGUIDPerPage;
		public string unitSpreadSheetID = "1PpT7zhUyvnxNfQoW6Z8a4ntcJ83OzwLOVkCfgDB5bX0";
		public string actionSpreadSheetID = "1quAn64T5zuE6npFmDzjOhNFFvw4UJaXSoWxX-uvy3gI";
		public SerializableDictionary<EntityActionData.ActionType, string> actionGUIDPerPage;
	}

	public void Initialize ()
	{
	}
}
