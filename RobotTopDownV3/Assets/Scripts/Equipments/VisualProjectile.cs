using UnityEngine;

public class VisualProjectile : PoolElement
{
	[SerializeField] private Transform m_spin;
	[SerializeField] private Vector3 m_spinAxis = Vector3.up;
	[SerializeField] private float m_spinSpeed;
	[SerializeField] private float m_releaseDelay = .3f;

	private TrailRenderer[] m_trails;
	private Renderer[] m_heads;
	private bool m_isVisible = true;

	public override void Init ( PoolData _pool )
	{
		base.Init(_pool);

		m_trails = GetComponentsInChildren<TrailRenderer>(true);
		System.Collections.Generic.List<Renderer> heads = new();
		foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
		{
			if (renderer is not TrailRenderer)
				heads.Add(renderer);
		}
		m_heads = heads.ToArray();
	}

	public override void OnStartUse ()
	{
		base.OnStartUse();

		m_isVisible = true;
		SetHeadsVisible(true);
		foreach (TrailRenderer trail in m_trails)
		{
			trail.enabled = true;
			trail.Clear();
			trail.emitting = true;
		}
	}

	public void SetVisible ( bool _isVisible )
	{
		m_isVisible = _isVisible;
		SetHeadsVisible(_isVisible);
		foreach (TrailRenderer trail in m_trails)
			trail.enabled = _isVisible;
	}

	public void Place ( Vector3 _position, Vector3 _direction )
	{
		transform.position = _position;
		if (_direction.sqrMagnitude > .0001f)
			transform.rotation = Quaternion.LookRotation(_direction);
	}

	public void Release ()
	{
		SetHeadsVisible(false);
		foreach (TrailRenderer trail in m_trails)
			trail.emitting = false;

		if (m_releaseDelay > 0f && gameObject.activeInHierarchy)
			Discard(m_releaseDelay);
		else
			Discard();
	}

	private void SetHeadsVisible ( bool _isVisible )
	{
		foreach (Renderer head in m_heads)
		{
			if (head != null)
				head.enabled = _isVisible && m_isVisible;
		}
	}

	private void Update ()
	{
		if (m_spin != null && m_spinSpeed != 0f)
			m_spin.Rotate(m_spinAxis, m_spinSpeed * Time.deltaTime, Space.Self);
	}
}
