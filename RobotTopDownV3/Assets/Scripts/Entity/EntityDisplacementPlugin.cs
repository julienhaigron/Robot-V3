using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EntityDisplacementPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityMovement;
	public static System.Action<Entity> onAnyEntitySpawn;

	[SerializeField] private Transform m_bottomPosition;
	public Vector3 FeetPosition => m_bottomPosition.position;

	private TileCoordinates m_coordinate;
	public TileCoordinates Coordinates => m_coordinate;

	private TileCoordinates m_previousCoordinate;
	public TileCoordinates PreviousCoordinates => m_previousCoordinate;
	private int m_lastMovementTick = -1;
	public bool DidMoveThisTick => m_lastMovementTick == TurnManager.currentTick;

	private int m_currentOrientation;
	public int CurrentOrientation => m_currentOrientation;

	private EntityAnchor.Spawn m_spawn;
	public EntityAnchor.Spawn Spawn => m_spawn;

	private bool m_didMoveThisTurn = false;
	public bool DidMoveThisTurn => m_didMoveThisTurn;
	private int m_traveledTileCountThisTurn = 0;
	public int TraveledTileCountThisTurn => m_traveledTileCountThisTurn;
	private int m_traveledTileTotalCount = 0;
	public int TraveledTileTotalCount => m_traveledTileTotalCount;

	private Tween m_movementTween;
	private Tween m_rotationTween;
	private System.Action m_pendingMovementDoneAction;


	private void Awake ()
	{
		m_linkedEntity.onStartPerformAction += OnStartPerformAction;
		TurnManager.onStartInputPhase += OnNewTurnBegin;
	}

	private void OnDestroy ()
	{
		m_linkedEntity.onStartPerformAction -= OnStartPerformAction;
		TurnManager.onStartInputPhase -= OnNewTurnBegin;

		if (m_movementTween.IsActive())
			m_movementTween.Kill();
		if (m_rotationTween.IsActive())
			m_rotationTween.Kill();
	}

	public void CancelMovement ( Tween _movement )
	{
		if (_movement == null || !_movement.IsActive())
			return;

		bool isCurrentMovement = _movement == m_movementTween;
		_movement.Kill();
		if (!isCurrentMovement)
			return;

		m_pendingMovementDoneAction = null;
		m_linkedEntity.Skin.SetIsMoving(false);
	}

	private void KillMovementTweenAndReleasePendingAction ()
	{
		if (m_movementTween.IsActive())
			m_movementTween.Kill();

		ConsumePendingMovementDoneAction();
	}

	private void ConsumePendingMovementDoneAction ()
	{
		m_linkedEntity.Skin.SetIsMoving(false);
		System.Action pendingAction = m_pendingMovementDoneAction;
		m_pendingMovementDoneAction = null;
		pendingAction?.Invoke();
	}

	private void ReleaseTileLeftBehind ( Tile _destination )
	{
		Tile originTile = m_coordinate.GetTile();
		if (originTile == null || originTile == _destination)
			return;

		if (originTile.GetEntity(true) == m_linkedEntity)
			originTile.SetEntity(null, _isThisTurn: true);

		if (originTile.GetEntity(false) == m_linkedEntity)
			originTile.SetEntity(null, _isThisTurn: false);
	}

	public void RegisterOnCurrentTile ()
	{
		Tile tile = m_coordinate.GetTile();
		if (tile == null)
			return;

		tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		tile.SetEntity(m_linkedEntity, _isThisTurn: false);
	}

	public void SetSpawn ( EntityAnchor.Spawn _spawn )
	{
		Tile spawn = _spawn.coordinates.GetTile();
		transform.position = spawn.transform.position - m_bottomPosition.localPosition;
		m_traveledTileTotalCount = 0;
		Rotate(_spawn.isFirstSide ? 1 : 4, 0f);

		spawn.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(spawn.coordinates.X, spawn.coordinates.Z, spawn.coordinates.ID);

		onAnyEntitySpawn?.Invoke(m_linkedEntity);
	}

	public Tween MoveToTile ( int _tileID, System.Action onMovementDoneAction, bool _overrideMovementSpeed = false, float _overritenMovementSpeed = 0, bool _keepsOrientation = false )
	{
		Tile tile = GridManager.Instance.Tiles[_tileID];
		if (IsEntryRefused(tile, onMovementDoneAction))
			return null;

		float movementDuration = _overrideMovementSpeed ? _overritenMovementSpeed : GameConfig.current.game.actionDuration;
		if (!_keepsOrientation && ShouldFaceMovementDirection())
			Rotate(tile, movementDuration);

		KillMovementTweenAndReleasePendingAction();

		m_pendingMovementDoneAction = onMovementDoneAction;
		m_linkedEntity.Skin.SetIsMoving(true);
		m_movementTween = transform.DOMove(tile.transform.position - m_bottomPosition.localPosition, movementDuration)
			.SetEase(Ease.Linear).OnComplete(ConsumePendingMovementDoneAction);
		EnterTile(tile, false);
		onAnyEntityMovement?.Invoke(m_linkedEntity);

		return m_movementTween;
	}

	public void TeleportToTile ( int _tileID, System.Action onMovementDoneAction )
	{
		Tile tile = GridManager.Instance.Tiles[_tileID];
		if (IsEntryRefused(tile, onMovementDoneAction))
			return;

		if (ShouldFaceMovementDirection())
			Rotate(tile, GameConfig.current.game.actionDuration);

		KillMovementTweenAndReleasePendingAction();

		transform.position = tile.transform.position - m_bottomPosition.localPosition;
		EnterTile(tile, true);
		onMovementDoneAction?.Invoke();
		onAnyEntityMovement?.Invoke(m_linkedEntity);
	}

	private bool IsEntryRefused ( Tile _tile, System.Action _onMovementDoneAction )
	{
		Entity occupant = _tile.GetBlockingEntityFor(m_linkedEntity);
		if (occupant == null)
		{
			ReleaseTileLeftBehind(_tile);
			return false;
		}

		Debug.LogError("Movement refused: " + m_linkedEntity.Data.name + " cannot enter tile " + _tile.coordinates.ID
			+ ", still held by " + occupant.Data.name, gameObject);

		RegisterOnCurrentTile();
		_onMovementDoneAction?.Invoke();
		return true;
	}

	private void EnterTile ( Tile _tile, bool _isTeleport )
	{
		_tile.SetEntity(m_linkedEntity, _isThisTurn: false);
		m_previousCoordinate = m_coordinate;
		m_lastMovementTick = TurnManager.currentTick;
		m_coordinate.SetCoordinate(_tile.coordinates.X, _tile.coordinates.Z, _tile.coordinates.ID);

		_tile.OnEntityEnter(m_linkedEntity, _isTeleport);
	}

	private bool ShouldFaceMovementDirection ()
	{
		AEntityAction performedAction = m_linkedEntity.LastPerformedAction;
		return performedAction == null || !performedAction.doesFreeActionOwnFacing;
	}

	public void Rotate ( int _orientation, float _duration = 0f, System.Action _onEndPerform = null )
	{
		if (_orientation == m_currentOrientation)
		{
			_onEndPerform?.Invoke();
			return;
		}

		m_currentOrientation = _orientation;

		if (m_rotationTween.IsActive())
			m_rotationTween.Kill();

		Quaternion facing = RotateEntityAction.GetFacingRotation(_orientation);
		if (_duration == 0f)
		{
			m_linkedEntity.SkinParent.transform.localRotation = facing;
			_onEndPerform?.Invoke();
		}
		else
			m_rotationTween = m_linkedEntity.SkinParent.transform.DOLocalRotateQuaternion(facing, _duration)
				.OnComplete(() => _onEndPerform?.Invoke());
	}

	public void Rotate(Tile _towards, float _duration, System.Action _onEndPerform = null )
	{
		Tile from = m_coordinate.GetTile();

		if (_towards == null || _towards == from)
		{
			_onEndPerform?.Invoke();
			return;
		}

		Rotate(GridManager.Instance.GetClosestOrientation(from, _towards), _duration, _onEndPerform);
	}

	private void OnStartPerformAction(AEntityAction _actionPerformed )
	{
		if (_actionPerformed.Data.type == EntityActionData.ActionType.Movement && !_actionPerformed.Data.IsRotation)
		{
			m_didMoveThisTurn = true;
			m_traveledTileCountThisTurn++;
			m_traveledTileTotalCount++;
		}
	}

	private void OnNewTurnBegin ()
	{
		m_didMoveThisTurn = false;
		m_traveledTileCountThisTurn = 0;
		m_lastMovementTick = -1;
	}
}
