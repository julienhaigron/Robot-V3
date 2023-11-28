using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TileCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{
	public override void OnGUI (
		Rect position, SerializedProperty property, GUIContent label
	)
	{
		TileCoordinates coordinates = new TileCoordinates(
			property.FindPropertyRelative("m_x").intValue,
			property.FindPropertyRelative("m_z").intValue
		);

		position = EditorGUI.PrefixLabel(position, label);
		GUI.Label(position, coordinates.ToString());
	}
}
