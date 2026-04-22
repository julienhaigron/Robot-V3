using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EntityDisplacementPlugin : EntityPlugin
{
	public static System.Action<Entity> onAnyEntityMovement;
	public static System.Action<Entity> onAnyEntitySpawn;

	[SerializeField] private Transform m_bottomPosition;

	private TileCoordinates m_coordinate;
	public TileCoordinates Coordinates => m_coordinate;

	private int m_currentOrientation;
	public int CurrentOrientation => m_currentOrientation;

	private EntityAnchor.Spawn m_spawn;
	public EntityAnchor.Spawn Spawn => m_spawn;

	private bool m_didMoveThisTurn = false;
	public bool DidMoveThisTurn => m_didMoveThisTurn;

	private Tween m_movementTween;
	private Tween m_rotationTween;


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

	public void SetSpawn ( EntityAnchor.Spawn _spawn )
	{
		//MoveToTile(_spawn.coordinates.GetTile(), null);
		Tile spawn = _spawn.coordinates.GetTile();
		transform.position = spawn.transform.position - m_bottomPosition.localPosition;

		//Rotate((new int[3] { 3, 4, 5 }).RandomElement(), true);
		if (!_spawn.isFirstSide)
			Rotate(4, 0f);
		else
			Rotate(1, 0f);

		spawn.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(spawn.coordinates.X, spawn.coordinates.Z, spawn.coordinates.ID);

		onAnyEntitySpawn.Invoke(m_linkedEntity);
	}

	public void MoveToTile( int _tileID,  System.Action onMovementDoneAction, bool _overrideMovementSpeed = false, float _overritenMovementSpeed = 0)
	{
		if(m_coordinate.GetTile().GetEntity(true) == m_linkedEntity)
			m_coordinate.GetTile().SetEntity(null, _isThisTurn: true);
		Tile tile = GridManager.Instance.Tiles[_tileID];

		if(m_linkedEntity.AI.TargetedEntity == null)
			Rotate(tile, GameConfig.current.game.actionDuration);
			//Rotate(tile, Mathf.Max(GameConfig.current.game.entityRotationDuration, GameConfig.current.game.actionDuration));

		if (m_movementTween.IsActive())
			m_movementTween.Kill();

		float movementDuration = _overrideMovementSpeed ? _overritenMovementSpeed : GameConfig.current.game.actionDuration;
		m_movementTween = transform.DOMove(tile.transform.position - m_bottomPosition.localPosition, movementDuration)
			.SetEase(Ease.Linear).OnComplete(() => onMovementDoneAction?.Invoke());
		tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(tile.coordinates.X, tile.coordinates.Z, tile.coordinates.ID);

		tile.OnEntityEnter(m_linkedEntity);

		//refresh fow
		onAnyEntityMovement?.Invoke(m_linkedEntity);
	}

	public void TeleportToTile(int _tileID, System.Action onMovementDoneAction )
	{
		if (m_coordinate.GetTile().GetEntity(true) == m_linkedEntity)
			m_coordinate.GetTile().SetEntity(null, _isThisTurn: true);
		Tile tile = GridManager.Instance.Tiles[_tileID];

		if (m_linkedEntity.AI.TargetedEntity == null)
			Rotate(tile, GameConfig.current.game.actionDuration);
		//Rotate(tile, Mathf.Max(GameConfig.current.game.entityRotationDuration, GameConfig.current.game.actionDuration));

		if (m_movementTween.IsActive())
			m_movementTween.Kill();

		transform.position = tile.transform.position - m_bottomPosition.localPosition;
		tile.SetEntity(m_linkedEntity, _isThisTurn: true);
		m_coordinate.SetCoordinate(tile.coordinates.X, tile.coordinates.Z, tile.coordinates.ID);

		tile.OnEntityEnter(m_linkedEntity);

		//refresh fow
		onMovementDoneAction?.Invoke();
		onAnyEntityMovement?.Invoke(m_linkedEntity);
	}

	public void Rotate ( int _orientation, float _duration = 0f, System.Action _onEndPerform = null )
	{
		if (_orientation == m_currentOrientation /*&& !_isInstant*/)
		{
			_onEndPerform?.Invoke();
			return;
		}

		m_currentOrientation = _orientation;

		if (m_rotationTween.IsActive())
			m_rotationTween.Kill();

		float angle = 30f + _orientation * 60f;
		if (_duration == 0f)
		{
			m_linkedEntity.SkinParent.transform.localRotation = Quaternion.Euler(0, angle, 0);
			_onEndPerform?.Invoke();
		}
		else
			m_rotationTween = m_linkedEntity.SkinParent.transform.DOLocalRotateQuaternion(Quaternion.Euler(0, angle, 0), _duration)
				.OnComplete(action: ()=> { _onEndPerform?.Invoke(); });
	}

	public void Rotate(Tile _towards, float _duration, System.Action _onEndPerform = null )
	{
		int closestOrientationToTile = GridManager.Instance.GetClosestOrientation(m_coordinate.GetTile(), _towards);
		Rotate(closestOrientationToTile, _duration, _onEndPerform);
	}

	private void OnStartPerformAction(AEntityAction _actionPerformed )
	{
		if (_actionPerformed.Data.type == EntityActionData.ActionType.Movement)
			m_didMoveThisTurn = true;
	}

	private void OnNewTurnBegin ()
	{
		m_didMoveThisTurn = false;
	}
}
