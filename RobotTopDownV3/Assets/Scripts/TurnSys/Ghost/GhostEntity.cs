using FIMSpace.FProceduralAnimation;
using UnityEngine;

public class GhostEntity : MonoBehaviour
{
	[SerializeField] private Transform m_visual;
	[SerializeField] private Material m_hologramMaterial;
	public static Material DefaultHologram => GameAssets.current.game.baseGhost != null ? GameAssets.current.game.baseGhost.m_hologramMaterial : null;

	public void Init ( Entity _linkedEntity )
	{
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

	public void ShowAtPositionAndOrientation ( Tile _position, int _orientation )
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
