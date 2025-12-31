using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

[CustomEditor(typeof(GridManager))]
public class GridEditor : Editor
{
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

	private void OnSceneGUI ()
	{
		// remember to toggle gizmos in scene
		// and to deactivate interaction to GameManger prefab in scene

		Event e = Event.current;

		if (GridManager.Instance.isGroundBrushSelected && e.type == EventType.MouseDown && e.keyCode == KeyCode.Mouse0)
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
	}
}
