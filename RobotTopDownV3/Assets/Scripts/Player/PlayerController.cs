using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Linq;

public class PlayerController : Singleton<PlayerController>
{
	public static Action<int?> onEntitySelected;

	[SerializeField] private TurnManager m_turnManager;
	[SerializeField] private FogOfWarRenderer m_fogRenderer;
	[SerializeField] private InputActionAsset m_inputActions;

	public InputActionAsset InputActions => m_inputActions;

	private InputAction m_moveAction;
	private InputAction m_rotateCWAction;
	private InputAction m_rotateCCWAction;
	private InputAction m_zoomAction;

	private const float ScrollTicksToLegacyAxis = 0.1f / 120f;

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


	private SerializableDictionary<int, List<ActionDisplayOnTile>> m_actionDisplays = new();
	private SerializableDictionary<int, List<ActionDisplayOnTile>> m_tempActionDisplays = new();
	private SerializableDictionary<int, List<RotationActionDisplay>> m_rotationActionDisplays = new();
	private SerializableDictionary<int, GhostEntity> m_ghostEntities = new();
	public SerializableDictionary<int, GhostEntity> GhostEntities => m_ghostEntities;
	private SerializableDictionary<int, GhostItem> m_ghostItems = new();

	public override void Awake ()
	{
		base.Awake();
		InputManager.onTileleftClick += OnTileLeftClick;
		InputManager.onTileRightClick += OnTileRightClick;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath += OnAnyEntityDeath;
		TurnManager.onEndLevel += OnEndLevel;

		InitInputActions();

		m_targetRotation = CameraManager.Instance.CameraParent.transform.rotation;
		m_currentZoomDistance = CameraManager.Instance.CameraParent.transform.position.y;
	}

	private void InitInputActions ()
	{
		string savedOverrides = GameDatas.current.app.inputBindingOverridesJson;
		if (!string.IsNullOrEmpty(savedOverrides))
			m_inputActions.LoadBindingOverridesFromJson(savedOverrides);

		InputActionMap playerMap = m_inputActions.FindActionMap("Player");

		m_moveAction = playerMap.FindAction("Move");
		m_rotateCWAction = playerMap.FindAction("RotateCameraCW");
		m_rotateCCWAction = playerMap.FindAction("RotateCameraCCW");
		m_zoomAction = playerMap.FindAction("ZoomCamera");

		playerMap.Enable();
	}

	public void SaveInputBindingOverrides ()
	{
		GameDatas.current.app.inputBindingOverridesJson = m_inputActions.SaveBindingOverridesAsJson();
		ApplicationManager.Instance.SaveApplication();
	}

	public void ResetInputBindingOverrides ()
	{
		foreach (InputActionMap map in m_inputActions.actionMaps)
			map.RemoveAllBindingOverrides();

		GameDatas.current.app.inputBindingOverridesJson = "";
		ApplicationManager.Instance.SaveApplication();
	}

	private void OnDestroy ()
	{
		InputManager.onTileleftClick -= OnTileLeftClick;
		InputManager.onTileRightClick -= OnTileRightClick;
		InputManager.onTileHovered -= OnTileHovered;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnAnyEntityDeath;
		TurnManager.onEndLevel -= OnEndLevel;

		m_inputActions.FindActionMap("Player")?.Disable();

		if (m_cameraRotationTween.IsActive())
			m_cameraRotationTween.Kill();
	}

	private void Update ()
	{
		if (!CanControlCamera())
			return;

		HandleCameraRotation();

		HandleCameraZoom();
	}

	private void FixedUpdate ()
	{
		if (!CanControlCamera())
			return;

		HandleCameraMovement();
	}

	private bool CanControlCamera ()
	{
		return m_turnManager.currentPhase != TurnManager.TurnPhase.Off
			&& UIManager.Instance.currentPanel is InGamePanel;
	}

	private void HandleCameraMovement ()
	{
		Vector2 moveInput = m_moveAction.ReadValue<Vector2>();
		Vector3 forward = CameraManager.Instance.CameraParent.transform.forward;
		Vector3 right = CameraManager.Instance.CameraParent.transform.right;

		forward.y = 0f;
		right.y = 0f;

		Vector3 move = (forward.normalized * moveInput.y + right.normalized * moveInput.x)
			* GameConfig.current.game.cameraMovementSpeed
			* Time.fixedDeltaTime;

		Vector3 targetPos = CameraManager.Instance.CameraParent.transform.position + move;

		targetPos.x = Mathf.Clamp(targetPos.x, xLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.x, xLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.x);
		targetPos.z = Mathf.Clamp(targetPos.z, zLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.y, zLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.y);

		CameraManager.Instance.CameraParent.transform.position = targetPos;
	}

	private void HandleCameraRotation ()
	{
		bool didInput = false;
		if (m_rotateCCWAction.WasPerformedThisFrame())
		{
			m_targetRotation *= Quaternion.Euler(0f, -GameConfig.current.game.cameraRotationStep, 0f);
			didInput = true;
		}
		else if (m_rotateCWAction.WasPerformedThisFrame())
		{
			m_targetRotation *= Quaternion.Euler(0f, GameConfig.current.game.cameraRotationStep, 0f);
			didInput = true;
		}

		if (!didInput)
			return;

		if (m_cameraRotationTween.IsActive())
			m_cameraRotationTween.Kill();

		m_cameraRotationTween = CameraManager.Instance.CameraParent.transform.DOLocalRotateQuaternion(m_targetRotation, GameConfig.current.game.cameraRotationDuration).SetEase(Ease.OutQuad);
		m_fogRenderer.MarkDirty();

	}

	private void HandleCameraZoom ()
	{
		float scroll = m_zoomAction.ReadValue<Vector2>().y * ScrollTicksToLegacyAxis;
		if (Mathf.Abs(scroll) < 0.001f)
			return;

		float zoomMovement = -(scroll * GameConfig.current.game.cameraZoomSpeed);

		m_currentZoomDistance += zoomMovement;
		m_currentZoomDistance = Mathf.Clamp(m_currentZoomDistance, GameConfig.current.game.cameraZoomBounds.x, GameConfig.current.game.cameraZoomBounds.y);

		CameraManager.Instance.CameraParent.transform.position = new Vector3(CameraManager.Instance.CameraParent.transform.position.x, m_currentZoomDistance, CameraManager.Instance.CameraParent.transform.position.z);
		if (zoomMovement != 0)
			m_fogRenderer.MarkDirty();

	}

	private void OnTileLeftClick ( Tile _tile )
	{
		if (m_turnManager.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		//Event => Select // unselect entity
		if (_tile.TryGetEntity(true, out Entity _entity) && !_tile.CanInteract)
		{
			//ally entity
			if (_entity.IsAlliedTo(PlayerID))
				SelectEntity(_entity);

			return;
		}

		//validate action
		if (m_selectedEntity != null)
		{
			if (_tile.CanInteract)
			{
				m_turnManager.CurrentActionSelected.RegisterInteraction(_tile);
				m_turnManager.CurrentActionSelected.OnSelectActionTileInteractPredicatePrewarm();
			}
		}
	}

	public void SelectEntity ( Entity _entity )
	{
		if (m_selectedEntity == _entity && _entity != null)
		{
			m_selectedEntity.Deselect();
			m_selectedEntity = null;
			onEntitySelected?.Invoke(null);
		}
		else if (m_selectedEntity == null)
		{
			m_selectedEntity = _entity;
			if (m_selectedEntity != null)
			{
				onEntitySelected?.Invoke(m_selectedEntity.ID);
				m_selectedEntity.Select();
			}
		}
		else
		{
			m_selectedEntity.Deselect();
			//onEntitySelected?.Invoke(null);
			m_selectedEntity = _entity;
			onEntitySelected?.Invoke(m_selectedEntity == null ? null : m_selectedEntity.ID);
			if(m_selectedEntity != null)
				m_selectedEntity.Select();
		}
	}

	private void OnTileRightClick ( Tile _tile )
	{
		if (m_turnManager.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		AEntityAction action = EntityActionDisplay.SelectedDisplay != null
			? (m_turnManager.hasModActionSelected ? EntityActionDisplay.SelectedDisplay.RecordedAction.freeAction : EntityActionDisplay.SelectedDisplay.RecordedAction.action)
			: (m_turnManager.hasModActionSelected ? m_turnManager.CurrentModActionSelected : m_turnManager.CurrentActionSelected);

		/*if (action != null && _tile != null && (EntityActionDisplay.SelectedDisplay != null ?
			(action.targetTileIDs != null && action.targetTileIDs.Contains(_tile.coordinates.ID))
			: (m_turnManager.CurrentActionTargetTiles != null && m_turnManager.CurrentActionTargetTiles.Contains(_tile))))
		{*/
		if (action != null && _tile != null && m_turnManager.CurrentActionTargetTiles != null && m_turnManager.CurrentActionTargetTiles.Contains(_tile))
		{
			/*if(EntityActionDisplay.SelectedDisplay != null)
			{
				for(int i = 0; i <action.targetTileIDs.Length; i++)
				{
					if(action.targetTileIDs[i] == _tile.coordinates.ID)
					{
						action.targetTileIDs[i] = -1;
						break;
					}
				}
			}
			else*/
			m_turnManager.CurrentActionTargetTiles.Remove(_tile);
		}
		else if (m_selectedEntity != null && m_actionDisplays.ContainsKey(m_selectedEntity.ID) && m_actionDisplays[m_selectedEntity.ID].Count > 0)
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
						List<TurnManager.RecordedAction> actionQueue = m_turnManager.RecordedActions[m_selectedEntity.ID].ToList();
						for (int i = 0; i < actionQueue.Count; i++)
						{
							if (actionQueue[i].action == display.RecordedAction.action)
							{
								m_turnManager.RemoveActionFrom(actionQueue[i], i);
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
		else if (m_selectedEntity != null)
		{
			if (!_tile.CanInteract && EntityActionDisplay.SelectedDisplay != null)
			{
				EntityActionDisplay.SelectedDisplay.Deselect();
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

	private void OnTileHovered ( Tile _tile )
	{
		if (m_selectedEntity == null || _tile == m_hoveredTile || EntityActionDisplay.SelectedDisplay != null || !_tile.CanInteract)
			return;

		m_hoveredTile = _tile;

		if (TurnManager.Instance.currentPhase == TurnManager.TurnPhase.Recording)
		{
			ClearGhostActionOnTileDisplay();
			int totalCostSpend = 0;
			bool didContainTile = false;
			
			if (m_actionDisplays.ContainsKey(m_selectedEntity.ID))
			{
				foreach (ActionDisplayOnTile display in m_actionDisplays[m_selectedEntity.ID])
				{
					if (display.DestinationTile == _tile)
					{
						totalCostSpend = display.RecordedAction.action.TimeAtEnd;
						didContainTile = true;
						break;
					}
				}
			}
			AEntityAction currentSelectedAction = EntityActionDisplay.SelectedDisplay != null
				? (m_turnManager.hasModActionSelected ? EntityActionDisplay.SelectedDisplay.RecordedAction.freeAction : EntityActionDisplay.SelectedDisplay.RecordedAction.action)
				: (m_turnManager.hasModActionSelected ? m_turnManager.CurrentModActionSelected : m_turnManager.CurrentActionSelected);

			if (!didContainTile)
				GridManager.Instance.BFS(GridManager.Instance.Tiles[m_turnManager.GetLastRegisteredPositionOfEntity(m_selectedEntity.ID)]
					, m_turnManager.RemainingActionToken[m_selectedEntity.ID] * currentSelectedAction.Data.movementSpeed, null, true, false);

			bool isTargetValid = m_turnManager.currentPhase == TurnManager.TurnPhase.Recording && _tile.CanInteract;
			int distanceToTarget = isTargetValid ? _tile.Distance : 0;
			int specificTokenCount = didContainTile ? totalCostSpend : (GameConfig.current.game.actionTokenPerRound - m_turnManager.RemainingActionToken[m_selectedEntity.ID]) + distanceToTarget;

			if (isTargetValid)
			{
				if (currentSelectedAction.Data.codeType == EntityActionData.ActionCodeType.MoveThenAttack || currentSelectedAction.Data.codeType == EntityActionData.ActionCodeType.TargetTileMove)
					currentSelectedAction.positionAtActionEndID = _tile.coordinates.ID;
			}

			m_turnManager.RefreshActionDisplay(m_selectedEntity.ID, false, specificTokenCount);
		}

	}

	#region Ghost

	private void OnEndInputPhase ()
	{
		SelectEntity(null);
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();
		ClearGhostEntitiesAndItems();
	}

	private void OnEndLevel ()
	{
		SelectEntity(null);
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();

		foreach (GhostEntity ghost in m_ghostEntities.Values)
		{
			Destroy(ghost.gameObject);
		}
		m_ghostEntities.Clear();
		foreach (GhostItem ghost in m_ghostItems.Values)
		{
			Destroy(ghost.gameObject);
		}
		m_ghostItems.Clear();
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

	public void AddRotationActionDisplay ( RotationActionDisplay _display, int _performingEntityID )
	{
		if (!m_rotationActionDisplays.ContainsKey(_performingEntityID))
			m_rotationActionDisplays.Add(_performingEntityID, new());
		m_rotationActionDisplays[_performingEntityID].Add(_display);
	}

	public void AddGhostEntityAt ( Entity _entity, Tile _position, int _orientation )
	{
		if (!m_ghostEntities.ContainsKey(_entity.ID))
		{
			GhostEntity newGhost = Instantiate(GameAssets.current.game.baseGhost/*, GameManager.Instance.transform*/);
			newGhost.Init(_entity);
			m_ghostEntities.Add(_entity.ID, newGhost);
		}

		m_ghostEntities[_entity.ID].ShowAtPositionAndOrientation(_position, _orientation);
	}

	public void AddGhostItemAt ( AItemData _itemData, Tile _position, int _orientation, int _id )
	{
		if (!m_ghostItems.ContainsKey(_id))
		{
			GhostItem newGhost = Instantiate(GameAssets.current.game.baseItem /*, GameManager.Instance.transform*/);
			newGhost.Init(_itemData);
			m_ghostItems.Add(_id, newGhost);
		}

		m_ghostItems[_id].ShowAtPositionAndOrientation(_position, _orientation);
	}

	public void ClearGhostEntitiesAndItems ()
	{
		foreach (GhostEntity ghost in m_ghostEntities.Values)
		{
			ghost.Hide();
		}

		foreach (GhostItem ghost in m_ghostItems.Values)
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

		foreach (int entityID in m_rotationActionDisplays.Keys)
		{
			foreach (RotationActionDisplay display in m_rotationActionDisplays[entityID])
			{
				display.Discard();
			}
			m_rotationActionDisplays[entityID].Clear();
		}
	}

	private void OnAnyEntityDeath ( Entity _entity )
	{
		m_ghostEntities.Remove(_entity.ID);
	}

	#endregion
}
