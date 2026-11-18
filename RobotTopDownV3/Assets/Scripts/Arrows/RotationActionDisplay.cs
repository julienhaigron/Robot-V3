using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationActionDisplay : PoolElement
{
	private static readonly int ColorID = Shader.PropertyToID("_Color");

    [SerializeField] private MeshRenderer[] m_renderers;
	[SerializeField] private Color m_rotationColor = new(.72f, .35f, 1f, 1f);

	private RotateEntityAction m_action;
	public RotateEntityAction Action => m_action;
	private MaterialPropertyBlock m_propertyBlock;
	/*public Tile OriginTile => GridManager.Instance.Tiles[m_action.supposedPositionAtActionStartID];
	public Tile DestinationTile => GridManager.Instance.Tiles[m_action.positionAtActionEndID];*/

	public void Init (RotateEntityAction _rotateAction, Entity.EntityState _state)
	{
		Init(_rotateAction, GameAssets.current.ui.entityStateMaterials[_state]);
	}

	public void Init ( RotateEntityAction _rotateAction, Material _material )
	{
		m_action = _rotateAction;
		if (_material != null)
			SetMaterial(_material);
	}

    public void SetMaterial(Material _mat)
	{
		foreach(MeshRenderer rd in m_renderers)
		{
			rd.material = _mat;
		}

		ApplyRotationColor(_mat);
	}

	private void ApplyRotationColor ( Material _material )
	{
		Color color = m_rotationColor;
		if (_material != null && _material.HasProperty(ColorID))
			color.a *= _material.GetColor(ColorID).a;

		m_propertyBlock ??= new MaterialPropertyBlock();
		foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>(true))
		{
			meshRenderer.GetPropertyBlock(m_propertyBlock);
			m_propertyBlock.SetColor(ColorID, color);
			meshRenderer.SetPropertyBlock(m_propertyBlock);
		}
	}
}
