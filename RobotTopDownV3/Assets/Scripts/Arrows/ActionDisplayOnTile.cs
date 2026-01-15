using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionDisplayOnTile : PoolElement
{
    [SerializeField] private MeshRenderer[] m_renderers;

    public void SetMaterial(Material _mat)
	{
		foreach(MeshRenderer rd in m_renderers)
			rd.material = _mat;
	}
}
