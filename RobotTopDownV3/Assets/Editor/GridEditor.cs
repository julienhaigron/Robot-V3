using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using UnityEditor.EditorTools;

[CustomEditor(typeof(GridManager))]
public class GridEditor : Editor
{

	//private bool m_hasReleaseKey = false;

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
			gridManager.SaveGrid();
		}
		if (GUILayout.Button("Fix Grid"))
		{
			GridManager.Instance.FixTiles();
		}
	}

}

[EditorTool("Grid Tool")]
public class GridTool : EditorTool
{
	private Tile m_lastPaintedTile;
	private bool m_isPainting;

	public override GUIContent toolbarIcon => new GUIContent(
		EditorGUIUtility.IconContent("LightProbeProxyVolume Gizmo").image,
		"Grid Tool"
	);

	public override void OnToolGUI ( EditorWindow window )
	{
		if (GridManager.Instance == null /*|| !GridManager.Instance.isGroundBrushSelected*/)
			return;

		Event e = Event.current;

		// Bloque les tools Unity par défaut
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

		if (m_isPainting)
		{
			if (window != EditorWindow.focusedWindow ||
				e.type == EventType.MouseLeaveWindow)
			{
				StopPainting();
				e.Use();
				return;
			}
		}

		if (e.type == EventType.MouseDown && e.button == 0)
		{
			m_isPainting = true;
			PaintAtMouse(e.mousePosition);
			e.Use();
		}

		if (e.type == EventType.MouseDrag && e.button == 0 && m_isPainting)
		{
			PaintAtMouse(e.mousePosition);
			e.Use();
		}

		if (e.type == EventType.MouseUp && e.button == 0)
		{
			StopPainting();
			e.Use();
		}

		if (e.type == EventType.MouseMove && m_isPainting)
			PaintAtMouse(e.mousePosition);

		HandleShortcuts(e);
	}

	public override void OnWillBeDeactivated ()
	{
		StopPainting();
	}

	private void PaintAtMouse ( Vector2 mousePosition )
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

		if (!Physics.Raycast(
			ray,
			out RaycastHit hit,
			GameConfig.current.input.interactionRayCastLength,
			GameConfig.current.input.interactionRayCastLayer))
			return;

		if (!hit.transform.parent.TryGetComponent(out Tile tile))
			return;

		// Empêche de repeindre la même tile en boucle
		if (tile == m_lastPaintedTile)
			return;

		/*Undo.RecordObject(tile, "Paint Tile");
		Undo.RecordObject(tile.Wall, "Paint Tile");*/
		//Undo.RecordObject(GridManager.Instance.gridData, "Paint Tile");
		tile.SetGroundType(GridManager.Instance.currentGroundBrushSelected, true);
		//EditorUtility.SetDirty(tile);
		//EditorUtility.SetDirty(GridManager.Instance.gridData);

		m_lastPaintedTile = tile;
	}

	private void StopPainting ()
	{
		m_isPainting = false;
		m_lastPaintedTile = null;
	}

	private void HandleShortcuts ( Event e )
	{
		if (e.type != EventType.KeyDown)
			return;

		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

		if (!Physics.Raycast(ray, out RaycastHit hit,
			GameConfig.current.input.interactionRayCastLength,
			GameConfig.current.input.interactionRayCastLayer))
			return;

		if (!hit.transform.parent.TryGetComponent(out Tile tile) || tile.Wall == null)
			return;

		if (e.keyCode == KeyCode.R)
		{
			//Undo.RecordObject(GridManager.Instance.gridData, "Rotate Wall");
			//Undo.RecordObject(tile, "Rotate Wall");
			//Undo.RecordObject(tile.Wall, "Rotate Wall");
			tile.Wall.RotateRight();
			e.Use();
		}

		if (e.keyCode == KeyCode.T)
		{
			//Undo.RecordObject(GridManager.Instance.gridData, "Change Wall Type");
			//Undo.RecordObject(tile, "Change Wall Type");
			//Undo.RecordObject(tile.Wall, "Change Wall Type");
			Wall.WallType next =
				(Wall.WallType)(((int)tile.Wall.Type + 1) % (int)Wall.WallType.Total);
			tile.Wall.SetWallType(next);
			e.Use();
		}
	}
}