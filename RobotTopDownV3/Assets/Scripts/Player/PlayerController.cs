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
	public static Action<int?> onEntityHovered;

	[SerializeField] private TurnManager m_turnManager;
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
	[SerializeField, Range(0f, .2f)] private float m_edgeScrollScreenRatio = .03f;
	[SerializeField] private float m_centerOnEntityDuration = .35f;

	private Tween m_cameraMoveTween;
	private bool m_areInteractableOutlinesHidden;

	private const float LegacyAxisPerNotch = 0.1f;

	private int PlayerID => !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;

	private Tween m_cameraRotationTween;

	private Quaternion m_targetRotation;
	private float m_currentZoomDistance;

	private Tile m_hoveredTile;
	public Tile HoveredTile => m_hoveredTile;
	private Entity m_hoveredEntity;
	public Entity HoveredEntity => m_hoveredEntity;

	private Entity m_selectedEntity;
	public Entity SelectedEntity => m_selectedEntity;

	private Color m_currentTargetColor = Color.blue;

	private readonly List<Tile> m_aoePreviewTiles = new();
	private readonly List<Tile> m_targetTiles = new();
	private readonly List<Tile> m_targetZoneTiles = new();

	private Tile m_designationHoverTile;
	private Entity m_designationHoveredEnemy;
	private Item m_designationHoveredItem;
	private bool m_isShowingDesignationPreview;


	private SerializableDictionary<int, List<ActionDisplayOnTile>> m_actionDisplays = new();
	private SerializableDictionary<int, List<ActionDisplayOnTile>> m_tempActionDisplays = new();
	private SerializableDictionary<int, List<GroundDisplay>> m_orderDisplays = new();
	private SerializableDictionary<int, GhostEntity> m_ghostEntities = new();
	private SerializableDictionary<int, GhostItem> m_ghostItems = new();

	public override void Awake ()
	{
		base.Awake();
		InputManager.onTileleftClick += OnTileLeftClick;
		InputManager.onTileRightClick += OnTileRightClick;
		InputManager.onEmptyRightClick += OnEmptyRightClick;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onStartInputPhase += ShowAITargets;
		TurnManager.onEndInputPhase += OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath += OnAnyEntityDeath;
		TurnManager.onEndLevel += OnEndLevel;
		GameDatas.onAfterLoad += ApplySavedInputBindings;
		TurnManager.onActionSelected += OnActionSelected;
		TurnManager.onActionAdded += OnActionAddedOrRemoved;
		TurnManager.onActionRemoved += OnActionAddedOrRemoved;
		onEntitySelected += OnEntitySelected;
		EntityActionDisplay.onSelect += OnActionDisplaySelected;

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
		TurnManager.onStartInputPhase -= ShowAITargets;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
		EntityEquipmentPlugin.onAnyEntityDeath -= OnAnyEntityDeath;
		TurnManager.onEndLevel -= OnEndLevel;
		GameDatas.onAfterLoad -= ApplySavedInputBindings;
		TurnManager.onActionSelected -= OnActionSelected;
		TurnManager.onActionAdded -= OnActionAddedOrRemoved;
		TurnManager.onActionRemoved -= OnActionAddedOrRemoved;
		onEntitySelected -= OnEntitySelected;
		EntityActionDisplay.onSelect -= OnActionDisplaySelected;

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
			&& UIManager.Instance.currentPanel is InGamePanel
			&& !UIManager.Instance.HaveAnActivePopup();
	}

	private void HandleCameraMovement ()
	{
		Vector2 keyboardInput = m_moveAction.ReadValue<Vector2>();
		if (keyboardInput.sqrMagnitude >= .0001f)
			CameraManager.Instance.StopFollowingUnits();

		if (CameraManager.Instance.IsFollowingUnits)
			return;

		Vector2 moveInput = Vector2.ClampMagnitude(keyboardInput + GetEdgeScrollInput(), 1f);
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
		if (m_edgeScrollScreenRatio <= 0f || !Application.isFocused)
			return Vector2.zero;

		Vector2 mousePosition = Input.mousePosition;
		if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
			return Vector2.zero;

		float margin = Screen.height * m_edgeScrollScreenRatio;
		Vector2 edgeInput = Vector2.zero;

		if (mousePosition.x <= margin)
			edgeInput.x = -1f;
		else if (mousePosition.x >= Screen.width - margin)
			edgeInput.x = 1f;

		if (mousePosition.y <= margin)
			edgeInput.y = -1f;
		else if (mousePosition.y >= Screen.height - margin)
			edgeInput.y = 1f;

		return edgeInput;
	}

	private void MoveCameraBy ( Vector3 _move )
	{
		if (m_cameraMoveTween.IsActive())
			m_cameraMoveTween.Kill();

		CameraManager.Instance.CameraParent.transform.position = CameraManager.Instance.ClampToBounds(CameraManager.Instance.CameraParent.transform.position + _move);
	}

	private void CenterCameraOn ( Entity _entity )
	{
		if (_entity == null || !CanControlCamera())
			return;

		Vector3 entityPosition = _entity.Displacement.Coordinates.GetTile().transform.position;
		Vector3 cameraPosition = CameraManager.Instance.CameraParent.transform.position;
		Vector3 focusOffset = CameraManager.Instance.GetFocusOffset(entityPosition.y);
		Vector3 targetPosition = CameraManager.Instance.ClampToBounds(new Vector3(entityPosition.x - focusOffset.x, cameraPosition.y, entityPosition.z - focusOffset.z));

		if (m_cameraMoveTween.IsActive())
			m_cameraMoveTween.Kill();

		m_cameraMoveTween = CameraManager.Instance.CameraParent.transform
			.DOMove(targetPosition, m_centerOnEntityDuration)
			.SetEase(Ease.OutQuad);
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
	}

	public void ResetZoom ()
	{
		m_currentZoomDistance = Mathf.Clamp(CameraManager.Instance.DefaultZoomDistance
			, GameConfig.current.game.cameraZoomBounds.x, GameConfig.current.game.cameraZoomBounds.y);

		Vector3 cameraPosition = CameraManager.Instance.CameraParent.transform.position;
		CameraManager.Instance.CameraParent.transform.position = new Vector3(cameraPosition.x, m_currentZoomDistance, cameraPosition.z);
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

		CameraManager.Instance.StopFollowingUnits();
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
	}

	private void OnTileLeftClick ( Tile _tile )
	{
		if (m_turnManager.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		if (m_turnManager.TryToggleDesignatedTarget(_tile))
			return;

		if (m_turnManager.IsFacingPickerOpen)
		{
			if (!m_turnManager.TryValidateFacing(_tile))
				m_turnManager.CloseFacingPicker();
			return;
		}

		if (m_selectedEntity != null && !_tile.CanInteract && m_turnManager.TryOpenFacingPickerAt(m_selectedEntity, _tile))
			return;

		if (!_tile.CanInteract && GameManager.Instance.PlayersEntityAnchor[PlayerID].TryGetPendingInvocationOn(_tile, out Entity pending))
		{
			SelectEntity(pending);
			return;
		}

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

		if (m_turnManager.TryRemoveDesignatedTarget(_tile))
			return;

		if (m_turnManager.IsFacingPickerOpen)
		{
			m_turnManager.CloseFacingPicker();
			return;
		}

		AEntityAction action = EntityActionDisplay.SelectedDisplay != null
			? (m_turnManager.hasModActionSelected ? EntityActionDisplay.SelectedDisplay.RecordedAction.freeAction : EntityActionDisplay.SelectedDisplay.RecordedAction.action)
			: m_turnManager.CurrentActionSelected;

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

	private void RefreshHoveredEntity ( Tile _tile )
	{
		Entity hoveredEntity = _tile == null || m_turnManager.currentPhase == TurnManager.TurnPhase.Off
			? null : _tile.GetCurrentEntity();

		if (hoveredEntity != null && (hoveredEntity.Equipment.IsDead
			|| (!hoveredEntity.IsAlliedTo(PlayerID) && !hoveredEntity.IsVisible)))
			hoveredEntity = null;

		if (hoveredEntity == m_hoveredEntity)
			return;

		m_hoveredEntity = hoveredEntity;
		onEntityHovered?.Invoke(hoveredEntity == null ? null : hoveredEntity.ID);
	}

	private void OnTileHovered ( Tile _tile )
	{
		RefreshHoveredEntity(_tile);

		if (m_turnManager.IsFacingPickerOpen)
		{
			if (_tile != m_hoveredTile)
			{
				m_hoveredTile = _tile;
				m_turnManager.RefreshFacingPicker(_tile);
			}
			return;
		}

		RefreshDesignationHover(_tile, false);

		if (EntityActionDisplay.SelectedDisplay == null && (_tile == null || !_tile.CanInteract))
			RangeOverlay.ClearVisionPreview();

		AEntityAction editedAction = m_turnManager.GetSelectedDisplayAction();
		bool isEditingTargets = editedAction != null && editedAction == m_turnManager.CurrentActionSelected;

		if (_tile == null || m_selectedEntity == null || _tile == m_hoveredTile || !_tile.CanInteract
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
				: m_turnManager.CurrentActionSelected;

			if (currentSelectedAction == null)
				return;

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

			if (isTargetValid && currentSelectedAction.Data.MovesItsUnit)
				ShowVisionPreview(m_selectedEntity, _tile);
			else
				RangeOverlay.ClearVisionPreview();
		}

	}

	#region Ghost

	private void OnEndInputPhase ()
	{
		if (m_cameraMoveTween.IsActive())
			m_cameraMoveTween.Kill();

		SetInteractableOutlinesHidden(false);
		SelectEntity(null);
		ClearAllOutlines();
		HideAITargets();
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();
		ClearGhostEntitiesAndItems();
	}

	private void OnEndLevel ()
	{
		RefreshHoveredEntity(null);
		SelectEntity(null);
		ClearAllOutlines();
		HideAITargets();
		ClearActionOnTileDisplay();
		ClearGhostActionOnTileDisplay();
		ClearGhosts();
	}

	public void ClearGhosts ()
	{
		foreach (GhostEntity ghost in m_ghostEntities.Values)
			if (ghost != null)
				Destroy(ghost.gameObject);
		m_ghostEntities.Clear();

		foreach (GhostItem ghost in m_ghostItems.Values)
			if (ghost != null)
				Destroy(ghost.gameObject);
		m_ghostItems.Clear();
	}

	public void AddActionDisplay ( ActionDisplayOnTile _display, int _performingEntityID, bool _isTemp )
	{
		SerializableDictionary<int, List<ActionDisplayOnTile>> displays = _isTemp ? m_tempActionDisplays : m_actionDisplays;
		if (!displays.TryGetValue(_performingEntityID, out List<ActionDisplayOnTile> entityDisplays))
		{
			entityDisplays = new();
			displays.Add(_performingEntityID, entityDisplays);
		}

		if (entityDisplays.Count > 0)
			entityDisplays[^1].LinkTo(_display);
		entityDisplays.Add(_display);
	}

	public void AddOrderDisplay ( GroundDisplay _display, int _performingEntityID )
	{
		if (!m_orderDisplays.ContainsKey(_performingEntityID))
			m_orderDisplays.Add(_performingEntityID, new());
		m_orderDisplays[_performingEntityID].Add(_display);
	}

	public void AddGhostEntityAt ( Entity _entity, Tile _position, int _orientation )
	{
		if (_position == _entity.Displacement.Coordinates.GetTile() && _orientation == _entity.Displacement.CurrentOrientation)
		{
			if (m_ghostEntities.TryGetValue(_entity.ID, out GhostEntity shownGhost) && shownGhost != null && shownGhost.gameObject.activeSelf)
				shownGhost.Hide();
			return;
		}

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
		foreach (int entityID in new List<int>(m_ghostEntities.Keys))
		{
			if (m_ghostEntities[entityID] == null)
				m_ghostEntities.Remove(entityID);
			else
				m_ghostEntities[entityID].Hide();
		}

		foreach (int itemID in new List<int>(m_ghostItems.Keys))
		{
			if (m_ghostItems[itemID] == null)
				m_ghostItems.Remove(itemID);
			else
				m_ghostItems[itemID].Hide();
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

		foreach (int entityID in m_orderDisplays.Keys)
		{
			foreach (GroundDisplay display in m_orderDisplays[entityID])
			{
				display.Discard();
			}
			m_orderDisplays[entityID].Clear();
		}
	}

	public void AddAoEPreviewOutline ( Tile _tile )
	{
		if (_tile == null || m_aoePreviewTiles.Contains(_tile))
			return;

		m_aoePreviewTiles.Add(_tile);
		RangeOverlay.SetAoEPreview(m_aoePreviewTiles, GameAssets.current.ui.aoePreviewColor);
	}

	private void OnActionSelected ( AEntityAction _action )
	{
		RangeOverlay.ClearVisionPreview();

		if (_action == null || GridManager.Instance == null)
		{
			RangeOverlay.ClearRange();
			return;
		}

		IEnumerable<Tile> rangeTiles = _action.Data.DoesResolveItsOwnTarget() && _action.PerformingEntity != null
			? _action.PerformingEntity.Equipment.GetTilesInReach(_action, GridManager.Instance.Tiles[_action.supposedPositionAtActionStartID], false, true)
			: GridManager.Instance.Tiles.Where(_action.TileInteractPredicate);

		EntityActionData.MainActionType colorType = _action is RotateEntityAction
			? EntityActionData.MainActionType.Attack : _action.Data.GetMainActionType();
		RangeOverlay.SetRange(rangeTiles, GameAssets.current.ui.GetActionRangeColor(colorType, false));
	}

	public void ShowFacingPicker ( Entity _entity, Tile _pivot, int _fromOrientation, int _shownOrientation, HashSet<Tile> _coneTiles = null )
	{
		AddGhostEntityAt(_entity, _pivot, _shownOrientation);

		RotationActionDisplay.Show(_entity.ID, _pivot, _fromOrientation, _shownOrientation, false);
		for (int orientation = 0; orientation < 6; orientation++)
		{
			if (orientation != _shownOrientation && orientation != _fromOrientation && _pivot.GetNeighbor((HexDirection)orientation) != null)
				RotationActionDisplay.Show(_entity.ID, _pivot, orientation, orientation, true);
		}

		ShowFacingConePreview(_entity, _pivot, _shownOrientation, _coneTiles);
	}

	public void ShowFacingConePreview ( Entity _entity, Tile _pivot, int _orientation, HashSet<Tile> _coneTiles = null )
	{
		HashSet<Tile> coneTiles = _coneTiles ?? (_entity == null ? null : _entity.Equipment.GetWeaponConeTilesAt(_pivot, _orientation));
		if (coneTiles == null || coneTiles.Count == 0)
		{
			RangeOverlay.ClearPreview();
			return;
		}

		RangeOverlay.SetPreview(coneTiles, GameAssets.current.ui.GetActionRangeColor(EntityActionData.MainActionType.Attack, true));
	}

	private void OnActionAddedOrRemoved ( TurnManager.RecordedAction _recordedAction )
	{
		RangeOverlay.ClearRange();
		RangeOverlay.ClearVisionPreview();
	}

	private void OnActionDisplaySelected ( EntityActionDisplay _display, bool _isModAction )
	{
		AEntityAction action = _display == null || _isModAction ? null : _display.RecordedAction.action;
		if (action == null || !action.Data.MovesItsUnit || action.positionAtActionEndID < 0)
		{
			RangeOverlay.ClearVisionPreview();
			return;
		}

		ShowVisionPreview(action.PerformingEntity, GridManager.Instance.Tiles[action.positionAtActionEndID]);
	}

	private void ShowVisionPreview ( Entity _entity, Tile _from )
	{
		if (_entity == null || _from == null || GridManager.Instance == null || _entity.Data.NeuronalMembraneData == null)
		{
			RangeOverlay.ClearVisionPreview();
			return;
		}

		RangeOverlay.SetVisionPreview(GridManager.Instance.GetVisionTilesOf(_entity, _from), _entity.Data.NeuronalMembraneData.visionType, _from);
	}

	private void OnEntitySelected ( int? _entityID )
	{
		if (!_entityID.HasValue)
			ClearAllOutlines();
	}

	private void ClearAllOutlines ()
	{
		ResetDesignationHover();
		ClearTargetOutlines();
		m_aoePreviewTiles.Clear();
		RangeOverlay.ClearAll();
		ShieldMarker.ClearAim();
		FacingMarker.ClearAim();
		TargetLaser.HideAll();
	}

	public void RefreshAimedTargets ( AEntityAction _hoveredAction )
	{
		ShieldMarker.ClearAim();
		FacingMarker.ClearAim();
		if (_hoveredAction == null || m_hoveredTile == null || GridManager.Instance == null
			|| _hoveredAction.Data.GetMainActionType() != EntityActionData.MainActionType.Attack)
			return;

		Entity shooter = _hoveredAction.PerformingEntity;
		if (shooter == null)
			return;

		Tile shooterTile = GridManager.Instance.Tiles[m_turnManager.GetLastRegisteredPositionOfEntity(_hoveredAction.performingEntityID)];
		Tile shotOrigin = shooter.Equipment.GetShotOrigin(_hoveredAction, shooterTile, m_hoveredTile);
		if (m_aoePreviewTiles.Count == 0)
			AimOn(m_hoveredTile, shotOrigin);
		else
		{
			foreach (Tile tile in m_aoePreviewTiles)
				AimOn(tile, shotOrigin);
		}
	}

	private static void AimOn ( Tile _tile, Tile _shotOrigin )
	{
		if (!_tile.TryGetEntity(true, out Entity target))
			return;

		int incomingSide = GridManager.Instance.GetIncomingSide(_shotOrigin, _tile);
		FacingMarker.Aim(target, incomingSide);
		foreach (Shield shield in target.Equipment.GetShieldsCovering(incomingSide))
			shield.SetAimed(true);
	}

	public void RefreshDesignationHover ()
	{
		RefreshDesignationHover(m_designationHoverTile, true);
	}

	private void RefreshDesignationHover ( Tile _tile, bool _force )
	{
		m_designationHoverTile = _tile;
		AEntityAction focus = m_turnManager.GetDesignationFocusAction();
		bool canHover = focus != null && !InputManager.IsPointerOverBlockingUI();
		Entity candidate = canHover && m_turnManager.TryGetDesignableEnemyOn(_tile, focus, out Entity enemy) ? enemy : null;
		Item itemCandidate = canHover && candidate == null && m_turnManager.TryGetDesignableItemOn(_tile, focus, out Item item) ? item : null;

		if (candidate == m_designationHoveredEnemy && itemCandidate == m_designationHoveredItem && !_force)
			return;

		ResetDesignationHover();
		m_designationHoveredEnemy = candidate;
		m_designationHoveredItem = itemCandidate;
		if (candidate != null || itemCandidate != null)
		{
			m_isShowingDesignationPreview = true;
			SetRangePreviewOutlines(m_turnManager.GetDesignationReach(focus), focus.Data.GetMainActionType());

			bool isDesignated = candidate != null ? focus.IsDesignated(candidate.ID) : focus.IsDesignated(itemCandidate);
			string targetName = candidate != null ? candidate.Data.name : itemCandidate.Data.GetLocalizedName();
			string hint = isDesignated
				? LocalizationManager.Instance.Get(LocalizationKey.action_desc_undesignate_click)
				: string.Format(LocalizationManager.Instance.Get(LocalizationKey.action_desc_designate_click), targetName);
			ToolTipManager.Instance.Show(focus.Data.GetLocalizedName(), hint, GetDesignationHoverOwner());
		}

		RefreshTargetMarkers();
	}

	private GameObject GetDesignationHoverOwner ()
	{
		if (m_designationHoveredEnemy != null)
			return m_designationHoveredEnemy.UI.gameObject;

		if (m_designationHoveredItem == null)
			return null;

		return m_designationHoveredItem.UI != null ? m_designationHoveredItem.UI.gameObject : m_designationHoveredItem.gameObject;
	}

	private bool ResetDesignationHover ()
	{
		GameObject hoverOwner = GetDesignationHoverOwner();
		if (m_isShowingDesignationPreview)
			ClearRangePreviewOutlines();

		if (hoverOwner != null && ToolTipManager.Instance.IsShownFor(hoverOwner))
			ToolTipManager.Instance.Hide();

		m_isShowingDesignationPreview = false;
		m_designationHoveredEnemy = null;
		m_designationHoveredItem = null;
		return hoverOwner != null;
	}

	public void RefreshTargetMarkers ()
	{
		TargetLaser.HideAll();
		if (GameManager.Instance == null || GameManager.Instance.PlayersEntityAnchor == null)
			return;

		AEntityAction focus = null;
		List<Entity> candidates = null;
		List<Item> itemCandidates = null;
		HashSet<int> designated = new();
		HashSet<Item> designatedItems = new();
		if (m_turnManager.currentPhase == TurnManager.TurnPhase.Recording && m_selectedEntity != null)
		{
			focus = m_turnManager.GetDesignationFocusAction();
			if (focus != null)
			{
				candidates = m_turnManager.GetDesignableEnemies(focus);
				itemCandidates = m_turnManager.GetDesignableItems(focus);
				ShowLasersOf(focus);
				if (m_designationHoveredEnemy != null && !focus.IsDesignated(m_designationHoveredEnemy.ID))
					TargetLaser.Show(focus, m_designationHoveredEnemy, true);
				if (m_designationHoveredItem != null && !focus.IsDesignated(m_designationHoveredItem))
					TargetLaser.Show(focus, m_designationHoveredItem, true);
			}
			else if (m_turnManager.RecordedActions.TryGetValue(m_selectedEntity.ID, out Queue<TurnManager.RecordedAction> queue) && queue != null)
			{
				foreach (TurnManager.RecordedAction recordedAction in queue)
				{
					if (!recordedAction.action.HasDesignatedTargets)
						continue;

					designated.UnionWith(recordedAction.action.designatedEntityIDs);
					if (recordedAction.action.DesignatedItem != null)
						designatedItems.Add(recordedAction.action.DesignatedItem);
					ShowLasersOf(recordedAction.action);
				}
			}
		}

		foreach (Item item in GameManager.Instance.Items)
		{
			if (item == null || item.UI == null || !item.Data.isTargetable)
				continue;

			TargetMarker.State state = TargetMarker.State.None;
			if (focus != null ? focus.IsDesignated(item) : designatedItems.Contains(item))
				state = TargetMarker.State.Designated;
			else if (item == m_designationHoveredItem)
				state = TargetMarker.State.Preview;
			else if (itemCandidates != null && itemCandidates.Contains(item))
				state = TargetMarker.State.Candidate;

			item.UI.SetTargetMarker(state);
		}

		foreach (Entity entity in GetAllEntities())
		{
			TargetMarker.State state = TargetMarker.State.None;
			if (focus != null ? focus.IsDesignated(entity.ID) : designated.Contains(entity.ID))
				state = TargetMarker.State.Designated;
			else if (entity == m_designationHoveredEnemy)
				state = TargetMarker.State.Preview;
			else if (candidates != null && candidates.Contains(entity))
				state = TargetMarker.State.Candidate;

			entity.UI.SetTargetMarker(state);
		}
	}

	private void ShowAITargets ()
	{
		HideAITargets();
		if (GameManager.Instance == null || GameManager.Instance.PlayersEntityAnchor == null)
			return;

		Dictionary<Entity, int> allyCountPerTarget = new();
		foreach (Entity ally in GetAllEntities())
		{
			if (ally.OwnerID != GameManager.Instance.PlayerID || ally.Equipment.IsDead || ally.AI == null)
				continue;

			Entity target = ally.AI.GetStickyTarget();
			if (target == null || target.Equipment.IsDead || !target.IsShownToLocalPlayer)
				continue;

			TargetLaser.ShowAITarget(ally, target);
			allyCountPerTarget[target] = allyCountPerTarget.TryGetValue(target, out int count) ? count + 1 : 1;
		}

		foreach (KeyValuePair<Entity, int> pair in allyCountPerTarget)
			pair.Key.UI.SetAITargetCount(pair.Value);
	}

	private void HideAITargets ()
	{
		TargetLaser.HideAITargets();
		if (GameManager.Instance == null || GameManager.Instance.PlayersEntityAnchor == null)
			return;

		foreach (Entity entity in GetAllEntities())
			entity.UI.SetAITargetCount(0);
	}

	private static IEnumerable<Entity> GetAllEntities ()
	{
		foreach (EntityAnchor anchor in GameManager.Instance.PlayersEntityAnchor)
		{
			if (anchor == null)
				continue;

			foreach (Entity entity in anchor.Entities)
				if (entity != null && entity.UI != null)
					yield return entity;
		}
	}

	private static void ShowLasersOf ( AEntityAction _action )
	{
		foreach (int entityID in _action.designatedEntityIDs)
			TargetLaser.Show(_action, GameManager.Instance.GetEntityFromID(entityID), false);
		TargetLaser.Show(_action, _action.DesignatedItem, false);
	}

	public void SetRangePreviewOutlines ( IEnumerable<Tile> _tiles, EntityActionData.MainActionType _mainActionType )
	{
		RangeOverlay.SetPreview(_tiles, GameAssets.current.ui.GetActionRangeColor(_mainActionType, true));
	}

	public void ClearRangePreviewOutlines ()
	{
		RangeOverlay.ClearPreview();
	}

	public void ClearAoEPreviewOutlines ()
	{
		m_aoePreviewTiles.Clear();
		RangeOverlay.ClearAoEPreview();
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

		RangeOverlay.SetTargets(m_targetTiles.Concat(m_targetZoneTiles), m_currentTargetColor);

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
			pair.Key.UI.SetTargetIndexText(pair.Value);
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

		m_targetTiles.Clear();
		m_targetZoneTiles.Clear();
		RangeOverlay.ClearTargets();
	}

	public void ClearHoverPreviews ()
	{
		m_hoveredTile = null;
		ClearAoEPreviewOutlines();
		if (ResetDesignationHover())
			RefreshTargetMarkers();
	}

	public bool AreInteractableOutlinesHidden => m_areInteractableOutlinesHidden;

	public void SetInteractableOutlinesHidden ( bool _hidden )
	{
		if (m_areInteractableOutlinesHidden == _hidden)
			return;

		m_areInteractableOutlinesHidden = _hidden;
		RangeOverlay.SetRangeHidden(_hidden);
	}

	private void OnAnyEntityDeath ( Entity _entity )
	{
		if (m_ghostEntities.ContainsKey(_entity.ID))
		{
			if (m_ghostEntities[_entity.ID] != null)
				Destroy(m_ghostEntities[_entity.ID].gameObject);
			m_ghostEntities.Remove(_entity.ID);
		}

		if (m_selectedEntity == _entity)
			SelectEntity(null);
	}

	#endregion
}
