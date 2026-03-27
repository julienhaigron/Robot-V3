using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class ObjectsPooling : SingletonPersistant<ObjectsPooling>
{
	[SerializeField] private Transform m_mainParent;
	[SerializeField] private PoolData[] m_pools;

	private Dictionary<PoolData, Stack<PoolElement>> m_availablePoolElements = new();
	private Dictionary<PoolData, List<PoolElement>> m_everyPoolElements = new();
	private Dictionary<PoolData, Transform> m_everyPoolTfm = new();
	private LoadingElement m_loadingElement;

	/*public override void Awake ()
	{
		base.Awake();
		m_loadingElement = GetComponent<LoadingElement>();
		m_loadingElement.onLoadingStarted += Load;
	}

	public void Load ()
	{
		m_loadingElement.onLoadingStarted -= Load;
		StartCoroutine(LoadCoroutine());
	}*/

	public override void Awake ()
	{
		base.Awake();
		StartCoroutine(LoadCoroutine());
	}

	IEnumerator LoadCoroutine ()
	{
		foreach (PoolData pool in m_pools)
		{
			if (!pool.baseElement)
			{
				Debug.LogError("The pool " + pool.name + " has no pool element assigned");
				continue;
			}

			if(m_everyPoolTfm.ContainsKey(pool) == false)
			{
				GameObject newParent = new GameObject(pool.name);
				newParent.transform.parent = m_mainParent;
				m_everyPoolTfm.Add(pool, newParent.transform);
			}

			m_availablePoolElements.Add(pool, new());
			m_everyPoolElements.Add(pool, new());

			for (int i = 0; i < pool.size; i++)
			{
				PopulatePool(pool);
			}

			yield return null;
		}

		//m_loadingElement.EndLoading(true);
	}

	public static PoolElement GetElement ( PoolData _pool, Vector3 _position = default, Quaternion _rotation = default )
	{
		if (!_pool)
		{
			Debug.LogError("unable to get the pool element, the pool is null");
			return null;
		}

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