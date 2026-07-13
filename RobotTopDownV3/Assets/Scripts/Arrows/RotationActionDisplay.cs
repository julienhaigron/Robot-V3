using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationActionDisplay : PoolElement
{
    [SerializeField] private MeshRenderer[] m_renderers;

	private RotateEntityAction m_action;
	public RotateEntityAction Action => m_action;
	/*public Tile OriginTile => GridManager.Instance.Tiles[m_action.supposedPositionAtActionStartID];
	public Tile DestinationTile => GridManager.Instance.Tiles[m_action.positionAtActionEndID];*/

	public void Init (RotateEntityAction _rotateAction, Entity.EntityState _state)
	{
		m_action = _rotateAction;
		SetMaterial(GameAssets.current.ui.entityStateMaterials[_state]);
	}

    public void SetMaterial(Material _mat)
	{
		foreach(MeshRenderer rd in m_renderers)
		{
			rd.material = _mat;
		}
	}
}
