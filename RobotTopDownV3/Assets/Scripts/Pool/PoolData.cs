using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PoolData : ScriptableObject
{
	public PoolElement baseElement;
	public int size = 10;

	public PoolElement Get ( Vector3 _position = default, Quaternion _rotation = default )
	{
		return Get<PoolElement>(_position, _rotation);
	}

	public T Get<T> ( Vector3 _position = default, Quaternion _rotation = default ) where T : PoolElement
	{
		return ObjectsPooling.GetElement(this, _position, _rotation) as T;
	}
}
