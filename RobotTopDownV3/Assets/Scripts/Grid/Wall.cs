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
		SmallAngle,
		ReverseSmallAngle,
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
			tfm.gameObject.AddComponent<WallSelector>().Link(this);
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

	public void DisplayHandles ()
	{
		GUIStyle style = new();
		style.fontStyle = FontStyle.Bold;
		float size = .3f;
		float pickSize = size;

		Handles.SphereHandleCap(0, transform.position, Quaternion.identity, size * .6f, EventType.Repaint);

		Handles.color = Color.red;
		if (Handles.Button(transform.position + Vector3.back + Vector3.left, Quaternion.identity, size, pickSize, Utils.MinusHandleCap))
		{
			//Undo.RecordObject(path, "Add circulatory path node");
			RotateLeft();
			//EditorUtility.SetDirty(wall);
		}

		Handles.color = Color.green;
		if (Handles.Button(transform.position + Vector3.back + Vector3.right, Quaternion.identity, size, pickSize, Utils.PlusHandleCap))
		{
			//Undo.RecordObject(path, "Add circulatory path node");
			RotateRight();
			//EditorUtility.SetDirty(wall);
		}

		Handles.color = Color.blue;
		if (Handles.Button(transform.position + Vector3.back, Quaternion.identity, size, pickSize, Utils.LinkHandleCap))
		{
			//Undo.RecordObject(path, "Add circulatory path node");
			WallType nextWallType = (WallType)((int)++Type % (int)WallType.Total);
			SetWallType(nextWallType);
			//EditorUtility.SetDirty(wall);
		}
	}

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

			wall.DisplayHandles();
		}
	}
#endif

}
