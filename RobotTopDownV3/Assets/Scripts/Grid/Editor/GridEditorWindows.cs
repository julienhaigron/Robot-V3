using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GridEditorWindows : EditorWindow
{
	public GridData m_data;


	[MenuItem("Tools/Grid Editor")]
	public static void LoadWindows ()
	{
		GetWindow<GridEditorWindows>("Grid Editor");
	}

	private void OnGUI ()
	{
		if (!EditorApplication.isPlaying)
		{
			EditorGUILayout.HelpBox("Press PLAY to use the Grid Editor", MessageType.Info);
			return;
		}


		Grid();
	}

	private void Grid ()
	{
		GUILayoutOption group = GUILayout.Height(30f);

		StartBox("Grid");
		EditorGUILayout.BeginHorizontal(group);

		m_data = (GridData)EditorGUILayout.ObjectField("Data: ", m_data, typeof(GridData), true);

		if (GUILayout.Button("Save Grid", group))
		{
			SaveGrid();
		}
		EditorGUILayout.EndHorizontal();

		GridManager.Instance.isGroundBrushSelected = EditorGUILayout.Toggle("IsGroundBrushSelected: ", GridManager.Instance.isGroundBrushSelected, group);

		if(GridManager.Instance.isGroundBrushSelected)
			GridManager.Instance.currentGroundBrushSelected = (TileGroundType)EditorGUILayout.EnumPopup("Current ground brush: ", GridManager.Instance.currentGroundBrushSelected);

		EndBox();
	}

	public void SaveGrid ()
	{
		GridManager grid = GridManager.Instance;
		m_data.height = grid.Height;
		m_data.width = grid.Width;
		m_data.tiles = new GridData.TileData[grid.Tiles.Length];

		for (int i = 0; i < grid.Tiles.Length; i++)
		{
			GridData.TileData tileData = new GridData.TileData(grid.Tiles[i].GroundType);
			m_data.tiles[i] = tileData;
		}

		EditorUtility.SetDirty(m_data);
	}

	#region window visual

	private void StartBox ( string _label/*, string _icon */)
	{
		EditorGUILayout.BeginVertical(GUI.skin.box);
		EditorGUILayout.BeginHorizontal(GUI.skin.box);
		//EditorGUILayout.LabelField(new GUIContent(_label, EditorGUIUtility.FindTexture(_icon)), EditorStyles.boldLabel);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space(5);
	}

	private void EndBox ()
	{
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space(10);
	}

	private void Title ( string _text )
	{
		EditorGUILayout.LabelField(_text);
	}

	private void SetSelection ( GameObject _target )
	{
		Selection.activeObject = _target;
	}

	#endregion
}
