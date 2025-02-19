using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : PoolElement
{
    [SerializeField] private MeshRenderer m_renderer;

    public void SetMaterial(Material _mat)
	{
		m_renderer.material = _mat;
	}
}
