using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Linq;

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

			return new Vector2(0, GridManager.Instance.GridData.width * Tile.innerRadius * 2f);
		}
	}
	private Vector2 zLimits
	{
		get
		{
			if (GridManager.Instance == null)
				return Vector2.zero;

			return new Vector2(0, GridManager.Instance.GridData.height * 1.5f);
		}
	}

	private int PlayerID => !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;

	private Tween m_cameraRotationTween;
	private Tile m_selectedTile;

	private Quaternion m_targetRotation;
	private float m_currentZoomDistance;

	private Tile m_hoveredTile;

	private Entity m_selectedEntity;
	public Entity SelectedEntity => m_selectedEntity;


	//public List<ActionDisplayOnTile> arrows = new();
	private SerializableDictionary<int, List<ActionDisplayOnTile>> m_actionDisplays = new();
	//public SerializableDictionary<int, List<ActionDisplayOnTile>> ActionDisplays => m_actionDisplays;
	//public List<ActionDisplayOnTile> tempArrows = new();
	private SerializableDictionary<int, List<ActionDisplayOnTile>> m_tempActionDisplays = new();
	//public SerializableDictionary<int, List<ActionDisplayOnTile>> TempActionDisplays => m_tempActionDisplays;
	private SerializableDictionary<int, GhostEntity> m_ghostEntities = new();

	private void Start ()
	{
		InputManager.onTileleftClick += OnTileLeftClick;
		InputManager.onTileRightClick += OnTileRightClick;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath += OnAnyEntityDeath;
		TurnManager.onEndLevel += OnEndLevel;

		m_targetRotation = playerCamera.transform.rotation;
		m_currentZoomDistance = playerCamera.transform.position.y;
	}

	private void OnDestroy ()
	{
		InputManager.onTileleftClick -= OnTileLeftClick;
		InputManager.onTileRightClick -= OnTileRightClick;
		InputManager.onTileHovered -= OnTileHovered;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnAnyEntityDeath;
		TurnManager.onEndLevel -= OnEndLevel;

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
		m_currentZoomDistance = Mathf.Clamp(m_currentZoomDistance, GameConfig.current.game.cameraZoomBounds.x, GameConfig.current.game.cameraZoomBounds.y);

		playerCamera.transform.position = new Vector3(playerCamera.transform.position.x, m_currentZoomDistance, playerCamera.transform.position.z);
	}

	private void OnTileLeftClick ( Tile _tile )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		//Event => Select // unselect entity
		if (_tile.GetEntity(true) != null && !_tile.CanInteract)
		{
			//ally entity
			if (_tile.GetEntity(true).IsAlliedTo(PlayerID))
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
				else
				{
					m_selectedEntity.Deselect();
					onEntitySelected?.Invoke(null);
					m_selectedEntity = _tile.GetEntity(true);
					onEntitySelected?.Invoke(m_selectedEntity.ID);
					m_selectedEntity.Select();

					return;
				}
			}

		}

		//validate action
		if (m_selectedEntity != null)
		{
			if (_tile.CanInteract)
			{
				TurnManager.Instance.CurrentActionSelected.RegisterInteraction(_tile);
			}
		}
	}

	private void OnTileRightClick ( Tile _tile )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		if (m_selectedEntity != null && m_actionDisplays.ContainsKey(m_selectedEntity.ID) && m_actionDisplays[m_selectedEntity.ID].Count > 0)
		{
			//ally entity
			if (m_selectedEntity.IsAlliedTo(PlayerID))
			{
				//remove action interaction
				List<ActionDisplayOnTile> actionsOnTile = new();
				foreach (ActionDisplayOnTile display in m_actionDisplays[m_selectedEntity.ID])
				{
					if (display.OriginTile == _tile)
						actionsOnTile.Add(display);
				}
				actionsOnTile.Reverse();

				if (actionsOnTile.Count > 0)
				{
					//remove actions
					foreach (ActionDisplayOnTile display in actionsOnTile)
					{
						List<TurnManager.RecordedAction> actionQueue = TurnManager.Instance.RecordedActions[m_selectedEntity.ID].ToList();
						for (int i = 0; i < actionQueue.Count; i++)
						{
							if (actionQueue[i].action == display.RecordedAction.action)
							{
								TurnManager.Instance.RemoveActionFrom(actionQueue[i], i);
							}
						}
					}
				}
				else
				{
					//unselect entity
					m_selectedEntity.Deselect();
					m_selectedEntity = null;
					onEntitySelected?.Invoke(null);
				}
			}
		}
	}

	private void OnTileHovered ( Tile _tile )
	{
		if (m_selectedEntity == null)
			return;

		if (_tile != m_hoveredTile)
		{

			ClearGhostActionOnTileDisplay();

			int totalCostSpend = 0;
			bool didContainTile = false;
			if (m_actionDisplays.ContainsKey(m_selectedEntity.ID))
			{
				foreach (ActionDisplayOnTile display in m_actionDisplays[m_selectedEntity.ID])
				{
					totalCostSpend += display.RecordedAction.action.Data.GetTokenTotalCost(m_selectedEntity, m_selectedEntity.AI.TargetedEntity);
					if (display.DestinationTile == _tile)
					{
						didContainTile = true;
						break;
					}
				}
			}

			bool isTargetValid = TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Recording
				&& (TurnManager.Instance.CurrentActionTypeSelected == EntityActionEnumID.NeighborMove || TurnManager.Instance.CurrentActionTypeSelected == EntityActionEnumID.TargetTileMove)
				&& TurnManager.Instance.CurrentActionSelected.TileInteractPredicate(_tile);

			Tile from = GridManager.Instance.Tiles[TurnManager.Instance.GetLastRegisteredPositionOfEntity(m_selectedEntity.ID)];
			int distanceToTarget = isTargetValid ? GridManager.Instance.GetDistanceBetween(from, _tile) : 0; 
			TurnManager.Instance.RefreshActionDisplay(m_selectedEntity.ID
				, didContainTile ? totalCostSpend : (GameConfig.current.game.actionTokenPerRound - TurnManager.Instance.RemainingActionToken[m_selectedEntity.ID]) + distanceToTarget);
			if (isTargetValid)
			{
				TurnManager.Instance.CurrentActionSelected.positionAtActionEndID = _tile.coordinates.ID;
				TurnManager.Instance.CurrentActionSelected.GhostDisplay(TurnManager.Instance.CurrentStateTypeSelected);
			}



			m_hoveredTile = _tile;

		}
	}

	#region Ghost

	private void OnEndInputPhase ()
	{
		m_selectedEntity = null;
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();
		ClearGhostEntities();
	}

	private void OnEndLevel ()
	{
		m_selectedEntity = null;
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();
		ClearGhostEntities();
	}

	public void AddActionDisplay ( ActionDisplayOnTile _display, int _performingEntityID, bool _isTemp )
	{
		if (_isTemp)
		{
			if (!m_tempActionDisplays.ContainsKey(_performingEntityID))
				m_tempActionDisplays.Add(_performingEntityID, new());
			m_tempActionDisplays[_performingEntityID].Add(_display);
		}
		else
		{
			if (!m_actionDisplays.ContainsKey(_performingEntityID))
				m_actionDisplays.Add(_performingEntityID, new());
			m_actionDisplays[_performingEntityID].Add(_display);
		}
	}

	public void AddGhostAt ( Entity _entity, Tile _position, int _orientation )
	{
		if (!m_ghostEntities.ContainsKey(_entity.ID))
		{
			GhostEntity newGhost = Instantiate(GameAssets.current.game.baseGhost/*, GameManager.Instance.transform*/);
			newGhost.Init(_entity);
			m_ghostEntities.Add(_entity.ID, newGhost);
		}

		m_ghostEntities[_entity.ID].ShowAtPositionAndOrientation(_position, _orientation);
	}

	public void ClearGhostEntities ()
	{
		foreach (GhostEntity ghost in m_ghostEntities.Values)
		{
			ghost.Hide();
		}
	}

	public void ClearActionOnTileDisplay ()
	{
		foreach (int entityID in m_actionDisplays.Keys)
		{
			foreach (ActionDisplayOnTile display in m_actionDisplays[entityID])
			{
				display.Discard();
			}
			m_actionDisplays[entityID].Clear();
		}
	}

	public void ClearGhostActionOnTileDisplay ()
	{
		foreach (int entityID in m_tempActionDisplays.Keys)
		{
			foreach (ActionDisplayOnTile display in m_tempActionDisplays[entityID])
			{
				display.Discard();
			}
			m_tempActionDisplays[entityID].Clear();
		}
	}

	private void OnAnyEntityDeath(Entity _entity )
	{
		m_ghostEntities.Remove(_entity.ID);
	}

	#endregion
}
