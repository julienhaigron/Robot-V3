using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private Tile m_selectedTile;
	private Tile m_hoveredTile;

	private RobotEntity m_selectedEntity;

	private void Awake ()
	{
		GridManager.Instance.onTileSelected += OnTileSelected;
		GridManager.Instance.onTileHovered += OnTileHovered;
	}

	private void OnDestroy ()
	{
		GridManager.Instance.onTileSelected-= OnTileSelected;
		GridManager.Instance.onTileHovered -= OnTileHovered;
	}

	private void OnTileSelected ( Tile _tile )
	{
		if (m_selectedTile != _tile && _tile.Entity != null)
		{
			m_selectedTile = _tile;
			m_selectedEntity = _tile.Entity;

			GridManager.Instance.BFS(_tile);
		}
	}

	private void OnTileHovered ( Tile _tile )
	{
		if (m_selectedTile == null)
			return;

		if (_tile != m_hoveredTile)
		{
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
