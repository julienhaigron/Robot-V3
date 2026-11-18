using System.Collections;
using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

public class GhostEntity : MonoBehaviour
{
	[SerializeField] private Transform m_visual;
	[SerializeField] private Material m_hologramMaterial;
	public Material HologramMaterial => m_hologramMaterial;

    private Entity m_linkedEntity;


    public void Init(Entity _linkedEntity )
	{
		m_linkedEntity = _linkedEntity;

		GameObject skin = Instantiate(_linkedEntity.Skin.IK.gameObject, m_visual);

		if (skin.TryGetComponent(out LegsAnimator legsAnimator))
			legsAnimator.Initialize_BaseTransform(m_visual);

		if (m_hologramMaterial == null)
			return;

		foreach (Renderer renderer in skin.GetComponentsInChildren<Renderer>(true))
		{
			if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
				EntitySkinPlugin.OverrideMaterials(renderer, m_hologramMaterial);
		}
	}

	public void ShowAtPositionAndOrientation(Tile _position, int _orientation )
	{
		if (m_linkedEntity != null)
		{
			m_linkedEntity.Skin.Hide();
			m_linkedEntity.Equipment.SetWeaponConesHidden(true);
		}

		transform.position = _position.transform.position/* - m_bottomPosition.localPosition*/;
		float angle = 30f + _orientation * 60f;
		m_visual.localRotation = Quaternion.Euler(0, angle, 0);

		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		if (m_linkedEntity != null)
		{
			m_linkedEntity.Skin.Show(NeuronalMembraneEquipmentData.VisionTypes.Optic);
			m_linkedEntity.Equipment.SetWeaponConesHidden(false);
		}

		transform.position = new Vector3(0f, -10f, 0f);
		gameObject.SetActive(false);
	}

}
