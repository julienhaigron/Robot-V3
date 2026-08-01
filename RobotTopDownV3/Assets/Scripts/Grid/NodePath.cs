using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

public class NodePath : MonoBehaviour
{
	[SerializeField] private Mesh m_arrowMesh;
	[SerializeField] private Tile[] m_path;
	public Tile[] Path => m_path;

	public Tile GetNextTile ( Tile _currentTile )
	{
		for (int i = 0; i < m_path.Length; i++)
			if (m_path[i] == _currentTile)
				return m_path[(i + 1) % m_path.Length];

		return null;
	}

#if UNITY_EDITOR

	[Button]
	private void AddTileToPath(int _x, int _y )
	{
		List<Tile> path = m_path.ToList();
		Tile[] allTilesInScene = FindObjectsByType<Tile>(FindObjectsSortMode.None);
		foreach(Tile tile in allTilesInScene)
			if (string.Equals(tile.gameObject.name, "Tile "+_x + "." + _y))
				path.Add(tile);
		m_path = path.ToArray();
	}

	private void OnDrawGizmosSelected ()
	{
        if (m_path == null || m_path.Length < 3 || m_arrowMesh == null)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < m_path.Length; i++)
        {
            Tile from = m_path[i];
            Tile to = m_path[(i + 1) % m_path.Length];

            if (from == null || to == null)
                continue;

            Vector3 start = from.transform.position;
            Vector3 end = to.transform.position;
            Gizmos.DrawLine(start, end);

			Vector3 direction = (end - start).normalized;
			if (direction.sqrMagnitude < 0.0001f)
                continue;

			Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
			Vector3 position = Vector3.Lerp(start, end, 0.5f);

            Gizmos.DrawMesh( m_arrowMesh, position, rotation, Vector3.one * .5f);
        }
    }

#endif

}
