using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerController : Singleton<PlayerController>
{
	public static Action<Entity> onEntitySelected;

	private Tile m_selectedTile;
	private Tile m_hoveredTile;

	private Entity m_selectedEntity;
	public Entity SelectedEntity => m_selectedEntity;

	public List<Arrow> arrows = new();

	private void Start ()
	{
		InputManager.onTileSelected += OnTileSelected;
		InputManager.onTileHovered += OnTileHovered;
		TurnManager.onEndInputPhase += ClearArrows;
	}

	private void OnDestroy ()
	{
		InputManager.onTileSelected-= OnTileSelected;
		InputManager.onTileHovered -= OnTileHovered;
		TurnManager.onEndInputPhase -= ClearArrows;
	}

	private void OnTileSelected ( Tile _tile )
	{
		if (TurnManager.Instance.currentPhase != TurnManager.TurnPhase.Recording)
			return;

		//Event => Select // unselect entity
		if(_tile.GetEntity(true) != null && !_tile.canInteract)
		{
			if (_tile.GetEntity(true).Data.faction == Entity.EntityFaction.Ally) 
			{ 
				if(m_selectedEntity == _tile.GetEntity(true))
				{
					m_selectedEntity.Deselect();
					m_selectedEntity = null;
					onEntitySelected?.Invoke(m_selectedEntity);
					return;
				}
				else if(m_selectedEntity == null)
				{
					m_selectedEntity = _tile.GetEntity(true);
					onEntitySelected?.Invoke(m_selectedEntity);

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
			if (_tile.canInteract)
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
		if (m_selectedTile == null)
			return;

		if (_tile != m_hoveredTile)
		{
			GridManager.Instance.ClearTileOutile();
			string weaponID = "default";
			m_selectedEntity.Equipment.AimAtTile(weaponID, _tile);
			List<Tile> tilesInRange = m_selectedEntity.Equipment.GetTilesInRange(weaponID);

			foreach(Tile tile in tilesInRange)
			{
				tile.UI.EnableOutline(Color.green);
			}

			m_hoveredTile = _tile;
			List<Tile> path = GridManager.Instance.GetPath(m_selectedTile, m_hoveredTile, true);

			if (path == null)
				return;

			foreach (Tile tile in path)
			{
				tile.UI.EnableOutline(Color.blue);
			}
		}
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
