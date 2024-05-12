using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private Tile m_selectedTile;
	private Tile m_hoveredTile;

	private RobotEntity m_selectedEntity;

	private void Start ()
	{
		InputManager.onTileSelected += OnTileSelected;
		InputManager.onTileHovered += OnTileHovered;
	}

	private void OnDestroy ()
	{
		InputManager.onTileSelected-= OnTileSelected;
		InputManager.onTileHovered -= OnTileHovered;
	}

	private void OnTileSelected ( Tile _tile )
	{
		if(m_selectedTile != null && m_selectedTile != _tile)
		{
			m_selectedEntity.Displacement.MoveToTile(_tile);
			m_selectedEntity = null;
			m_selectedTile = null;
			GridManager.Instance.ClearTileOutile();
		}
		else if (m_selectedTile != _tile && _tile.Entity != null && _tile.Entity == m_selectedEntity)
		{
			m_selectedEntity.Deselect();
			m_selectedEntity = null;
			m_selectedTile = null;
			GridManager.Instance.ClearTileOutile();
		}
		else if (m_selectedTile != _tile && _tile.Entity != null)
		{
			m_selectedTile = _tile;
			m_selectedEntity = _tile.Entity;
			m_selectedEntity.Select();
			GridManager.Instance.BFS(_tile);
		}
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
			List<Tile> path = GridManager.Instance.GetPath(m_selectedTile, m_hoveredTile);

			if (path == null)
				return;

			foreach (Tile tile in path)
			{
				tile.UI.EnableOutline(Color.blue);
			}
		}
	}
}
