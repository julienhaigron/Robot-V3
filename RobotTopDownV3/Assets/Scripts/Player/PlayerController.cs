using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;

public class PlayerController : Singleton<PlayerController>
{
	public static Action<int?> onEntitySelected;

	[Title("Camera Settings")]
	[SerializeField] private GameObject playerCamera;

	[Header("Camera Limits")]
	private Vector2 xLimits
	{
		get
		{
			if (GridManager.Instance == null)
				return Vector2.zero;

			return new Vector2(0, GridManager.Instance.gridData.width * Tile.innerRadius * 2f);
		}
	}
	private Vector2 zLimits
	{
		get
		{
			if (GridManager.Instance == null)
				return Vector2.zero;

			return new Vector2 (0, GridManager.Instance.gridData.height * 1.5f);
		}
	}


	private Tween m_cameraRotationTween;
	private Tile m_selectedTile;

	private Quaternion m_targetRotation;
	private float m_currentZoomDistance;

	private Tile m_hoveredTile;

	private Entity m_selectedEntity;
	public Entity SelectedEntity => m_selectedEntity;

	public List<ActionDisplayOnTile> arrows = new();
	public List<ActionDisplayOnTile> tempArrows = new();

	private void Start ()
	{
		InputManager.onTileSelected += OnTileSelected;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onEndInputPhase += OnEndInputPhase;

		m_targetRotation = playerCamera.transform.rotation;
		m_currentZoomDistance = playerCamera.transform.position.y;
	}

	private void OnDestroy ()
	{
		InputManager.onTileSelected -= OnTileSelected;
		InputManager.onTileHovered -= OnTileHovered;
		TurnManager.onEndInputPhase -= OnEndInputPhase;

		if (m_cameraRotationTween.IsActive())
			m_cameraRotationTween.Kill();
	}

	private void FixedUpdate ()
	{
		HandleCameraMovement();

		HandleCameraRotation();

		HandleCameraZoom();
	}

	private void HandleCameraMovement ()
	{
		float moveX = 0f;
		float moveZ = 0f;
		Vector3 forward = playerCamera.transform.forward;
		Vector3 right = playerCamera.transform.right;

		forward.y = 0f;
		right.y = 0f;

		if (Input.GetKey(KeyCode.W)) moveZ += 1f;
		if (Input.GetKey(KeyCode.S)) moveZ -= 1f;
		if (Input.GetKey(KeyCode.A)) moveX -= 1f;
		if (Input.GetKey(KeyCode.D)) moveX += 1f;

		Vector3 move = (forward.normalized * moveZ + right.normalized * moveX)
			* GameConfig.current.game.cameraMovementSpeed
			* Time.fixedDeltaTime;

		Vector3 targetPos = playerCamera.transform.position + move;

		targetPos.x = Mathf.Clamp(targetPos.x, xLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.x, xLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.x);
		targetPos.z = Mathf.Clamp(targetPos.z, zLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.y, zLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.y);

		playerCamera.transform.position = targetPos;
	}

	private void HandleCameraRotation ()
	{
		bool didInput = false;
		if (Input.GetKeyDown(KeyCode.Q))
		{
			m_targetRotation *= Quaternion.Euler(0f, -GameConfig.current.game.cameraRotationStep, 0f);
			didInput = true;
		}
		else if (Input.GetKeyDown(KeyCode.E))
		{
			m_targetRotation *= Quaternion.Euler(0f, GameConfig.current.game.cameraRotationStep, 0f);
			didInput = true;
		}

		if (!didInput)
			return;

		if (m_cameraRotationTween.IsActive())
			m_cameraRotationTween.Kill();

		m_cameraRotationTween = playerCamera.transform.DOLocalRotateQuaternion(m_targetRotation, GameConfig.current.game.cameraRotationDuration).SetEase(Ease.OutQuad);
	}

	private void HandleCameraZoom ()
	{
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (Mathf.Abs(scroll) < 0.001f)
			return;

		float zoomMovement = -(scroll * GameConfig.current.game.cameraZoomSpeed);

		m_currentZoomDistance += zoomMovement;
		m_currentZoomDistance = Mathf.Clamp(m_currentZoomDistance,  GameConfig.current.game.cameraZoomBounds.x, GameConfig.current.game.cameraZoomBounds.y);

		playerCamera.transform.position = new Vector3(playerCamera.transform.position.x, m_currentZoomDistance, playerCamera.transform.position.z);
	}

	private void OnTileSelected ( Tile _tile )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		//Event => Select // unselect entity
		if (_tile.GetEntity(true) != null && !_tile.CanInteract)
		{
			int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
			if (_tile.GetEntity(true).IsAlliedTo(playerId))
			{
				if (m_selectedEntity == _tile.GetEntity(true))
				{
					m_selectedEntity.Deselect();
					m_selectedEntity = null;
					onEntitySelected?.Invoke(null);
					return;
				}
				else if (m_selectedEntity == null)
				{
					m_selectedEntity = _tile.GetEntity(true);
					onEntitySelected?.Invoke(m_selectedEntity == null ? null : m_selectedEntity.ID);

					if (m_selectedEntity != null)
						m_selectedEntity.Select();
					return;
				}
			}

		}

		//validate action
		if (m_selectedEntity != null)
		{
			//if(_tile.canInteract)
			//  => add new action of current selected action type to queue
			if (_tile.CanInteract)
			{
				//TODO : give info to action before adding it
				TurnManager.Instance.CurrentActionSelected.RegisterInteraction(_tile);
			}
		}

		/*if(m_selectedTile != null && m_selectedTile != _tile)
		{
			m_selectedEntity.Displacement.MoveToTile(_tile, null);
			m_selectedEntity = null;
			m_selectedTile = null;
			GridManager.Instance.ClearTileOutile();
		}
		else if (m_selectedTile != _tile && _tile.GetEntity(true) != null && _tile.GetEntity(true) == m_selectedEntity)
		{
			m_selectedEntity.Deselect();
			m_selectedEntity = null;
			m_selectedTile = null;
			GridManager.Instance.ClearTileOutile();
		}
		else if (m_selectedTile != _tile && _tile.GetEntity(true) != null)
		{
			m_selectedTile = _tile;
			m_selectedEntity = _tile.GetEntity(true);
			m_selectedEntity.Select();
			GridManager.Instance.BFS(_tile);
		}*/
	}

	private void OnTileHovered ( Tile _tile )
	{
		if (m_selectedEntity == null)
			return;

		if (_tile != m_hoveredTile)
		{

			ClearGhostActionOnTileDisplay();

			//TODO : display movement towards tile
			if (TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Recording 
				&& (TurnManager.Instance.CurrentActionTypeSelected == EntityActionEnumID.NeighborMove || TurnManager.Instance.CurrentActionTypeSelected == EntityActionEnumID.TargetTileMove)
				&& TurnManager.Instance.CurrentActionSelected.TileInteractPredicate(_tile))
			{
				TurnManager.Instance.CurrentActionSelected.positionAtActionEndID = _tile.coordinates.ID;
				TurnManager.Instance.CurrentActionSelected.GhostDisplay(TurnManager.Instance.CurrentStateTypeSelected);
			}





			/*GridManager.Instance.ClearTileOutile();
			m_selectedEntity.Equipment.AimAtTile("default", _tile);
			List<Tile> tilesInRange = m_selectedEntity.Equipment.GetTilesInRange("default");

			foreach(Tile tile in tilesInRange)
			{
				tile.UI.EnableOutline(Color.green);
			}*/

			m_hoveredTile = _tile;
			/*List<Tile> path = GridManager.Instance.GetPath(m_selectedTile, m_hoveredTile, true);

			if (path == null)
				return;

			foreach (Tile tile in path)
			{
				tile.UI.EnableOutline(Color.blue);
			}*/
		}
	}

	private void OnEndInputPhase ()
	{
		m_selectedEntity = null;
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();
	}

	public void ClearActionOnTileDisplay ()
	{
		foreach (ActionDisplayOnTile arrow in arrows)
		{
			arrow.Discard();
		}
		arrows.Clear();
	}

	public void ClearGhostActionOnTileDisplay ()
	{
		foreach (ActionDisplayOnTile arrow in tempArrows)
		{
			arrow.Discard();
		}
		tempArrows.Clear();
	}
}
