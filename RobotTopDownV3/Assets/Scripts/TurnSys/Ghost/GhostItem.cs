using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostItem : MonoBehaviour
{
	[SerializeField] private Transform m_visual;


    public void Init(AItemData _itemData )
	{
		Instantiate(_itemData.itemPrefab, m_visual);
	}

	public void ShowAtPositionAndOrientation(Tile _position, int _orientation )
	{
		transform.position = _position.transform.position;
		m_visual.localRotation = RotateEntityAction.GetFacingRotation(_orientation);

		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		transform.position = new Vector3(0f, -10f, 0f);
		gameObject.SetActive(false);
	}

}
