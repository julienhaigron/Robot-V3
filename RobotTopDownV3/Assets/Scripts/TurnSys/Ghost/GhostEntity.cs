using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostEntity : MonoBehaviour
{
	[SerializeField] private Transform m_visual;

    private Entity m_linkedEntity;


    public void Init(Entity _linkedEntity )
	{
		m_linkedEntity = _linkedEntity;

		GameObject skin = Instantiate(_linkedEntity.Skin.IK.gameObject, m_visual);
	}

	public void ShowAtPositionAndOrientation(Tile _position, int _orientation )
	{
		m_linkedEntity.Skin.Hide();

		transform.position = _position.transform.position/* - m_bottomPosition.localPosition*/;
		float angle = 30f + _orientation * 60f;
		m_visual.localRotation = Quaternion.Euler(0, angle, 0);

		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		m_linkedEntity.Skin.Show();

		transform.position = new Vector3(0f, -10f, 0f);
		gameObject.SetActive(false);
	}

}
