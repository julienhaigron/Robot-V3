using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NodePathManager : Singleton<NodePathManager>
{
    [SerializeField] private NodePath[] m_paths;

    public NodePath GetClosestPath (Tile _from, out Tile _closestTile )
	{
		_closestTile = null;
		if (m_paths == null || m_paths.Length == 0)
			return null;

		NodePath closestPath = m_paths[0];
		int closestDistance = int.MaxValue;
		GridManager.Instance.BFS(_from, _isThisTurn: true);
		foreach(NodePath path in m_paths)
		{
			foreach(Tile tile in path.Path)
			{
				if(tile.Distance < closestDistance)
				{
					closestDistance = tile.Distance;
					closestPath = path;
					_closestTile = tile;
				}

			}
		}

		return closestPath;
	}
}
