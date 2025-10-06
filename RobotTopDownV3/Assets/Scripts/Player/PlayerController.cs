using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

public class PlayerController : Singleton<PlayerController>
{
	public static Action<int?> onEntitySelected;

	[Title("Camera Settings")]
	[SerializeField] private GameObject playerCamera;
	[SerializeField] private float moveSpeed = 5f;

	[Header("Camera Limits")]
	[SerializeField] private Vector2 xLimits = new Vector2(-10f, 10f);
	[SerializeField] private Vector2 zLimits = new Vector2(-10f, 10f);


	private Tile m_selectedTile;
	private Tile m_hoveredTile;

	private Entity m_selectedEntity;
	public Entity SelectedEntity => m_selectedEntity;

	public List<Arrow> arrows = new();

	private void Start ()
	{
		InputManager.onTileSelected += OnTileSelected;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onEndInputPhase += OnEndInputPhase;
	}

	private void OnDestroy ()
	{
		InputManager.onTileSelected-= OnTileSelected;
		InputManager.onTileHovered -= OnTileHovered;
		TurnManager.onEndInputPhase -= OnEndInputPhase;
	}

	private void Update ()
	{
		HandleCameraMovement();
	}

	private void HandleCameraMovement ()
	{
		// Récupère les inputs (ZQSD)
		float moveX = 0f;
		float moveZ = 0f;

		if (Input.GetKey(KeyCode.W)) moveZ += 1f;
		if (Input.GetKey(KeyCode.S)) moveZ -= 1f;
		if (Input.GetKey(KeyCode.A)) moveX -= 1f;
		if (Input.GetKey(KeyCode.D)) moveX += 1f;

		Vector3 move = new Vector3(moveX, 0, moveZ).normalized * moveSpeed * Time.deltaTime;

		// Nouvelle position candidate
		Vector3 targetPos = playerCamera.transform.position + move;

		// Clamp dans les limites
		targetPos.x = Mathf.Clamp(targetPos.x, xLimits.x, xLimits.y);
		targetPos.z = Mathf.Clamp(targetPos.z, zLimits.x, zLimits.y);

		playerCamera.transform.position = targetPos;
	}

	private void OnTileSelected ( Tile _tile )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		//Event => Select // unselect entity
		if(_tile.GetEntity(true) != null && !_tile.CanInteract)
		{
			int playerId = !GameManager.Instance.IsOnline ? 0 : OnlinePlayerInstance.Self.connectionIndex;
			if (_tile.GetEntity(true).IsAlliedTo(playerId)) 
			{ 
				if(m_selectedEntity == _tile.GetEntity(true))
				{
					m_selectedEntity.Deselect();
					m_selectedEntity = null;
					onEntitySelected?.Invoke(null);
					return;
				}
				else if(m_selectedEntity == null)
				{
					m_selectedEntity = _tile.GetEntity(true);
					onEntitySelected?.Invoke(m_selectedEntity == null ? null : m_selectedEntity.ID);

					if(m_selectedEntity != null)
						m_selectedEntity.Select();
					return;
				}
			}
			
		}

		//validate action
		if(m_selectedEntity != null)
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
		ClearArrows();
	}

	public void ClearArrows ()
	{
		foreach(Arrow arrow in arrows)
		{
			arrow.Discard();
		}
		arrows.Clear();
	}
}
