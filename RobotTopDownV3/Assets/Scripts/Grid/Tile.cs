using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class Tile : MonoBehaviour
{
	public static float outerRadius = 1f;
	public static float innerRadius = outerRadius * 0.866025404f;

	[Title("Depedencies")]
	[SerializeField] private TileUIPlugin m_ui;
	public TileUIPlugin UI => m_ui;

	public TileCoordinates coordinates;
	[SerializeField, ReadOnly] Tile[] m_neighbors;
	public Tile[] Neighbors => m_neighbors;

	//ground
	[SerializeField, ReadOnly] private TileGroundType m_groundType;
	public TileGroundType GroundType => m_groundType;

	private bool m_canInteract = false;
	public bool CanInteract => m_canInteract;

	private bool m_isVisible = false;
	public bool IsVisible => m_isVisible;

	[SerializeField] private Transform m_wallPartsParent;
	public Transform WallPartsParent { get => m_wallPartsParent; set => m_wallPartsParent = value; }

	[SerializeField] private Wall m_wall;
	public Wall Wall { get { return m_wall; } set { m_wall = value; } }
	[SerializeField] private Wall m_structure;
	public Wall Structure { get { return m_structure; } set { m_structure = value; } }

	private List<EntityStatusEnumID> m_status = new();
	public List<EntityStatusEnumID> Status => m_status;
	private Dictionary<AEntityStatus, int> m_remainingDurationToActiveEffects = new();
	[SerializeField] private Transform m_statusVisualsParent;
	private Dictionary<EntityStatusEnumID, GameObject> m_statusVisuals = new();

	//Content on tile
	//private TileContent m_currentContent = new() { itemID = -1, entityID = -1 };
	//private TileContent m_nextTurnActionContent = new() { itemID = -1, entityID = -1 };
	private TileContent[] m_plannedContentsPerTick;

	//private HashSet<NeuronalMembraneEquipmentData.VisionTypes> m_currentVisionTypesOnThisTile = new();
	private Dictionary<NeuronalMembraneEquipmentData.VisionTypes, int> m_visionTypeCounts = new();
	private int m_opticVisionBlockerCount = 0;

	[Serializable]
	public class TileContent
	{
		public int entityID = -1;
		public int itemID = -1;

		public Entity Entity => GameManager.Instance.GetEntityFromID(entityID);
		public Item Item => GameManager.Instance.GetItemFromID(itemID);

		public void Reset ()
		{
			entityID = -1;
			itemID = -1;
		}

		public void Copy ( TileContent _otherContent )
		{
			entityID = _otherContent.entityID;
			itemID = _otherContent.itemID;
		}
	}

	public enum TileDirectionType
	{
		Front,
		ForwardSide,
		BackSide,
		Back
	}

	#region Pathfinding params
	private int m_distance;
	public int Distance
	{
		get
		{
			return m_distance;
		}
		set
		{
			m_distance = value;
		}
	}

	#endregion

	private void Awake ()
	{
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionRemoved += OnActionRemoved;
		TurnManager.onActionSelected += OnActionSelected;
		TurnManager.onStartInputPhase += OnStartInputPhase;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		TurnManager.onNewRoundStart += OnRoundStart;
		TurnManager.onEndPlayPhase += OnPlayPhaseEnd;
		PlayerController.onEntitySelected += OnEntitySelected;

		if (m_plannedContentsPerTick == null || m_plannedContentsPerTick.Length == 0)
		{
			m_plannedContentsPerTick = new TileContent[GameConfig.current.game.actionTokenPerRound + 1];
			for (int i = 0; i < m_plannedContentsPerTick.Length; i++)
			{
				m_plannedContentsPerTick[i] = new() { entityID = -1, itemID = -1 };
			}
		}
	}

	private void OnDestroy ()
	{
		TurnManager.onActionAdded -= OnActionAdded;
		TurnManager.onActionRemoved -= OnActionRemoved;
		TurnManager.onActionSelected -= OnActionSelected;
		TurnManager.onStartInputPhase -= OnStartInputPhase;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
		TurnManager.onNewRoundStart -= OnRoundStart;
		TurnManager.onEndPlayPhase -= OnPlayPhaseEnd;
		PlayerController.onEntitySelected -= OnEntitySelected;
	}

	#region Grid sys

#if UNITY_EDITOR
	public void Init ( int _x, int _y, GridData.TileData _data = null )
	{
		m_neighbors = new Tile[6];

		m_ui.SetPosition(_x, _y);
		if (_data != null)
		{
			SetGroundType(_data.groundType);
			if (_data.groundType == TileGroundType.Wall || _data.groundType == TileGroundType.Cover)
				SetupWall(_data.wallType, _data.orientation, _data.groundType == TileGroundType.Cover);
			else if (_data.groundType == TileGroundType.PlayerStructure || _data.groundType == TileGroundType.EnemyStructure)
				SetupStructure();
			else
			{
				RemoveWall();
				RemoveStructure();
			}
		}

		//SetActiveFOW(NeuronalMembraneEquipmentData.VisionTypes.Optical, false, true);
	}

	public void SetGroundType ( TileGroundType _groundType )
	{
		UnityEditor.Undo.RecordObject(this, "Paint Tile");
		//UnityEditor.Undo.RecordObject(m_wall, "Paint Tile");
		m_groundType = _groundType;
		m_ui.UpdateGroundMaterial();

		UnityEditor.EditorUtility.SetDirty(this);
	}

	public void SetupWall ( Wall.WallType _wallType, int _orientation, bool _isCover )
	{
		if (m_wall == null)
			m_wall = gameObject.AddComponent<Wall>();
		//m_wall = UnityEditor.Undo.AddComponent<Wall>(gameObject);

		m_wall.Init(this, _wallType, m_groundType == TileGroundType.Cover, _orientation);
	}

	public void RemoveWall ()
	{
		if (m_wall != null)
		{
			foreach (GameObject wallPart in m_wall.WallParts)
				DestroyImmediate(wallPart);
			m_wall.WallParts.Clear();

			DestroyImmediate(m_wall);
			m_wall = null;
		}
	}

	public void SetupStructure ()
	{
		if (m_wall == null)
			m_wall = gameObject.AddComponent<Wall>();
		//m_wall = UnityEditor.Undo.AddComponent<Wall>(gameObject);

		m_wall.InitStructure(this);
	}

	public void RemoveStructure ()
	{
		if (m_wall != null)
		{
			foreach (GameObject wallPart in m_wall.WallParts)
				DestroyImmediate(wallPart);
			m_wall.WallParts.Clear();

			DestroyImmediate(m_wall);
			m_wall = null;
		}
	}

	[Button]
	private void PrintSavedData ()
	{
		GridData.TileData data = GridManager.Instance.GridData.tiles[coordinates.ID];
		Debug.Log(coordinates.ID + " : " + data.groundType + " ; " + data.wallType + " ; " + data.orientation);
	}

#endif

	public Tile GetNeighbor ( HexDirection _direction )
	{
		return m_neighbors[(int)_direction];
	}

	public void SetNeighbor ( HexDirection _direction, Tile _tile )
	{
		m_neighbors[(int)_direction] = _tile;
		_tile.Neighbors[(int)_direction.Opposite()] = this;
	}

	public bool IsObstacle ( bool _isThisTurn )
	{
		int currentTick = TurnManager.currentTick;
		if ((m_groundType == TileGroundType.Wall || m_groundType == TileGroundType.Cover) && (_isThisTurn ? m_wall.RegisteredHealth > 0 : m_wall.Health > 0))
			return true;
		else if (m_groundType == TileGroundType.Void)
			return true;
		else if (_isThisTurn && m_plannedContentsPerTick[currentTick].Item != null && !m_plannedContentsPerTick[currentTick].Item.Data.CanWalkThroughPredicate(m_plannedContentsPerTick[currentTick].Item.LinkedData, m_plannedContentsPerTick[currentTick].Item, _isThisTurn))
			return true;
		else if (!_isThisTurn && m_plannedContentsPerTick[currentTick + 1].Item != null && !m_plannedContentsPerTick[currentTick + 1].Item.Data.CanWalkThroughPredicate(m_plannedContentsPerTick[currentTick + 1].Item.LinkedData, m_plannedContentsPerTick[currentTick + 1].Item, _isThisTurn))
			return true;

		return false;
	}

	public bool CanSeeThrough ()
	{
		if (m_groundType == TileGroundType.Wall && m_wall.Health > 0)
			return false;

		if (m_status.Contains(EntityStatusEnumID.Smoked))
			return false;

		return true;
	}

	#endregion


	#region Turn sys

	public void OnEntityEnter ( Entity _enteringEntity, bool _isFromTeleportation )
	{
		if (m_groundType == TileGroundType.Void && !_enteringEntity.Status.Contains(EntityStatusEnumID.Flying))
		{
			Dictionary<WeaponEquipmentData.DamageType, int> damages = new();
			damages.Add(WeaponEquipmentData.DamageType.Bludgeoning, 9999);
			_enteringEntity.Equipment.TakeDamage(new EntityEquipmentPlugin.TakeDamageCallback() { damages = damages });
		}

		if (m_plannedContentsPerTick[TurnManager.currentTick].Item != null)
			m_plannedContentsPerTick[TurnManager.currentTick].Item.OnTileEnter(_enteringEntity, _isFromTeleportation);
	}

	private void OnEntitySelected ( int? _entityID )
	{
		if (!_entityID.HasValue)
		{
			UI.ResetOutline();
			m_canInteract = false;
		}
	}

	private void OnActionSelected ( AEntityAction _action )
	{
		bool canInteract = _action.TileInteractPredicate(this);
		m_canInteract = canInteract;
		UI.SetAsInteractable(m_canInteract, GameAssets.current.game.entityActionsData[_action.enumID].tileOutlineColor);
	}

	private void OnActionAdded ( TurnManager.RecordedAction _recordedAction )
	{
		UI.ResetOutline();
		m_canInteract = false;
	}

	private void OnActionRemoved ( TurnManager.RecordedAction _recordedAction )
	{
		UI.ResetOutline();
		m_canInteract = false;
	}

	private void OnStartInputPhase ()
	{
		UI.ClearPositionText();
		m_canInteract = false;
		//if (m_plannedContentsPerTick[^1].entityID != -1)
		for (int i = 1; i < m_plannedContentsPerTick.Length; i++)
			m_plannedContentsPerTick[i].Reset();
	}

	private void OnPlayPhaseEnd ()
	{
		NewPhase(m_plannedContentsPerTick.Length - 1);
		m_plannedContentsPerTick[0].Copy(m_plannedContentsPerTick[^1]);
	}

	private void OnEndInputPhase ()
	{
		UI.ResetOutline();
		m_canInteract = false;
	}

	public void NewPhase ( int _currentTick )
	{
		if (_currentTick == 0)
			return;

		if (m_plannedContentsPerTick[_currentTick - 1].entityID != -1)
			m_plannedContentsPerTick[_currentTick].entityID = m_plannedContentsPerTick[_currentTick - 1].entityID;
		if (m_plannedContentsPerTick[_currentTick - 1].itemID != -1)
			m_plannedContentsPerTick[_currentTick].itemID = m_plannedContentsPerTick[_currentTick - 1].itemID;
	}

	private void OnRoundStart ()
	{
		foreach (EntityStatusEnumID status in m_status.ToArray())
		{
			if (--m_remainingDurationToActiveEffects[GameAssets.current.game.entityStatus[status]] <= 0)
				RemoveStatus(status);

			GameAssets.current.game.entityStatus[status].PerformStatusEffectAtBeginingOfRound(this);
		}
	}

	public void ClearEntityAnyTurn ( Entity _entity )
	{
		ClearEntityAnyTurn(_entity.ID);
	}

	public void ClearEntityAnyTurn ( int _entityID )
	{
		foreach (TileContent content in m_plannedContentsPerTick)
		{
			if (content.entityID == _entityID)
				content.entityID = -1;
		}
	}

	public void SetEntity ( Entity _entity, bool _isThisTurn )
	{
		m_plannedContentsPerTick[TurnManager.currentTick + (_isThisTurn ? 0 : 1)].entityID = _entity == null ? -1 : _entity.ID;
		/*if (_isThisTurn)
			m_currentContent.entityID = _entity == null ? -1 : _entity.ID;
		else
			m_nextTurnActionContent.entityID = _entity == null ? -1 : _entity.ID;*/
	}

	public Entity GetEntity ( bool _isThisTurn )
	{
		return m_plannedContentsPerTick[TurnManager.currentTick + (_isThisTurn ? 0 : 1)].Entity;
		/*if (_isThisTurn)
			return m_currentContent.Entity;
		else
			return m_nextTurnActionContent.Entity;*/
	}

	public int GetEntityId ( bool _isThisTurn )
	{
		return m_plannedContentsPerTick[TurnManager.currentTick + (_isThisTurn ? 0 : 1)].entityID;
	}

	public bool TryGetEntity ( bool _isThisTurn, out Entity _entity )
	{
		int currentTick = TurnManager.currentTick;
		if (_isThisTurn)
		{
			_entity = m_plannedContentsPerTick[currentTick].Entity;
			return m_plannedContentsPerTick[currentTick].entityID != -1;
		}
		else
		{
			_entity = m_plannedContentsPerTick[currentTick + 1].Entity;
			return m_plannedContentsPerTick[currentTick + 1].entityID != -1;
		}
	}

	public void SetItem ( Item _item, bool _isThisTurn )
	{
		if (_isThisTurn)
			m_plannedContentsPerTick[TurnManager.currentTick].itemID = _item == null ? -1 : _item.ID;
		else
			m_plannedContentsPerTick[TurnManager.currentTick + 1].itemID = _item == null ? -1 : _item.ID;
	}

	public bool TryGetItem ( bool _isThisTurn, out Item _item )
	{
		if (_isThisTurn)
		{
			_item = m_plannedContentsPerTick[TurnManager.currentTick].Item;
			return m_plannedContentsPerTick[TurnManager.currentTick].itemID != -1;
		}
		else
		{
			_item = m_plannedContentsPerTick[TurnManager.currentTick + 1].Item;
			return m_plannedContentsPerTick[TurnManager.currentTick + 1].itemID != -1;
		}
	}

	public Item GetItem ( bool _isThisTurn )
	{
		if (_isThisTurn)
			return m_plannedContentsPerTick[TurnManager.currentTick].Item;
		else
			return m_plannedContentsPerTick[TurnManager.currentTick + 1].Item;
	}

	public bool TryGetPlannedItemAt ( int _time, out Item _item )
	{
		_item = m_plannedContentsPerTick != null && m_plannedContentsPerTick.Length > _time && m_plannedContentsPerTick[_time] != null
			? m_plannedContentsPerTick[_time].Item : null;
		return _item != null;
	}

	public void SetPlannedItemAt ( Item _item, int _time )
	{
		for (int i = _time; i < m_plannedContentsPerTick.Length; i++)
		{
			if (_item == null && m_plannedContentsPerTick[i].Item != null)
				m_plannedContentsPerTick[i].Item.Cancel();
			m_plannedContentsPerTick[i].itemID = _item == null ? -1 : _item.ID;
		}
	}

	public Item GetPlannedItemAt ( int _time )
	{
		return m_plannedContentsPerTick[_time].Item;
	}

	#endregion

	public void AddStatus ( EntityStatusEnumID _statusID )
	{
		AEntityStatus statusData = GameAssets.current.game.entityStatus[_statusID];
		statusData.ApplyStatus(this);
		m_status.Add(_statusID);
		m_remainingDurationToActiveEffects.Add(GameAssets.current.game.entityStatus[_statusID], GameAssets.current.game.entityStatus[_statusID].duration);

		//spawn visual
		if (statusData.groundPrefab != null && !m_statusVisuals.ContainsKey(_statusID))
			m_statusVisuals.Add(_statusID, Instantiate(statusData.groundPrefab, m_statusVisualsParent));
	}

	public void RemoveStatus ( EntityStatusEnumID _statusID )
	{
		GameAssets.current.game.entityStatus[_statusID].RemoveStatus(this);
		m_status.Remove(_statusID);
		m_remainingDurationToActiveEffects.Remove(GameAssets.current.game.entityStatus[_statusID]);

		if (m_statusVisuals.ContainsKey(_statusID))
		{
			Destroy(m_statusVisuals[_statusID]);
			m_statusVisuals.Remove(_statusID);
		}
	}

	public void SetActiveFOW ( NeuronalMembraneEquipmentData.VisionTypes _visionType, bool _isRemoving = false, bool _isInstant = false )
	{
		m_visionTypeCounts.TryGetValue(_visionType, out int count);
		count += _isRemoving ? -1 : 1;

		if (count <= 0)
			m_visionTypeCounts.Remove(_visionType);
		else
			m_visionTypeCounts[_visionType] = count;

		RefreshFOW(_isInstant);
	}

	//Smoke and alike block optic vision on this tile without touching the vision counters,
	//so lifting the block cannot resurrect a vision that nobody provides anymore.
	public void SetOpticVisionBlocked ( bool _isBlocked, bool _isInstant = false )
	{
		m_opticVisionBlockerCount = Mathf.Max(0, m_opticVisionBlockerCount + (_isBlocked ? 1 : -1));

		RefreshFOW(_isInstant);
	}

	private void RefreshFOW ( bool _isInstant )
	{
		m_isVisible = false;
		foreach (NeuronalMembraneEquipmentData.VisionTypes type in m_visionTypeCounts.Keys)
		{
			if (HasVision(type))
			{
				m_isVisible = true;
				break;
			}
		}

		m_ui.SetActiveFOW(!HasVision(NeuronalMembraneEquipmentData.VisionTypes.Optic), _isInstant);

		if (TryGetCurrentEntity(out Entity entity) && !entity.IsAlliedTo(GameManager.Instance.PlayerID))
		{
			NeuronalMembraneEquipmentData.VisionTypes bestVision = m_isVisible ? GetBestCurrentVisionType() : NeuronalMembraneEquipmentData.VisionTypes.Radar;
			entity.SetVisibility(m_isVisible, bestVision);
		}
	}

	private bool HasVision ( NeuronalMembraneEquipmentData.VisionTypes _visionType )
	{
		if (_visionType == NeuronalMembraneEquipmentData.VisionTypes.Optic && m_opticVisionBlockerCount > 0)
			return false;

		return m_visionTypeCounts.ContainsKey(_visionType);
	}

	//Who will still be standing here once this tick is played. The next tick slot only holds entities that
	//booked a move, so a motionless one has to be read on the current tick and checked against its own action.
	//Prepare clears a mover's current tick slot, which is what lets a unit follow an ally out of a tile.
	public Entity GetEntityAtEndOfTick ()
	{
		Entity booked = GetEntity(_isThisTurn: false);
		if (booked != null)
			return booked;

		Entity current = GetEntity(_isThisTurn: true);
		if (current == null || TurnManager.Instance == null)
			return current;

		return TurnManager.Instance.IsEntityLeavingTileThisTick(current.ID, coordinates.ID) ? null : current;
	}

	//During the play phase a moving entity is registered on its destination tile at currentTick + 1,
	//while the tile it just left still references it at currentTick. Both slots are checked, and each
	//candidate is validated against its real coordinates so a stale slot never drives the FOW visual.
	public Entity GetCurrentEntity ()
	{
		return TryGetCurrentEntity(out Entity entity) ? entity : null;
	}

	public bool TryGetCurrentEntity ( out Entity _entity )
	{
		if (TryGetEntity(false, out _entity) && IsEntityStandingHere(_entity))
			return true;
		if (TryGetEntity(true, out _entity) && IsEntityStandingHere(_entity))
			return true;

		_entity = null;
		return false;
	}

	private bool IsEntityStandingHere ( Entity _entity )
	{
		return _entity != null && _entity.Displacement.Coordinates.ID == coordinates.ID;
	}

	public NeuronalMembraneEquipmentData.VisionTypes GetBestCurrentVisionType ()
	{
		NeuronalMembraneEquipmentData.VisionTypes best = NeuronalMembraneEquipmentData.VisionTypes.Radar;
		foreach (NeuronalMembraneEquipmentData.VisionTypes type in m_visionTypeCounts.Keys)
			if ((int)type < (int)best && HasVision(type))
				best = type;
		return best;
	}
}
