using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Utils
{
	public static T RandomElement<T> ( this List<T> _list )
	{
		return _list[UnityEngine.Random.Range(0, _list.Count)];
	}

	public static T RandomElement<T> ( this T[] _array )
	{
		return _array[UnityEngine.Random.Range(0, _array.Length)];
	}

	public static int Random ( this Vector2Int _minMax )
	{
		return UnityEngine.Random.Range(_minMax.x, _minMax.y);
	}

	public static void SetColor ( this ParticleSystem _ps, Color _color )
	{
		if (_ps == null)
			return;

		float a = _ps.main.startColor.color.a;
		Color col = _color;
		col.a = a;
		ParticleSystem.MainModule main = _ps.main;
		main.startColor = col;
	}

	/// <summary>
	/// Swaps 2 elements at the specified index positions in place.
	/// </summary>
	public static IList<T> SwapInPlace<T> ( this IList<T> source, int index1, int index2 )
	{
		(source[index1], source[index2]) = (source[index2], source[index1]);
		return source;
	}

	/// <summary>
	/// Shuffles a collection in place using the Knuth algorithm.
	/// </summary>
	public static IList<T> Shuffle<T> ( this IList<T> source )
	{
		for (int i = 0; i < source.Count - 1; ++i)
		{
			var indexToSwap = UnityEngine.Random.Range(i, source.Count);
			source.SwapInPlace(i, indexToSwap);
		}
		return source;
	}

#if UNITY_EDITOR
	public static void PlusHandleCap ( int _controlID, Vector3 _position, Quaternion _rotation, float _size,
		EventType _eventType )
	{
		switch (_eventType)
		{
			case EventType.MouseMove:
			case EventType.Layout:
				HandleUtility.AddControl(_controlID, HandleUtility.DistanceToCircle(_position, _size));
				break;
			case EventType.Repaint:
				Graphics.DrawMeshNow(Resources.Load<Mesh>("Plus"), StartCapDraw(_position, _rotation, _size));
				break;
		}
	}

	public static void MinusHandleCap ( int _controlID, Vector3 _position, Quaternion _rotation, float _size,
		EventType _eventType )
	{
		switch (_eventType)
		{
			case EventType.MouseMove:
			case EventType.Layout:
				HandleUtility.AddControl(_controlID, HandleUtility.DistanceToCircle(_position, _size));
				break;
			case EventType.Repaint:
				Graphics.DrawMeshNow(Resources.Load<Mesh>("Minus"), StartCapDraw(_position, _rotation, _size));
				break;
		}
	}

	public static void CursorHandleCap ( int _controlID, Vector3 _position, Quaternion _rotation, float _size,
		EventType _eventType )
	{
		switch (_eventType)
		{
			case EventType.MouseMove:
			case EventType.Layout:
				HandleUtility.AddControl(_controlID, HandleUtility.DistanceToCircle(_position, _size));
				break;
			case EventType.Repaint:
				Graphics.DrawMeshNow(Resources.Load<Mesh>("Cursor"), StartCapDraw(_position, _rotation, _size));
				break;
		}
	}

	public static void FlagHandleCap ( int _controlID, Vector3 _position, Quaternion _rotation, float _size,
		EventType _eventType )
	{
		switch (_eventType)
		{
			case EventType.MouseMove:
			case EventType.Layout:
				HandleUtility.AddControl(_controlID, HandleUtility.DistanceToCircle(_position, _size));
				break;
			case EventType.Repaint:
				Graphics.DrawMeshNow(Resources.Load<Mesh>("Flag"), StartCapDraw(_position, _rotation, _size));
				break;
		}
	}

	public static void LinkHandleCap ( int _controlID, Vector3 _position, Quaternion _rotation, float _size,
		EventType _eventType )
	{
		switch (_eventType)
		{
			case EventType.MouseMove:
			case EventType.Layout:
				HandleUtility.AddControl(_controlID, HandleUtility.DistanceToCircle(_position, _size));
				break;
			case EventType.Repaint:
				Graphics.DrawMeshNow(Resources.Load<Mesh>("Link"), StartCapDraw(_position, _rotation, _size));
				break;
		}
	}

	internal static Matrix4x4 StartCapDraw ( Vector3 _position, Quaternion _rotation, float _size )
	{
		Shader.SetGlobalColor("_HandleColor", Handles.color);
		Shader.SetGlobalFloat("_HandleSize", _size);
		Matrix4x4 matrix4x = Matrix4x4.TRS(_position, _rotation, Vector3.one);
		Shader.SetGlobalMatrix("_ObjectToWorld", matrix4x);
		//HandleUtility.handleMaterial.SetFloat("_HandleZTest", (float)zTest);
		HandleUtility.handleMaterial.SetPass(0);
		return matrix4x;
	}
#endif
}
