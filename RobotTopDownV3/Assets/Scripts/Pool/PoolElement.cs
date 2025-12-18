using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PoolElement : MonoBehaviour
{
	public Action<PoolElement> onDiscard;

	[Header("Pooling")]
	[SerializeField] private bool m_isDiscardedAfterTime = false;
	[ShowIf("@m_isDiscardedAfterTime"), SerializeField] private float m_discardAfter = 20f;

	private PoolData m_pool;
	public PoolData Pool => m_pool;

	private Coroutine m_discardCR;

	public virtual void Init ( PoolData _pool )
	{
		m_pool = _pool;
	}

	public virtual void OnStartUse ()
	{
		if (m_isDiscardedAfterTime && m_discardAfter > 0f)
			Discard(m_discardAfter);
	}

	public virtual void Discard ()
	{
		if (m_discardCR != null)
			StopCoroutine(m_discardCR);

		onDiscard?.Invoke(this);
	}

	public void Discard ( float _delay )
	{
		if (!gameObject.activeInHierarchy)
			return;

		if (m_discardCR != null)
			StopCoroutine(m_discardCR);

		m_discardCR = StartCoroutine(DiscardCR(_delay));
	}

	private IEnumerator DiscardCR ( float _delay )
	{
		yield return new WaitForSeconds(_delay);
		Discard();
	}

	private void OnDestroy ()
	{
		onDiscard = null;
	}
}