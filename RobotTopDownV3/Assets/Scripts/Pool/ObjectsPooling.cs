using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;

[DisallowMultipleComponent()]
[RequireComponent(typeof(LoadingElement))]
public class ObjectsPooling : Singleton<ObjectsPooling>
{
	private const float k_prewarmFrameBudget = .004f;

	private static readonly Dictionary<Type, FieldInfo[]> s_poolFieldsPerType = new();

	[SerializeField] private LoadingElement m_loadingElement;
	[SerializeField] private Transform m_mainParent;
	[SerializeField] private PoolData[] m_pools;

	private Dictionary<PoolData, Stack<PoolElement>> m_availablePoolElements = new();
	private Dictionary<PoolData, List<PoolElement>> m_everyPoolElements = new();
	private Dictionary<PoolData, Transform> m_everyPoolTfm = new();

	public override void Awake ()
	{
		base.Awake();
		m_loadingElement.onLoadingStarted += Load;
	}

	public void Load ()
	{
		m_loadingElement.onLoadingStarted -= Load;
		StartCoroutine(LoadCoroutine());
	}

	IEnumerator LoadCoroutine ()
	{
		foreach (PoolData pool in m_pools)
		{
			RegisterPool(pool);
			yield return null;
		}

		m_loadingElement.EndLoading(true);
	}

	public void Prewarm ( IEnumerable<PoolData> _pools )
	{
		StartCoroutine(PrewarmRoutine(_pools));
	}

	private IEnumerator PrewarmRoutine ( IEnumerable<PoolData> _pools )
	{
		Queue<PoolData> pending = new(_pools);
		HashSet<PoolData> visited = new(pending);
		HashSet<PoolData> chained = new();
		float frameStart = Time.realtimeSinceStartup;

		while (pending.Count > 0)
		{
			PoolData pool = pending.Dequeue();
			if (!RegisterPool(pool, false))
				continue;

			chained.Clear();
			foreach (MonoBehaviour behaviour in pool.baseElement.GetComponentsInChildren<MonoBehaviour>(true))
				CollectPools(behaviour, chained);
			foreach (PoolData chainedPool in chained)
			{
				if (visited.Add(chainedPool))
					pending.Enqueue(chainedPool);
			}

			while (pool && m_everyPoolElements.TryGetValue(pool, out List<PoolElement> elements) && elements.Count < pool.size)
			{
				PopulatePool(pool);
				if (Time.realtimeSinceStartup - frameStart > k_prewarmFrameBudget)
				{
					yield return null;
					frameStart = Time.realtimeSinceStartup;
				}
			}
		}
	}

	public static void CollectPools ( UnityEngine.Object _source, HashSet<PoolData> _pools )
	{
		if (_source == null)
			return;

		foreach (FieldInfo field in GetPoolFields(_source.GetType()))
		{
			object value = field.GetValue(_source);
			if (value is PoolData pool)
			{
				if (pool != null)
					_pools.Add(pool);
			}
			else if (value is IEnumerable<PoolData> pools)
			{
				foreach (PoolData listedPool in pools)
				{
					if (listedPool != null)
						_pools.Add(listedPool);
				}
			}
		}
	}

	private static FieldInfo[] GetPoolFields ( Type _type )
	{
		if (s_poolFieldsPerType.TryGetValue(_type, out FieldInfo[] cachedFields))
			return cachedFields;

		List<FieldInfo> fields = new();
		for (Type type = _type; type != null && type != typeof(MonoBehaviour) && type != typeof(ScriptableObject); type = type.BaseType)
		{
			foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (typeof(PoolData).IsAssignableFrom(field.FieldType) || typeof(IEnumerable<PoolData>).IsAssignableFrom(field.FieldType))
					fields.Add(field);
			}
		}

		cachedFields = fields.ToArray();
		s_poolFieldsPerType.Add(_type, cachedFields);
		return cachedFields;
	}

	private bool RegisterPool ( PoolData _pool, bool _isPopulated = true )
	{
		if (!_pool)
			return false;

		if (m_availablePoolElements.ContainsKey(_pool))
			return true;

		if (!_pool.baseElement)
		{
			Debug.LogError("The pool " + _pool.name + " has no pool element assigned");
			return false;
		}

		if (m_everyPoolTfm.ContainsKey(_pool) == false)
		{
			GameObject newParent = new GameObject(_pool.name);
			newParent.transform.parent = m_mainParent;
			m_everyPoolTfm.Add(_pool, newParent.transform);
		}

		m_availablePoolElements.Add(_pool, new());
		m_everyPoolElements.Add(_pool, new());

		if (_isPopulated)
		{
			for (int i = 0; i < _pool.size; i++)
				PopulatePool(_pool);
		}

		return true;
	}

	public static PoolElement GetElement ( PoolData _pool, Vector3 _position = default, Quaternion _rotation = default )
	{
		if (!_pool)
		{
			Debug.LogError("unable to get the pool element, the pool is null");
			return null;
		}

		if (!Instance.m_availablePoolElements.ContainsKey(_pool) && !Instance.RegisterPool(_pool))
			return null;

		if (Instance.m_availablePoolElements[_pool].Count <= 0)
			Instance.PopulatePool(_pool);

		PoolElement output = Instance.m_availablePoolElements[_pool].Pop();
		output.transform.SetPositionAndRotation(_position, _rotation);
		output.transform.localScale = Vector3.one;
		output.gameObject.SetActive(true);
		output.OnStartUse();

		return output;
	}

	private void OnDiscarded ( PoolElement _element )
	{
		_element.gameObject.SetActive(false);

		if (m_everyPoolTfm.TryGetValue(_element.Pool, out Transform poolParent) && _element.transform.parent != poolParent)
			_element.transform.SetParent(poolParent, false);

		m_availablePoolElements[_element.Pool].Push(_element);
	}

	private void PopulatePool ( PoolData _pool )
	{
		PoolElement element = Instantiate(_pool.baseElement, m_everyPoolTfm[_pool]);
		element.Init(_pool);
		element.onDiscard += OnDiscarded;
		m_everyPoolElements[_pool].Add(element);
		OnDiscarded(element);
	}

	public void DiscardEverything ()
	{
		foreach (KeyValuePair<PoolData, List<PoolElement>> item in m_everyPoolElements)
		{
			foreach (var element in item.Value)
			{
				OnDiscarded(element);
			}
		}
	}
}