using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObject/GameConfig")]
public partial class GameConfig : ScriptableObject
{
	public static GameConfig current => ApplicationManager.config;

	public GameSettings game = new GameSettings();
	public Input input = new Input();
	public Meta meta = new Meta();
	public UI ui = new UI();
	public DatasConfigs datas = new DatasConfigs();

	[System.Serializable]
	public partial class GameSettings
	{
		[Title("Actions")]
		public EntityActionData defaultStartAction;
		public float actionDuration = 1f;
		public float entityRotationDuration = .5f;

		[Title("Camera")]
		public float cameraMovementSpeed = 15f;
		public float cameraRotationDuration = .5f;
		public float cameraRotationStep = 90f;
		public Vector2 cameraMovementBoundsOffset = new Vector2(7f, 1.75f);
		public float cameraZoomSpeed;
		public Vector2 cameraZoomBounds;

		public SerializableDictionary<Tile.TileDirectionType, float> entityFlankRatio = new();
		public float entityMovementEvasionBonus = 2;
		public float entityCoverBonus = 2;
		public SerializableDictionary<WeaponEquipmentData.DistanceType, float> distanceTypeSpreadEvaluation;
	}

	[System.Serializable]
	public class Meta
	{
		public SerializableDictionary<LogConsole.LogEventType, Color> colorsPerType = new();
	}
	
	[System.Serializable]
	public class UI
	{
		public LayerMask wallLayerMask;
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
		public LayerMask bulletInteractionLayerMask;

	}

	public void Initialize ()
	{
	}
}
