using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using UnityEditor.EditorTools;

[CustomEditor(typeof(GridManager))]
public class GridEditor : Editor
{

	private bool m_hasReleaseKey = false;

	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector();
		GridManager gridManager = (GridManager)target;

		//TODO
		//=> add security validation bubble before calling fncts bellow
		// in order not to lose info by accident

		// => add toggle select tiles only
		//display selected tile info?

		if (GUILayout.Button("GenerateGrid"))
		{
			gridManager.GenerateGrid(gridManager.Height, gridManager.Width);
		}
		if (GUILayout.Button("LoadGrid"))
		{
			gridManager.LoadGrid(gridManager.gridData, true);
		}
		if (GUILayout.Button("SavedGrid"))
		{
			gridManager.gridData.height = gridManager.Height;
			gridManager.gridData.width = gridManager.Width;
			gridManager.gridData.tiles = new GridData.TileData[gridManager.Tiles.Length];

			for (int i = 0; i < gridManager.Tiles.Length; i++)
			{
				GridData.TileData tileData = new GridData.TileData(gridManager.Tiles[i].GroundType);
				if(gridManager.Tiles[i].GroundType == TileGroundType.Wall)
				{
					tileData.wallType = gridManager.Tiles[i].Wall.Type;
					tileData.orientation = gridManager.Tiles[i].Wall.Orientation;
				}
				gridManager.gridData.tiles[i] = tileData;
			}

			EditorUtility.SetDirty(gridManager.gridData);
		}
	}

	/*private void OnSceneGUI ()
	{
		// remember to toggle gizmos in scene
		// and to deactivate interaction to GameManger prefab in scene
		if (!GridManager.Instance.isGroundBrushSelected)
			return;

		Event e = Event.current;

		if (e.type == EventType.MouseDown && e.keyCode == KeyCode.Mouse0)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
			{
				if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
				{
					tile.SetGroundType(GridManager.Instance.currentGroundBrushSelected);
					EditorUtility.SetDirty(this);
				}
			}
		}

		if (!m_hasReleaseKey && e.type == EventType.KeyDown)
			return;
		else if (e.type == EventType.KeyUp)
		{
			m_hasReleaseKey = true;
			return;
		}
		else if (m_hasReleaseKey && e.type == EventType.KeyDown)
			m_hasReleaseKey = false;

		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
			{
				if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
				{
					tile.Wall.RotateRight();
				}
			}
		}

		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.T)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
			{
				if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
				{
					Wall.WallType nextWallType = (Wall.WallType)((int)++tile.Wall.Type % (int)Wall.WallType.Total);
					tile.Wall.SetWallType(nextWallType);
				}
			}
		}
	}*/
}

[EditorTool("Grid Tool")]
public class GridTool : EditorTool
{
	private bool m_hasReleaseKey = true;

	public override GUIContent toolbarIcon => new GUIContent(
		EditorGUIUtility.IconContent("d_EditCollider").image,
		"Grid Tool"
	);

	public override void OnToolGUI ( EditorWindow window )
	{
		if (!GridManager.Instance.isGroundBrushSelected)
			return;

		Event e = Event.current;

		if (e.type == EventType.MouseDown && e.keyCode == KeyCode.Mouse0)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
			{
				if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
				{
					tile.SetGroundType(GridManager.Instance.currentGroundBrushSelected);
					EditorUtility.SetDirty(this);
				}
			}
		}

		if (!m_hasReleaseKey && e.type == EventType.KeyDown)
			return;
		else if (e.type == EventType.KeyUp)
		{
			m_hasReleaseKey = true;
			return;
		}
		else if (m_hasReleaseKey && e.type == EventType.KeyDown)
			m_hasReleaseKey = false;

		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Alpha1)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
			{
				if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
				{
					tile.Wall.RotateRight();
				}
			}
		}

		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Alpha2)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, GameConfig.current.input.interactionRayCastLength, GameConfig.current.input.interactionRayCastLayer))
			{
				if (hitInfo.transform.parent.TryGetComponent(out Tile tile))
				{
					Wall.WallType nextWallType = (Wall.WallType)((int)++tile.Wall.Type % (int)Wall.WallType.Total);
					tile.Wall.SetWallType(nextWallType);
				}
			}
		}
		/*Event e = Event.current;

		if (e.type == EventType.MouseDown && e.button == 0)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

			if (Physics.Raycast(ray, out var hit))
			{
				Selection.activeGameObject = hit.collider.gameObject;
				e.Use();
			}
		}

		// Exemple de gizmo
		Handles.color = Color.cyan;
		Handles.DrawWireDisc(Vector3.zero, Vector3.up, 5f);*/
	}
}