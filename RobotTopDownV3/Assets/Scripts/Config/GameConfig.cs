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
	public Online online = new Online();

	[System.Serializable]
	public class Online
	{
		public float disconnectionWaitDuration = 60f;
		public float reconnectionAttemptInterval = 2f;
	}

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

		[Title("Micro")]
		public int zonePointsToWin = 5;
		public int sabotagePointHealth = 10;
		public float demolisherSabotageHitBonus = 1f;

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
		public float cameraPanSpeed = .05f;

		[Title("Entity")]
		public SerializableDictionary<Tile.TileDirectionType, float> entityFlankRatio = new();
		public float entityMovementEvasionBonus = .2f;
		public float entityCoverBonus = .2f;
		public SerializableDictionary<TileGroundType, float> coverBonusPerGroundType;
		//public int SerializableDictionary<EntityActionData., > maxSlotAmountPerType
		public SerializableDictionary<NeuronalMembraneEquipmentData.VisionTypes, int> rangePerVisionType;
		public SerializableDictionary<WeaponEquipmentData.DistanceType, float> distanceTypeSpreadEvaluation;
		public SerializableDictionary<WeaponEquipmentData.DamageType, WeaponEquipmentData.DamageCategory> damageCategoryPerDamageType;
		public SerializableDictionary<WeaponEquipmentData.DamageCategory, EntityEquipmentData.SecondaryStat.StatType> statTypePerDamageCategory;
		public SerializableDictionary<WeaponEquipmentData.DamageCategory, EntityEquipmentData.SecondaryStat.StatType> statTypePerResistanceCategory;
		public SerializableDictionary<WeaponEquipmentData.DamageType, EntityEquipmentData.SecondaryStat.StatType> statTypePerDamageType;
		public SerializableDictionary<WeaponEquipmentData.DamageType, EntityEquipmentData.SecondaryStat.StatType> statTypePerDamageResistanceType;
		public int pushCollisionDamage = 2;

		[Title("Hub")]
		public string hubSceneName;
		public string startScreenSceneName;
		[Min(1)] public int dailyMatchAmount = 4;
		[Min(1)] public int maxRewardComponentCount = 3;

		[Title("Day events")]
		[Range(0f, 1f)] public float randomDayEventChance = .5f;
		[Range(0f, 1f)] public float matchNextDayEventChance = .2f;
		[InfoBox("Cycle and day as shown in the hub, counted from 1.")]
		public List<ScriptedDayEvent> scriptedDayEvents = new();

		[Title("Procedural matches")]
		public EnemySquadRules[] enemySquadRules;
		public StrategyArchetypes[] strategyArchetypes;
		[Range(0f, 1f)]
		public float expensiveEnemyChance = 0.35f;
		[Range(1f, 5f)]
		public float expensiveEnemyEnergyRatio = 2f;

		[Title("Tournament")]
		public int challengerTournamentInterval = 2;

	}

	[System.Serializable]
	public class ScriptedDayEvent
	{
		[Min(1)] public int cycle = 1;
		[Min(1)] public int day = 1;
		public DayEventData dayEvent;
	}

	[System.Serializable]
	public class EnemySquadRules
	{
		public PlaythroughPhase phase;
		public int startCycle;
		public int maxUnitCount;
		public int maxEnergy;
	}

	[System.Serializable]
	public class StrategyArchetypes
	{
		public MatchStrategy strategy;
		public Archetype[] archetypes;
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
		public EntityEquipmentData.SecondaryStat.StatType[] damageStats;
		public EntityEquipmentData.SecondaryStat.StatType[] resistanceStats;
		public EntityEquipmentData.SecondaryStat.StatType[] statusStats;
		public Color opticVisionColor = new(.93f, .95f, .97f);
		public Color thermalVisionColor = new(1f, .42f, .24f);
		public Color radarVisionColor = new(.49f, 1f, .42f);
		public PanelMotion motion = new();

		public Color GetVisionColor ( NeuronalMembraneEquipmentData.VisionTypes _vision )
		{
			return _vision switch
			{
				NeuronalMembraneEquipmentData.VisionTypes.Thermic => thermalVisionColor,
				NeuronalMembraneEquipmentData.VisionTypes.Radar => radarVisionColor,
				_ => opticVisionColor
			};
		}
	}

	[System.Serializable]
	public class PanelMotion
	{
		[Title("Holo reveal of panel sections")]
		[Min(0f)] public float revealDuration = .5f;
		[Min(0f)] public float revealStagger = .1f;
		[Min(0f)] public float revealSoftness = 28f;
		[Min(1f)] public float scanLineHeight = 12f;
		public Color revealColor = new(.33f, .86f, .95f, 1f);
		[Range(0f, 1f)] public float trailAlpha = .16f;
		[Min(0f)] public float trailHeight = 110f;

		[Title("Leaving a hub place")]
		[Min(0f)] public float exitDuration = .24f;
		[Min(0f)] public float exitStagger = .05f;
		[Min(0f)] public float exitDistance = 90f;
		[Min(0f)] public float slideEntranceDelay = .14f;

		[Title("Micro-interactions")]
		[Min(1f)] public float buttonHoverScale = 1.03f;
		[Min(0f)] public float hoverDuration = .14f;
		[Range(.5f, 1f)] public float pressScale = .94f;
		[Min(0f)] public float pressDuration = .06f;
		[Range(0f, 1f)] public float shineAlpha = .3f;
		[Min(0f)] public float shineDuration = .45f;
		[Min(0f)] public float swapDistance = 14f;
		[Min(0f)] public float swapDuration = .22f;
		[Min(0f)] public float fillDuration = .35f;
		[Min(0f)] public float growDuration = .22f;
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
