using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Text.RegularExpressions;

[DisallowMultipleComponent, ExecuteInEditMode]
public class InstancedRenderer : MonoBehaviour
{
	public enum PropertyBlockReference
	{
		First,
		Self,
	}

	public Material material
	{
		get
		{
			if (attachedRenderer == null || attachedRenderer[0] == null)
				return null;

			return attachedRenderer[0].sharedMaterial;
		}
	}

	[HideInInspector] public PropertyBlockReference reference;
	[ReadOnly] public SerializableDictionary<string, SerializedPropertyValue> serializedPropertyValues = new();
	public Renderer[] attachedRenderer;

	public MaterialPropertyBlock propertyBlock;

	private Tweener m_floatTweener;
	private Tweener m_colorTweener;

	#region Monobehavior

	public void Reset ()
	{
		AttachRenderers(GetComponent<Renderer>());

		serializedPropertyValues.Clear();
		propertyBlock = null;

		//yield return null;

		if (material == null)
			return;

		Shader shader = material.shader;

		for (int i = 0; i < shader.GetPropertyCount(); i++)
		{
			if (!string.IsNullOrEmpty(shader.GetPropertyDescription(i)))
			{
				if (ShouldDrawProperty(i, out string header))
				{
					AddSerializedProperty(i, material);
				}
			}
		}

		InitPropertyBlocks();
	}

	/// <summary>
	/// Don't call this method :) Use add or remove in play mode instead
	/// </summary>
	public void AttachRenderers ( params Renderer[] _renderer )
	{
		if (_renderer != null)
		{
			List<Renderer> rList = new();
			foreach (Renderer r in _renderer)
			{
				if (r != null)
					rList.Add(r);
			}
			attachedRenderer = rList.ToArray();
		}
	}

	public void AddRenderers ( params Renderer[] _renderer )
	{
		Renderer[] result = new Renderer[attachedRenderer.Length + _renderer.Length];
		for (int i = 0; i < attachedRenderer.Length; i++)
		{
			result[i] = attachedRenderer[i];
		}
		for (int i = 0; i < _renderer.Length; i++)
		{
			result[attachedRenderer.Length + i] = _renderer[i];
		}

		attachedRenderer = result;
	}

	public void RemoveRenderers ( params Renderer[] _renderer )
	{
		List<Renderer> rList = new();
		foreach (Renderer r in attachedRenderer)
		{
			rList.Add(r);
		}

		foreach (Renderer r in _renderer)
		{
			if (rList.Contains(r))
				rList.Remove(r);
		}

		attachedRenderer = rList.ToArray();
	}
	private void Awake ()
	{
		if (!Application.isPlaying)
			return;

		InitPropertyBlocks();
	}

	private void OnValidate ()
	{
		InitPropertyBlocks();
	}

	private void OnDestroy ()
	{
		propertyBlock = new MaterialPropertyBlock();
		propertyBlock.Clear();

		if (attachedRenderer != null)
		{
			foreach (Renderer r in attachedRenderer)
			{
				if (r == null)
					continue;
				r.SetPropertyBlock(propertyBlock);
			}
		}

		if (m_colorTweener.IsActive())
			m_colorTweener.Kill();
		if (m_floatTweener.IsActive())
			m_floatTweener.Kill();
	}

	#endregion

	#region Init

	private void InitPropertyBlocks ()
	{
		foreach (Renderer r in attachedRenderer)
		{
			if (r == null)
				continue;

			InitPropertyBlock(r);
		}
	}

	private void InitPropertyBlock ( Renderer _renderer )
	{
		propertyBlock = new MaterialPropertyBlock();

		_renderer.GetPropertyBlock(propertyBlock);

		foreach (KeyValuePair<string, SerializedPropertyValue> kvp in serializedPropertyValues)
		{
			if (kvp.Value.hasChanged)
			{
				switch (kvp.Value.type)
				{
					case ShaderPropertyType.Color:
						propertyBlock.SetColor(kvp.Key, kvp.Value.colorValue);
						break;
					case ShaderPropertyType.Vector:
						propertyBlock.SetVector(kvp.Key, kvp.Value.vectorValue);
						break;
					case ShaderPropertyType.Float:
						propertyBlock.SetFloat(kvp.Key, kvp.Value.floatValue);
						break;
					case ShaderPropertyType.Range:
						propertyBlock.SetFloat(kvp.Key, kvp.Value.floatValue);
						break;
					case ShaderPropertyType.Texture:
						break;
				}
			}
		}

		_renderer.SetPropertyBlock(propertyBlock);
	}

	#endregion

	#region Get

	public Color GetColor ( string _propertyName )
	{
		return serializedPropertyValues[_propertyName].colorValue;
	}

	public Vector4 GetVector ( string _propertyName )
	{
		return serializedPropertyValues[_propertyName].vectorValue;
	}

	public float GetFloat ( string _propertyName )
	{
		return serializedPropertyValues[_propertyName].floatValue;
	}

	#endregion

	#region Set

	public void SetColor ( string _propertyName, Color _color )
	{
		if (propertyBlock == null)
		{
			Debug.LogError("Property block is null");
			return;
		}

		foreach (Renderer r in attachedRenderer)
		{
			if (r == null)
				continue;

			serializedPropertyValues[_propertyName].colorValue = _color;
			r.GetPropertyBlock(propertyBlock);

			if (_color != material.GetColor(_propertyName))
			{
				serializedPropertyValues[_propertyName].hasChanged = true;
				propertyBlock.SetColor(_propertyName, _color);
				r.SetPropertyBlock(propertyBlock);
			}
			else
			{
#if UNITY_EDITOR
				serializedPropertyValues[_propertyName].hasChanged = false;
				r.SetPropertyBlock(GetMaterialPropertyBlockWithoutEmpty(r));
#else
					serializedPropertyValues[_propertyName].hasChanged = true;
					propertyBlock.SetColor(_propertyName, _color);
					r.SetPropertyBlock(propertyBlock);
#endif
			}
		}
	}

	public void SetVector ( string _propertyName, Vector4 _vector )
	{
		if (propertyBlock == null)
		{
			Debug.LogError("Property block is null");
			return;
		}

		foreach (Renderer r in attachedRenderer)
		{
			if (r == null)
				continue;

			serializedPropertyValues[_propertyName].vectorValue = _vector;
			r.GetPropertyBlock(propertyBlock);

			if (_vector != material.GetVector(_propertyName))
			{
				serializedPropertyValues[_propertyName].hasChanged = true;
				propertyBlock.SetVector(_propertyName, _vector);
				r.SetPropertyBlock(propertyBlock);
			}
			else
			{
#if UNITY_EDITOR
				serializedPropertyValues[_propertyName].hasChanged = false;
				r.SetPropertyBlock(GetMaterialPropertyBlockWithoutEmpty(r));
#else
					serializedPropertyValues[_propertyName].hasChanged = true;
					propertyBlock.SetVector(_propertyName, _vector);
					r.SetPropertyBlock(propertyBlock);
#endif
			}
		}
	}

	public void SetFloat ( string _propertyName, float _value )
	{
		if (propertyBlock == null)
		{
			Debug.LogError("Property block is null");
			return;
		}

		foreach (Renderer r in attachedRenderer)
		{
			if (r == null)
				continue;

			serializedPropertyValues[_propertyName].floatValue = _value;
			r.GetPropertyBlock(propertyBlock);

			if (_value != material.GetFloat(_propertyName))
			{
				serializedPropertyValues[_propertyName].hasChanged = true;
				propertyBlock.SetFloat(_propertyName, _value);
				r.SetPropertyBlock(propertyBlock);
			}
			else
			{
#if UNITY_EDITOR
				serializedPropertyValues[_propertyName].hasChanged = false;
				r.SetPropertyBlock(GetMaterialPropertyBlockWithoutEmpty(r));
#else
					serializedPropertyValues[_propertyName].hasChanged = true;
					propertyBlock.SetFloat(_propertyName, _value);
					r.SetPropertyBlock(propertyBlock);
#endif
			}
		}
	}

	#region DOTween Extensions

	public Tweener DOFloat ( float _to, string _propertyName, float _duration )
	{
		if (!serializedPropertyValues.ContainsKey(_propertyName))
		{
			Debug.LogError("Property " + _propertyName + " is not serialized on this instanced renderer (" + gameObject.name + ")");
			return null;
		}
		m_floatTweener = DOVirtual.Float(serializedPropertyValues[_propertyName].floatValue, _to, _duration, ( x ) =>
		{
			SetFloat(_propertyName, x);
		});
		m_floatTweener.id = this;
		return m_floatTweener;
	}

	public Tweener DOColor ( Color _to, string _propertyName, float _duration )
	{
		if (!serializedPropertyValues.ContainsKey(_propertyName))
		{
			Debug.LogError("Property " + _propertyName + " is not serialized on this instanced renderer (" + gameObject.name + ")");
			return null;
		}
		m_colorTweener = DOVirtual.Color(serializedPropertyValues[_propertyName].colorValue, _to, _duration, ( x ) =>
		{
			SetColor(_propertyName, x);
		});
		m_colorTweener.id = this;
		return m_colorTweener;
	}

	#endregion

	#endregion

	#region Reset

	public void ResetProperty ( string _propertyName )
	{
		if (attachedRenderer[0] == null)
			return;

		switch (serializedPropertyValues[_propertyName].type)
		{
			case ShaderPropertyType.Color:
				SetColor(_propertyName, material.GetColor(_propertyName));
				break;
			case ShaderPropertyType.Vector:
				SetVector(_propertyName, material.GetVector(_propertyName));
				break;
			case ShaderPropertyType.Float:
				SetFloat(_propertyName, material.GetFloat(_propertyName));
				break;
			case ShaderPropertyType.Range:
				SetFloat(_propertyName, material.GetFloat(_propertyName));
				break;
			case ShaderPropertyType.Texture:
				break;
			default:
				break;
		}
	}

	public void ResetAllProperty ()
	{
		List<string> keys = new List<string>();
		foreach (KeyValuePair<string, SerializedPropertyValue> kvp in serializedPropertyValues)
		{
			keys.Add(kvp.Key);
		}

		foreach (string k in keys)
		{
			if (serializedPropertyValues.ContainsKey(k))
				ResetProperty(k);
		}
	}

	private MaterialPropertyBlock GetMaterialPropertyBlockWithoutEmpty ( Renderer reference )
	{
		Material refMaterial = null;
		switch (this.reference)
		{
			case PropertyBlockReference.First:
				refMaterial = reference.sharedMaterial;
				break;
			case PropertyBlockReference.Self:
				refMaterial = material;
				break;
			default:
				break;
		}

		MaterialPropertyBlock newMaterialPropertyBlock = new MaterialPropertyBlock();

		List<string> obsoleteKeys = new List<string>();

		foreach (KeyValuePair<string, SerializedPropertyValue> kvp in serializedPropertyValues)
		{
			if (!refMaterial.HasProperty(kvp.Key))
			{
				obsoleteKeys.Add(kvp.Key);
				continue;
			}

			switch (kvp.Value.type)
			{
				case ShaderPropertyType.Color:
					if (kvp.Value.colorValue != refMaterial.GetColor(kvp.Key))
					{
						newMaterialPropertyBlock.SetColor(kvp.Key, kvp.Value.colorValue);
					}
					break;
				case ShaderPropertyType.Vector:
					if (kvp.Value.vectorValue != refMaterial.GetVector(kvp.Key))
					{
						newMaterialPropertyBlock.SetVector(kvp.Key, kvp.Value.vectorValue);
					}
					break;
				case ShaderPropertyType.Float:
					if (kvp.Value.floatValue != refMaterial.GetFloat(kvp.Key))
					{
						newMaterialPropertyBlock.SetFloat(kvp.Key, kvp.Value.floatValue);
					}
					break;
				case ShaderPropertyType.Range:
					if (kvp.Value.floatValue != refMaterial.GetFloat(kvp.Key))
					{
						newMaterialPropertyBlock.SetFloat(kvp.Key, kvp.Value.floatValue);
					}
					break;
				case ShaderPropertyType.Texture:
					break;
			}
		}

		foreach (string key in obsoleteKeys)
		{
			serializedPropertyValues.Remove(key);
		}

		return newMaterialPropertyBlock;
	}

	#endregion

	#region Serialized Property

	public bool AddSerializedProperty ( int _index, Material _material )
	{
		Shader shader = _material.shader;
		string propertyName = shader.GetPropertyName(_index);

		if (serializedPropertyValues.ContainsKey(propertyName))
			return false;

		ShaderPropertyType type = shader.GetPropertyType(_index);
		switch (type)
		{
			case ShaderPropertyType.Color:
				serializedPropertyValues.Add(propertyName, new SerializedPropertyValue() { type = type, colorValue = _material.GetColor(propertyName) });
				return true;
			case ShaderPropertyType.Vector:
				serializedPropertyValues.Add(propertyName, new SerializedPropertyValue() { type = type, vectorValue = _material.GetVector(propertyName) });
				return true;
			case ShaderPropertyType.Float:
				serializedPropertyValues.Add(propertyName, new SerializedPropertyValue() { type = type, floatValue = _material.GetFloat(propertyName) });
				return true;
			case ShaderPropertyType.Range:
				serializedPropertyValues.Add(propertyName, new SerializedPropertyValue() { type = type, floatValue = _material.GetFloat(propertyName) });
				return true;
			case ShaderPropertyType.Texture:
				return false;
			default:
				return false;
		}
	}

	public bool ShouldDrawProperty ( int _index, out string _header )
	{
		Shader shader = material.shader;
		string showIfParameter = string.Empty;
		string showOnlyIfParameter = string.Empty;
		_header = string.Empty;

		//if ((shader.GetPropertyFlags(_index) & ShaderPropertyFlags.PerRendererData) == 0)
		//	return false;

		if ((shader.GetPropertyFlags(_index) & ShaderPropertyFlags.HideInInspector) > 0)
			return false;

		foreach (string a in shader.GetPropertyAttributes(_index))
		{
			if (Regex.IsMatch(a, "Toggle", RegexOptions.IgnoreCase))
			{
				return false;
			}
			else if (Regex.IsMatch(a, "Enum", RegexOptions.IgnoreCase))
			{
				return false;
			}


			string[] shaderKeyWords = material.shaderKeywords;

			if (Regex.IsMatch(a, "ShowIf", RegexOptions.IgnoreCase))
			{
				showIfParameter = Regex.Replace(a, "[()]", "");
				showIfParameter = showIfParameter.Substring("ShowIf".Length);
			}
			if (Regex.IsMatch(a, "ShowOnlyIf", RegexOptions.IgnoreCase))
			{
				showOnlyIfParameter = Regex.Replace(a, "[()]", "");
				showOnlyIfParameter = showOnlyIfParameter.Substring("ShowOnlyIf".Length);
			}


			bool showOnlyIfMatch = false;
			if (!string.IsNullOrEmpty(showOnlyIfParameter))
			{
				foreach (string keyWord in shaderKeyWords)
				{
					if (Regex.IsMatch(keyWord, showOnlyIfParameter, RegexOptions.IgnoreCase))
					{
						showOnlyIfMatch = true;
					}
				}

				if (!showOnlyIfMatch)
					return false;
			}

			bool showIfMatch = false;
			if (!string.IsNullOrEmpty(showIfParameter))
			{
				string[] splitParameter = showIfParameter.Split(',');
				foreach (string p in splitParameter)
				{
					foreach (string keyWord in shaderKeyWords)
					{
						if (Regex.IsMatch(keyWord, p.Trim(), RegexOptions.IgnoreCase))
						{
							showIfMatch = true;
						}
					}
				}

				if (!showIfMatch)
					return false;

			}

			if (Regex.IsMatch(a, "Header", RegexOptions.IgnoreCase))
			{
				_header = Regex.Replace(a, "[()]", "");
				_header = _header.Substring("Header".Length);
			}
		}

		return true;
	}

	public void ClearSerializedProperty ()
	{
		SerializableDictionary<string, SerializedPropertyValue> changedProperties = new();

		foreach (KeyValuePair<string, SerializedPropertyValue> kvp in serializedPropertyValues)
		{
			if (kvp.Value.hasChanged)
			{
				changedProperties.Add(kvp.Key, kvp.Value);
			}
		}

		serializedPropertyValues.Clear();

		foreach (KeyValuePair<string, SerializedPropertyValue> kvp in changedProperties)
		{
			serializedPropertyValues.Add(kvp.Key, kvp.Value);
		}
	}

	public void OnUndo ()
	{
		foreach (Renderer r in attachedRenderer)
		{
			r.SetPropertyBlock(GetMaterialPropertyBlockWithoutEmpty(r));
		}
	}

	[System.Serializable]
	public class SerializedPropertyValue
	{
		public ShaderPropertyType type;
		public float floatValue;
		public Color colorValue;
		public Vector4 vectorValue;
		public bool hasChanged;
	}

	#endregion
}