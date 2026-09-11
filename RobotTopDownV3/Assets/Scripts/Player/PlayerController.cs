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
	private InputAction m_panAction;

	private bool m_isPanning;
	private Vector2 m_lastPanScreenPosition;

	[Header("Camera")]
	[SerializeField] private float m_edgeScrollMargin = 12f;
	[SerializeField] private float m_centerOnEntityDuration = .35f;

	private Tween m_cameraMoveTween;
	private bool m_areInteractableOutlinesHidden;

	private const float LegacyAxisPerNotch = 0.1f;

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
	public Tile HoveredTile => m_hoveredTile;

	private Entity m_selectedEntity;
	public Entity SelectedEntity => m_selectedEntity;

	private Color m_currentRangePreviewColor = Color.blue;
	private Color m_currentTargetColor = Color.blue;

	private readonly List<Tile> m_aoePreviewTiles = new();
	private readonly List<Tile> m_rangePreviewTiles = new();
	private readonly List<Tile> m_targetTiles = new();
	private readonly List<Tile> m_targetZoneTiles = new();


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
		InputManager.onEmptyRightClick += OnEmptyRightClick;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath += OnAnyEntityDeath;
		TurnManager.onEndLevel += OnEndLevel;
		GameDatas.onAfterLoad += ApplySavedInputBindings;

		InitInputActions();
	}

	private void Start ()
	{
		m_targetRotation = CameraManager.Instance.CameraParent.transform.rotation;
		m_currentZoomDistance = CameraManager.Instance.CameraParent.transform.position.y;
	}

	private void ApplySavedInputBindings ()
	{
		GameDatas datas = GameDatas.current;
		string savedOverrides = datas == null || datas.app == null ? null : datas.app.inputBindingOverridesJson;

		if (!string.IsNullOrEmpty(savedOverrides))
			m_inputActions.LoadBindingOverridesFromJson(savedOverrides);
	}

	private void InitInputActions ()
	{
		ApplySavedInputBindings();

		InputActionMap playerMap = m_inputActions.FindActionMap("Player");

		m_moveAction = playerMap.FindAction("Move");
		m_rotateCWAction = playerMap.FindAction("RotateCameraCW");
		m_rotateCCWAction = playerMap.FindAction("RotateCameraCCW");
		m_zoomAction = playerMap.FindAction("ZoomCamera");
		m_panAction = playerMap.FindAction("PanCamera");

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
		InputManager.onEmptyRightClick -= OnEmptyRightClick;
		InputManager.onTileHovered -= OnTileHovered;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnAnyEntityDeath;
		TurnManager.onEndLevel -= OnEndLevel;
		GameDatas.onAfterLoad -= ApplySavedInputBindings;

		m_inputActions.FindActionMap("Player")?.Disable();

		if (m_cameraRotationTween.IsActive())
			m_cameraRotationTween.Kill();

		if (m_cameraMoveTween.IsActive())
			m_cameraMoveTween.Kill();
	}

	private void Update ()
	{
		if (!CanControlCamera())
			return;

		HandleCameraRotation();

		HandleCameraZoom();

		HandleCameraPan();
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
		Vector2 moveInput = Vector2.ClampMagnitude(m_moveAction.ReadValue<Vector2>() + GetEdgeScrollInput(), 1f);
		if (moveInput.sqrMagnitude < .0001f)
			return;

		Vector3 forward = CameraManager.Instance.CameraParent.transform.forward;
		Vector3 right = CameraManager.Instance.CameraParent.transform.right;

		forward.y = 0f;
		right.y = 0f;

		MoveCameraBy((forward.normalized * moveInput.y + right.normalized * moveInput.x)
			* GameConfig.current.game.cameraMovementSpeed
			* Time.fixedDeltaTime);
	}

	private Vector2 GetEdgeScrollInput ()
	{
		if (m_edgeScrollMargin <= 0f || !Application.isFocused)
			return Vector2.zero;

		Vector2 mousePosition = Input.mousePosition;
		if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
			return Vector2.zero;

		Vector2 edgeInput = Vector2.zero;

		if (mousePosition.x <= m_edgeScrollMargin)
			edgeInput.x = -1f;
		else if (mousePosition.x >= Screen.width - m_edgeScrollMargin)
			edgeInput.x = 1f;

		if (mousePosition.y <= m_edgeScrollMargin)
			edgeInput.y = -1f;
		else if (mousePosition.y >= Screen.height - m_edgeScrollMargin)
			edgeInput.y = 1f;

		return UIManager.Instance.HaveAnActivePopup() ? Vector2.zero : edgeInput;
	}

	private void MoveCameraBy ( Vector3 _move )
	{
		if (m_cameraMoveTween.IsActive())
			m_cameraMoveTween.Kill();

		CameraManager.Instance.CameraParent.transform.position = ClampToCameraBounds(CameraManager.Instance.CameraParent.transform.position + _move);
		m_fogRenderer.MarkDirty();
	}

	private Vector3 ClampToCameraBounds ( Vector3 _position )
	{
		_position.x = Mathf.Clamp(_position.x, xLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.x, xLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.x);
		_position.z = Mathf.Clamp(_position.z, zLimits.x - GameConfig.current.game.cameraMovementBoundsOffset.y, zLimits.y + GameConfig.current.game.cameraMovementBoundsOffset.y);
		return _position;
	}

	private Vector3 GetCameraFocusOffset ( float _groundHeight )
	{
		Ray centerRay = CameraManager.Instance.Camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
		Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, _groundHeight, 0f));

		if (!groundPlane.Raycast(centerRay, out float distance))
			return Vector3.zero;

		Vector3 focusPoint = centerRay.GetPoint(distance);
		Vector3 parentPosition = CameraManager.Instance.CameraParent.transform.position;

		return new Vector3(focusPoint.x - parentPosition.x, 0f, focusPoint.z - parentPosition.z);
	}

	private void CenterCameraOn ( Entity _entity )
	{
		if (_entity == null || !CanControlCamera())
			return;

		Vector3 entityPosition = _entity.Displacement.Coordinates.GetTile().transform.position;
		Vector3 cameraPosition = CameraManager.Instance.CameraParent.transform.position;
		Vector3 focusOffset = GetCameraFocusOffset(entityPosition.y);
		Vector3 targetPosition = ClampToCameraBounds(new Vector3(entityPosition.x - focusOffset.x, cameraPosition.y, entityPosition.z - focusOffset.z));

		if (m_cameraMoveTween.IsActive())
			m_cameraMoveTween.Kill();

		m_cameraMoveTween = CameraManager.Instance.CameraParent.transform
			.DOMove(targetPosition, m_centerOnEntityDuration)
			.SetEase(Ease.OutQuad)
			.OnUpdate(m_fogRenderer.MarkDirty);
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

	public void ResetZoom ()
	{
		m_currentZoomDistance = Mathf.Clamp(CameraManager.Instance.DefaultZoomDistance
			, GameConfig.current.game.cameraZoomBounds.x, GameConfig.current.game.cameraZoomBounds.y);

		Vector3 cameraPosition = CameraManager.Instance.CameraParent.transform.position;
		CameraManager.Instance.CameraParent.transform.position = new Vector3(cameraPosition.x, m_currentZoomDistance, cameraPosition.z);

		m_fogRenderer.MarkDirty();
	}

	public void ResetRotation ()
	{
		if (m_cameraRotationTween.IsActive())
			m_cameraRotationTween.Kill();

		m_targetRotation = CameraManager.Instance.CameraParent.transform.rotation;
	}

	private void HandleCameraPan ()
	{
		if (m_panAction == null)
			return;

		if (m_panAction.WasPressedThisFrame())
		{
			m_isPanning = true;
			m_lastPanScreenPosition = Input.mousePosition;
			return;
		}

		if (!m_panAction.IsPressed())
		{
			m_isPanning = false;
			return;
		}

		if (!m_isPanning)
			return;

		Vector2 screenPosition = Input.mousePosition;
		Vector2 screenDelta = screenPosition - m_lastPanScreenPosition;
		m_lastPanScreenPosition = screenPosition;

		if (screenDelta.sqrMagnitude < .01f)
			return;

		Vector3 forward = CameraManager.Instance.CameraParent.transform.forward;
		Vector3 right = CameraManager.Instance.CameraParent.transform.right;

		forward.y = 0f;
		right.y = 0f;

		MoveCameraBy(-(right.normalized * screenDelta.x + forward.normalized * screenDelta.y) * GameConfig.current.game.cameraPanSpeed);
	}

	private void HandleCameraZoom ()
	{
		float rawScroll = m_zoomAction.ReadValue<Vector2>().y;
		if (Mathf.Abs(rawScroll) < 0.01f)
			return;

		if (InputManager.IsPointerOverBlockingUI())
			return;

		float scroll = Mathf.Sign(rawScroll) * LegacyAxisPerNotch;

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
				AEntityAction editedAction = m_turnManager.GetSelectedDisplayAction();

				if (editedAction != null && editedAction == m_turnManager.CurrentActionSelected)
				{
					m_turnManager.TryAddTargetTileOnSelectedDisplay(_tile);
					return;
				}

				if (m_turnManager.CurrentActionSelected.Data.DoesResolveItsOwnTarget())
					return;

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

		CenterCameraOn(m_selectedEntity);
	}

	private void OnEmptyRightClick ()
	{
		if (m_turnManager.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		SelectEntity(null);
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
			if (!m_turnManager.TryRemoveTargetTileOnSelectedDisplay(_tile))
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
		AEntityAction editedAction = m_turnManager.GetSelectedDisplayAction();
		bool isEditingTargets = editedAction != null && editedAction == m_turnManager.CurrentActionSelected;

		if (m_selectedEntity == null || _tile == m_hoveredTile || !_tile.CanInteract
			|| (EntityActionDisplay.SelectedDisplay != null && !isEditingTargets))
			return;

		m_hoveredTile = _tile;

		if (isEditingTargets)
		{
			if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
				return;

			if (m_turnManager.CanAddTargetOnSelectedDisplay(_tile))
				m_turnManager.RefreshActionDisplay(m_selectedEntity.ID, false, editedAction.TimeAtEnd);
			else
				ClearAoEPreviewOutlines();

			return;
		}

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
		SetInteractableOutlinesHidden(false);
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
			if(ghost != null)
				Destroy(ghost.gameObject);
		}
		m_ghostEntities.Clear();
		foreach (GhostItem ghost in m_ghostItems.Values)
		{
			if(ghost != null)
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

	public void AddAoEPreviewOutline ( Tile _tile )
	{
		if (_tile == null || m_aoePreviewTiles.Contains(_tile))
			return;

		m_aoePreviewTiles.Add(_tile);
		_tile.UI.SetOutlineColor(GameAssets.current.ui.aoePreviewColor);
	}

	public void SetRangePreviewOutlines ( IEnumerable<Tile> _tiles, EntityActionData.MainActionType _mainActionType )
	{
		ClearRangePreviewOutlines();

		m_currentRangePreviewColor = GameAssets.current.ui.GetActionRangeColor(_mainActionType, true);

		foreach (Tile tile in _tiles)
		{
			if (tile == null || m_rangePreviewTiles.Contains(tile))
				continue;

			m_rangePreviewTiles.Add(tile);
			tile.UI.SetOutlineColor(m_currentRangePreviewColor);
		}
	}

	public void ClearRangePreviewOutlines ()
	{
		if (m_rangePreviewTiles.Count == 0)
			return;

		RestoreInteractableOutlines(m_rangePreviewTiles);
		m_rangePreviewTiles.Clear();
		RedrawOutlines(m_aoePreviewTiles, GameAssets.current.ui.aoePreviewColor);
		RedrawOutlines(m_targetTiles, m_currentTargetColor);
	}

	public void ClearAoEPreviewOutlines ()
	{
		if (m_aoePreviewTiles.Count > 0)
		{
			RestoreInteractableOutlines(m_aoePreviewTiles);
			m_aoePreviewTiles.Clear();
		}

		RedrawOutlines(m_rangePreviewTiles, m_currentRangePreviewColor);
		RedrawOutlines(m_targetTiles, m_currentTargetColor);
	}

	public void SetTargetOutlines ( IEnumerable<Tile> _targetTiles, IEnumerable<Tile> _zoneTiles, EntityActionData.MainActionType _mainActionType )
	{
		ClearTargetOutlines();

		m_currentTargetColor = GameAssets.current.ui.GetActionRangeColor(_mainActionType, true);
		m_targetTiles.AddRange(_targetTiles);
		m_targetZoneTiles.AddRange(_zoneTiles);

		RedrawTargetOutlines();
	}

	public void RedrawTargetOutlines ()
	{
		if (m_targetTiles.Count == 0 && m_targetZoneTiles.Count == 0)
			return;

		foreach (Tile tile in m_targetZoneTiles)
		{
			if (tile != null)
				tile.UI.SetOutlineColor(m_currentTargetColor);
		}

		Dictionary<Tile, string> indexesPerTile = new();

		for (int i = 0; i < m_targetTiles.Count; i++)
		{
			Tile tile = m_targetTiles[i];
			if (tile == null)
				continue;

			indexesPerTile[tile] = indexesPerTile.ContainsKey(tile)
				? indexesPerTile[tile] + ", " + (i + 1)
				: (i + 1).ToString();
		}

		foreach (KeyValuePair<Tile, string> pair in indexesPerTile)
		{
			pair.Key.UI.SetOutlineColor(m_currentTargetColor);
			pair.Key.UI.SetTargetIndexText(pair.Value);
		}
	}

	public void ClearTargetOutlines ()
	{
		if (m_targetTiles.Count == 0 && m_targetZoneTiles.Count == 0)
			return;

		foreach (Tile tile in m_targetTiles)
		{
			if (tile != null)
				tile.UI.ClearPositionText();
		}

		RestoreInteractableOutlines(m_targetTiles);
		RestoreInteractableOutlines(m_targetZoneTiles);
		m_targetTiles.Clear();
		m_targetZoneTiles.Clear();
		RedrawOutlines(m_rangePreviewTiles, m_currentRangePreviewColor);
		RedrawOutlines(m_aoePreviewTiles, GameAssets.current.ui.aoePreviewColor);
	}

	public void ClearHoverPreviews ()
	{
		m_hoveredTile = null;
		ClearAoEPreviewOutlines();
	}

	public bool AreInteractableOutlinesHidden => m_areInteractableOutlinesHidden;

	public void SetInteractableOutlinesHidden ( bool _hidden )
	{
		if (m_areInteractableOutlinesHidden == _hidden || GridManager.Instance == null)
			return;

		m_areInteractableOutlinesHidden = _hidden;

		RestoreInteractableOutlines(GridManager.Instance.Tiles);
		RedrawOutlines(m_rangePreviewTiles, m_currentRangePreviewColor);
		RedrawOutlines(m_aoePreviewTiles, GameAssets.current.ui.aoePreviewColor);
		RedrawTargetOutlines();
	}

	private void RestoreInteractableOutlines ( IEnumerable<Tile> _tiles )
	{
		Color interactableColor = m_turnManager.CurrentActionSelected != null
			? GameAssets.current.ui.GetActionRangeColor(m_turnManager.CurrentActionSelected.Data.GetMainActionType(), false)
			: GameAssets.current.ui.movementRangeColor;

		foreach (Tile tile in _tiles)
			tile.UI.SetAsInteractable(!m_areInteractableOutlinesHidden && tile.CanInteract, interactableColor);
	}

	private void RedrawOutlines ( List<Tile> _tiles, Color _color )
	{
		foreach (Tile tile in _tiles)
			tile.UI.SetOutlineColor(_color);
	}

	private void OnAnyEntityDeath ( Entity _entity )
	{
		m_ghostEntities.Remove(_entity.ID);

		if (m_selectedEntity == _entity)
			SelectEntity(null);
	}

	#endregion
}
