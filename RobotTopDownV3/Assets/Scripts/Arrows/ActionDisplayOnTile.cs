using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActionDisplayOnTile : GroundDisplay
{
	private const float c_numberLift = .06f;

	[Header("Tick ring")]
	[SerializeField] private SpriteRenderer m_ring;
	[SerializeField] private SpriteRenderer m_ringCore;
	[SerializeField] private TextMeshPro m_tickNumber;
	[SerializeField] private float m_ringRadius = .27f;
	[SerializeField] private float m_unitClearance = .42f;
	[SerializeField] private Color m_coreColor = new(.96f, .97f, .98f, 1f);
	[SerializeField] private Color m_numberColor = new(.04f, .05f, .09f, 1f);

	private static readonly List<Tile> s_path = new();
	private readonly List<Vector3> m_corners = new();
	private bool m_hasPrevious;
	private bool m_hasNext;

	public TurnManager.RecordedAction RecordedAction { get; private set; }
	public Tile OriginTile { get; private set; }
	public Tile DestinationTile { get; private set; }

	public static void ShowRecorded ( TurnManager.RecordedAction _recordedAction, IReadOnlyList<int> _stepTileIDs )
	{
		if (_stepTileIDs == null || _stepTileIDs.Count == 0)
			return;

		AEntityAction action = _recordedAction.action;
		Tile[] tiles = GridManager.Instance.Tiles;
		s_path.Clear();
		s_path.Add(tiles[action.supposedPositionAtActionStartID]);
		for (int i = 0; i < _stepTileIDs.Count; i++)
			s_path.Add(tiles[_stepTileIDs[i]]);

		Show(action.performingEntityID, s_path, 0, s_path.Count, action.TimeAtStartPerform + 1, _recordedAction.entityState, _recordedAction);
	}

	public static void ShowRecorded ( TurnManager.RecordedAction _recordedAction, int _destinationTileID )
	{
		AEntityAction action = _recordedAction.action;
		if (_destinationTileID < 0 || _destinationTileID == action.supposedPositionAtActionStartID)
			return;

		s_path.Clear();
		s_path.Add(GridManager.Instance.Tiles[action.supposedPositionAtActionStartID]);
		s_path.Add(GridManager.Instance.Tiles[_destinationTileID]);
		Show(action.performingEntityID, s_path, 0, 2, action.TimeAtStartPerform + 1, _recordedAction.entityState, _recordedAction);
	}

	public static void ShowGhost ( AEntityAction _action, IReadOnlyList<Tile> _path, int _tilesPerTick, Entity.EntityState _state )
	{
		if (_path == null)
			return;

		int tilesPerTick = Mathf.Max(1, _tilesPerTick);
		int orientation = TurnManager.Instance.GetLastRegisteredOrientationOfEntity(_action.performingEntityID);
		for (int start = 0, tick = 0; start < _path.Count - 1; start += tilesPerTick, tick++)
		{
			int end = Mathf.Min(start + tilesPerTick, _path.Count - 1);
			Show(_action.performingEntityID, _path, start, end - start + 1, _action.TimeAtStartPerform + 1 + tick * _action.Data.tokenDuration, _state, null);

			int facing = GridManager.Instance.GetClosestOrientation(_path[end - 1], _path[end]);
			if (facing != orientation)
				RotationActionDisplay.ShowMovementTurn(_action.performingEntityID, _path[start], orientation, facing);
			orientation = facing;
		}
	}

	public static void ShowGhost ( AEntityAction _action, Tile _from, Tile _to, Entity.EntityState _state )
	{
		if (_from == _to)
			return;

		s_path.Clear();
		s_path.Add(_from);
		s_path.Add(_to);
		Show(_action.performingEntityID, s_path, 0, 2, _action.TimeAtStartPerform + 1, _state, null);
	}

	private static void Show ( int _performingEntityID, IReadOnlyList<Tile> _path, int _start, int _count, int _arrivalTick, Entity.EntityState _state, TurnManager.RecordedAction _recordedAction )
	{
		if (ObjectsPooling.GetElement(GameAssets.current.game.arrowPoolData, Vector3.zero, Quaternion.identity) is not ActionDisplayOnTile display)
			return;

		display.Init(_path, _start, _count, _arrivalTick, GameAssets.current.ui.entityStateColors[_state], _recordedAction);
		PlayerController.Instance.AddActionDisplay(display, _performingEntityID, _recordedAction == null);
	}

	private void Init ( IReadOnlyList<Tile> _path, int _start, int _count, int _arrivalTick, Color _color, TurnManager.RecordedAction _recordedAction )
	{
		RecordedAction = _recordedAction;
		OriginTile = _path[_start];
		DestinationTile = _path[_start + _count - 1];

		m_corners.Clear();
		for (int i = _start; i < _start + _count; i++)
			m_corners.Add(_path[i].transform.position);

		m_hasPrevious = false;
		m_hasNext = false;
		SetStyle(_color, _recordedAction == null);
		m_tickNumber.SetText("{0}", _arrivalTick);
		Redraw();
	}

	public void LinkTo ( ActionDisplayOnTile _next )
	{
		if (_next.OriginTile != DestinationTile)
			return;

		m_hasNext = true;
		_next.m_hasPrevious = true;
		Redraw();
		_next.Redraw();
	}

	private void Redraw ()
	{
		Vector3 destination = m_corners[^1];
		Vector3 direction = destination - m_corners[^2];
		direction.y = 0f;
		direction.Normalize();

		SetFilletedPath(m_corners, m_hasPrevious ? 0f : m_unitClearance, m_hasNext ? 0f : m_ringRadius + HeadLength * c_stripEndInHead);
		DrawStrip();

		if (m_hasNext)
			HideHead();
		else
			ShowHead(destination - direction * m_ringRadius, direction);

		PlaceFlat(m_ring, destination, direction, MainColor);
		PlaceFlat(m_ringCore, destination, direction, Faded(m_coreColor));
		m_tickNumber.color = Faded(m_numberColor);
		m_tickNumber.transform.position = OnGround(destination) + Vector3.up * c_numberLift;
	}
}
