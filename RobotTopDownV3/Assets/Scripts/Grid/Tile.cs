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

	//Content on tile
	public TileContent currentContent;
	public TileContent nextTurnActionContent;
	public struct TileContent
	{
		public Entity entity;
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
			m_ui.UpdateDistanceLabel(value);
		}
	}

	#endregion

	/*private void Awake ()
	{
		m_neighbors = new Tile[6];
	}*/

	private void Start ()
	{
		TurnManager.onActionAdded += OnActionAdded;
		TurnManager.onActionRemoved += OnActionRemoved;
		TurnManager.onActionSelected += OnActionSelected;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		TurnManager.onStartInputPhase += OnStartInputPhase;
		PlayerController.onEntitySelected += OnEntitySelected;
	}

	private void OnDestroy ()
	{
		TurnManager.onActionAdded -= OnActionAdded;
		TurnManager.onActionRemoved -= OnActionRemoved;
		TurnManager.onActionSelected -= OnActionSelected;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
		TurnManager.onStartInputPhase -= OnStartInputPhase;
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
			if (_data.groundType == TileGroundType.Wall)
				SetupWall(_data.wallType, _data.orientation);
			else
				RemoveWall();
		}

		SetActiveFOW(false, true);
	}

	public void SetGroundType ( TileGroundType _groundType )
	{
		UnityEditor.Undo.RecordObject(this, "Paint Tile");
		//UnityEditor.Undo.RecordObject(m_wall, "Paint Tile");
		m_groundType = _groundType;
		m_ui.UpdateGroundMaterial();

		UnityEditor.EditorUtility.SetDirty(this);
	}

	public void SetupWall ( Wall.WallType _wallType, int _orientation)
	{
		if (m_wall == null)
			m_wall = UnityEditor.Undo.AddComponent<Wall>(gameObject);

		m_wall.LinkWithTile(this);
		m_wall.SetWallType(_wallType);
		m_wall.Rotate(_orientation);
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

	public bool IsObstacle ()
	{
		if (m_groundType == TileGroundType.Wall && m_wall.Health > 0)
			return true;
		else if (m_groundType == TileGroundType.Void)
			return true;

		return false;
	}

	public bool CanSeeThrough ()
	{
		if (m_groundType == TileGroundType.Wall && m_wall.Health > 0)
			return false;

		return true;
	}

	#endregion


	#region Turn sys

	private void OnEntitySelected ( int? _entityID )
	{
		UI.ResetOutline();
		m_canInteract = false;
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
		m_canInteract = false;
	}

	private void OnEndInputPhase ()
	{
		UI.ResetOutline();
		m_canInteract = false;
	}

	public void NewPhase ()
	{
		SetEntity(currentContent.entity, false);
	}

	public void SetEntity ( Entity _entity, bool _isThisTurn )
	{
		if (_isThisTurn)
			currentContent.entity = _entity;
		else
			nextTurnActionContent.entity = _entity;
	}

	public Entity GetEntity ( bool _isThisTurn )
	{
		if (_isThisTurn)
			return currentContent.entity;
		else
			return nextTurnActionContent.entity;
	}

	#endregion

	public void SetActiveFOW ( bool _isActive = false, bool _isInstant = false )
	{
		m_isVisible = !_isActive;
		m_ui.SetActiveFOW(!m_isVisible, _isInstant);

		if (currentContent.entity != null)
			currentContent.entity.SetVisibility(m_isVisible);
	}
}
