using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GridEditorWindows : EditorWindow
{

	[MenuItem("Tools/Grid Editor")]
	public static void LoadWindows ()
	{
		GetWindow<GridEditorWindows>("Grid Editor");
	}

	private void OnGUI ()
	{
		Grid();
	}


	private void Grid ()
	{
		if (GridManager.Instance == null)
			return;

		GUILayoutOption group = GUILayout.Height(30f);

		StartBox("Grid");
		EditorGUILayout.BeginHorizontal(group);

		if (GUILayout.Button("Load Grid", group))
		{
			GridManager.Instance.LoadGrid(GridManager.Instance.gridData, true);
		}

		if (GUILayout.Button("Save Grid", group))
		{
			GridManager.Instance.SaveGrid();
		}

		if (GUILayout.Button("Fix Grid", group))
		{
			GridManager.Instance.FixTiles();
		}
		EditorGUILayout.EndHorizontal();

		//GridManager.Instance.isGroundBrushSelected = EditorGUILayout.Toggle("IsGroundBrushSelected: ", GridManager.Instance.isGroundBrushSelected, group);
		//if(GridManager.Instance.isGroundBrushSelected)
		GridManager.Instance.currentGroundBrushSelected = (TileGroundType)EditorGUILayout.EnumPopup("Current ground brush: ", GridManager.Instance.currentGroundBrushSelected);

		EndBox();
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
