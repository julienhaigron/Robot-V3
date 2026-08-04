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
	
	[SerializeField] private int m_hp = 1;
	public int Health { get { return m_hp; } set { m_hp = value; } }
	
	[SerializeField, ReadOnly] private int m_orientation; //between 0-5
	public int Orientation { get { return m_orientation; } set { m_orientation = value; } }
	
	[SerializeField] private WallType m_type = WallType.VerticalStrait;
	public WallType Type { get { return m_type; } set { m_type = value; } }

	private bool m_isCover;

	[Serializable]
	public enum WallType
	{
		VerticalStrait,
		HorizontalStrait,
		LAngle,
		ReverseLAngle,
		TAngle,
		SmallAngle,
		ReverseSmallAngle,
		WideV,
		ThinV,
		HorizontalYAngle,
		HorizontalReverseYAngle,
		VerticalYAngle,
		VerticalReverseYAngle,
		XCross,
		LnYAngle,
		TriWall,
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

	public void TakeDamage(Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		foreach(KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _damages)
		{
			m_hp -= pair.Value;
		}
		if (m_hp <= 0)
			Destroy();
	}

	private void Destroy ()
	{
		foreach (GameObject go in m_wallParts)
			go.SetActive(false);
	}

#if UNITY_EDITOR
	public void SetWallType(WallType _type, bool _isCover )
	{
		//Rotate(0);
		//Undo.RecordObject(this, "Set Wall Type");
		//Undo.RecordObject(m_linkedTile, "Set Wall Type 2");
		m_type = _type;
		m_isCover = _isCover;

		foreach(GameObject go in m_wallParts)
		{
			DestroyImmediate(go);
		}
		m_wallParts.Clear();
		m_linkedTile.WallPartsParent.rotation = Quaternion.identity;

		if (m_linkedTile.WallPartsParent != null)
			DestroyImmediate(m_linkedTile.WallPartsParent.gameObject);

		if (!GameAssets.current.game.baseWallVisualPerType.ContainsKey(_type))
		{
			Debug.LogError("Missing value in GameAssets.current.game.baseWallVisualPerType");
			return;
		}
		
		GameObject wallPrefab = PrefabUtility.InstantiatePrefab(GameAssets.current.game.baseWallVisualPerType[_type], m_linkedTile.transform) as GameObject;
		m_linkedTile.WallPartsParent = wallPrefab.transform;
		Transform partsParent = wallPrefab.transform.GetChild(0);
		partsParent.localScale = new Vector3(1f, m_isCover ? .5f : 1, 1f);
		for (int i = partsParent.childCount - 1; i >= 0; i--)
		{
			Transform tfm = partsParent.GetChild(i);
			//Undo.AddComponent<WallSelector>(tfm.gameObject).Link(this);
			tfm.gameObject.AddComponent<WallSelector>().Link(this);
			m_wallParts.Add(tfm.gameObject);
		}

		//EditorUtility.SetDirty(this);
		//EditorUtility.SetDirty(m_linkedTile);
	}

	[Button]
	public void Rotate(int _newRotation )
	{
		//Undo.RecordObject(this, "Rotate Wall");
		//Undo.RecordObject(m_linkedTile, "Rotate Wall 2");

		m_orientation = _newRotation;
		float yRotation = 60f * _newRotation;
		m_linkedTile.WallPartsParent.localRotation = Quaternion.Euler(0, yRotation, 0);

		//EditorUtility.SetDirty(this);
		//EditorUtility.SetDirty(m_linkedTile);
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
			RotateLeft();
		}

		Handles.color = Color.green;
		if (Handles.Button(transform.position + Vector3.back + Vector3.right, Quaternion.identity, size, pickSize, Utils.PlusHandleCap))
		{
			RotateRight();
		}

		Handles.color = Color.blue;
		if (Handles.Button(transform.position + Vector3.back, Quaternion.identity, size, pickSize, Utils.LinkHandleCap))
		{
			WallType nextWallType = (WallType)((int)++Type % (int)WallType.Total);
			SetWallType(nextWallType, m_isCover);
		}
	}

	/*public void HandleInputs ()
	{
		if (Input.GetKeyDown(KeyCode.R))
			RotateRight();

		if (Input.GetKeyDown(KeyCode.T))
		{
			WallType nextWallType = (WallType)((int)++Type % (int)WallType.Total);
			SetWallType(nextWallType);
		}
	}*/

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
