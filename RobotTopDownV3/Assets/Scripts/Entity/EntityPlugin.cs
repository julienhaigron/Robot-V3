using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPlugin : MonoBehaviour
{
    [SerializeField] protected Entity m_linkedEntity;

	private void Reset ()
	{
		if(TryGetComponent(out Entity entity))
		{
			m_linkedEntity = entity;
		}
	}

	public virtual void Init ()
	{

	}
}
