using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Wall : MonoBehaviour
{
	[SerializeField] private List<GameObject> m_wallParts = new();
	public List<GameObject> WallParts => m_wallParts;

	//saved infos
    [SerializeField] private Tile m_linkedTile;
	public Tile LinkedTile { get { return m_linkedTile; } set { m_linkedTile = value; } }
	
	[SerializeField] private bool m_isDestructible = true;
	public bool IsDestructible { get { return m_isDestructible; } set { m_isDestructible = value; } }
	
	[SerializeField] private int m_health;
	public int Health { get { return m_health; } set { m_health = value; } }
	
	[SerializeField, ReadOnly] private int m_orientation; //between 0-5
	public int Orientation { get { return m_orientation; } set { m_orientation = value; } }
	
	[SerializeField] private WallType m_type = WallType.VerticalStrait;
	public WallType Type { get { return m_type; } set { m_type = value; } }

	public enum WallType
	{
		VerticalStrait,
		HorizontalStrait,
		LAngle,
		ReverseLAngle,
		TAngle,
		Total
	}
	//TODO :
	// => Destructible feature
	// => cover feature

	//EDITOR :
	// - lier le visuel d'un mur facilement à une tile
	// - déterminé + visualiser la "coverability" d'un mur, angle
	// - tourner un mur

	public void LinkWithTile ( Tile m_tile )
	{
		m_linkedTile = m_tile;
	}

	public void TakeDamage(int _amount )
	{
		m_health -= _amount;
		if (m_health <= 0)
			Destroy();
	}

	private void Destroy ()
	{
		
	}

	public void SetWallType(WallType _type )
	{
		m_orientation = 0;
		m_type = _type;

		foreach(GameObject go in m_wallParts)
		{
			DestroyImmediate(go);
		}
		m_wallParts.Clear();

		if (!GameAssets.current.game.baseWallVisualPerType.ContainsKey(_type))
		{
			Debug.LogError("Missing value in GameAssets.current.game.baseWallVisualPerType");
			return;
		}

		GameObject wallPrefab = Instantiate(GameAssets.current.game.baseWallVisualPerType[_type]);
		for (int i = wallPrefab.transform.childCount - 1; i >= 0; i--)
		{
			Transform tfm = wallPrefab.transform.GetChild(i);
			Vector3 localPosition = tfm.localPosition;
			tfm.parent = m_linkedTile.WallPartsParent;
			tfm.localPosition = localPosition;
			m_wallParts.Add(tfm.gameObject);
		}
		DestroyImmediate(wallPrefab);

		EditorUtility.SetDirty(this);
		EditorUtility.SetDirty(m_linkedTile);
	}

	[Button]
	public void Rotate(int _newRotation )
	{
		m_orientation = _newRotation;
		float yRotation = 60f * _newRotation;
		m_linkedTile.WallPartsParent.localRotation = Quaternion.Euler(0, yRotation, 0);

		EditorUtility.SetDirty(this);
		EditorUtility.SetDirty(m_linkedTile);
	}

	[Button]
	public void RotateRight ()
	{
		Rotate(++m_orientation % 6);
	}

	[Button]
	public void RotateLeft ()
	{
		Rotate(--m_orientation % 6);
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Wall))]
	class WallEditor : Editor
	{
		//PathNode selectedNode = null;

		public override void OnInspectorGUI ()
		{
			DrawDefaultInspector();
			Wall wall = (Wall)target;
		}

		protected virtual void OnSceneGUI ()
		{
			Wall wall = (Wall)target;

			GUIStyle style = new();
			style.fontStyle = FontStyle.Bold;
			float size = .3f;
			float pickSize = size;

			Handles.SphereHandleCap(0, wall.transform.position, Quaternion.identity, size * .6f, EventType.Repaint);

			Handles.color = Color.red;
			if (Handles.Button(wall.transform.position + Vector3.back + Vector3.left, Quaternion.identity, size, pickSize, Utils.MinusHandleCap))
			{
				//Undo.RecordObject(path, "Add circulatory path node");
				wall.RotateLeft();
				//EditorUtility.SetDirty(wall);
			}

			Handles.color = Color.green;
			if (Handles.Button(wall.transform.position + Vector3.back + Vector3.right, Quaternion.identity, size, pickSize, Utils.PlusHandleCap))
			{
				//Undo.RecordObject(path, "Add circulatory path node");
				wall.RotateRight();
				//EditorUtility.SetDirty(wall);
			}
			
			Handles.color = Color.blue;
			if (Handles.Button(wall.transform.position + Vector3.back, Quaternion.identity, size, pickSize, Utils.LinkHandleCap))
			{
				//Undo.RecordObject(path, "Add circulatory path node");
				WallType nextWallType = (WallType)((int)++wall.Type % (int)WallType.Total);
				wall.SetWallType(nextWallType);
				//EditorUtility.SetDirty(wall);
			}

			/*if (path.IsEmpty)
			{
				Handles.color = Color.green;
				Handles.Label(Vector3.right * .3f, "Click to create path", style);
				if (Handles.Button(path.transform.position, Quaternion.identity, size, pickSize, Handles.SphereHandleCap))
				{
					selectedNode = path.AddNode(path.transform.position, selectedNode);
				}

				return;
			}*/

			//Handles.color = path.PathColor;
			/*for (int i = 0; i < path.Nodes.Count; i++)
			{
				PathNode node = path.Nodes[i];

				if (path.Nodes.Count > i + 1)
					Handles.DrawDottedLine(node.AbsolutePosition + path.transform.position, path.Nodes[i + 1].AbsolutePosition + path.transform.position, 10f);

				if (selectedNode == node)
					continue;

				if (Handles.Button(node.AbsolutePosition + path.transform.position, Quaternion.identity, size * 2f, pickSize, Handles.SphereHandleCap))
				{
					selectedNode = node;
				}
			}

			if (selectedNode == null)
				return;*/

			//Vector3 selectedNodeWorldPos = selectedNode.AbsolutePosition + path.transform.position;

			//Handles.color = path.PathColor;
			/*Handles.SphereHandleCap(0, selectedNodeWorldPos, Quaternion.identity, size * .6f, EventType.Repaint);

			//move node
			EditorGUI.BeginChangeCheck();
			Vector3 newTargetPosition = Handles.PositionHandle(selectedNodeWorldPos, Quaternion.identity);
			if (EditorGUI.EndChangeCheck())
			{
				//Undo.RecordObject(path, "Move node from circulatory path");
				selectedNode.AbsolutePosition = newTargetPosition - path.transform.position;
				EditorUtility.SetDirty(path);
			}

			//add node
			Handles.color = Color.green;
			if (Handles.Button(selectedNodeWorldPos + Vector3.back + Vector3.right, Quaternion.identity, size, pickSize, ObjectsExtensions.PlusHandleCap))
			{
				//Undo.RecordObject(path, "Add circulatory path node");
				selectedNode = path.AddNode(selectedNode.AbsolutePosition + Vector3.right, selectedNode);
				EditorUtility.SetDirty(path);
			}

			//remove node
			Handles.color = Color.red;
			if (Handles.Button(selectedNodeWorldPos + Vector3.back + Vector3.left, Quaternion.identity, size, pickSize, ObjectsExtensions.MinusHandleCap))
			{
				//Undo.RecordObject(path, "Add circulatory path node");
				path.RemoveNode(selectedNode);

				if (path.Nodes.Count > 0)
					selectedNode = path.Nodes.Last();
				else
					selectedNode = null;

				EditorUtility.SetDirty(path);
			}

			//Handles.color = path.PathColor;
			EditorGUI.BeginChangeCheck();
			Vector3 newTargetRange = Handles.Slider(selectedNodeWorldPos + (Vector3.left * selectedNode.radius) + new Vector3(-.2f, 0f, 0f), Vector3.left, size, Handles.ConeHandleCap, 1f);
			newTargetRange -= new Vector3(-.2f, 0f, 0f);
			if (EditorGUI.EndChangeCheck())
			{
				//Undo.RecordObject(selectedNode, "Change node radius");
				selectedNode.radius = Mathf.Clamp(-(newTargetRange - selectedNodeWorldPos).x, 0f, Mathf.Infinity);
			}

			Handles.CircleHandleCap(0, selectedNodeWorldPos, Quaternion.LookRotation(Vector3.up), selectedNode.radius, EventType.Repaint);*/
		}
	}
#endif

}
