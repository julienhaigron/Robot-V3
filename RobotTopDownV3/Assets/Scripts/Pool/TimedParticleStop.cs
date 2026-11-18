using UnityEngine;

public class TimedParticleStop : MonoBehaviour
{
	[SerializeField] private float m_emitDuration = .6f;

	private ParticleSystem[] m_systems;
	private float m_stopTime;
	private bool m_isStopped;

	private void Awake ()
	{
		m_systems = GetComponentsInChildren<ParticleSystem>(true);
	}

	private void OnEnable ()
	{
		m_stopTime = Time.time + m_emitDuration;
		m_isStopped = false;
	}

	private void Update ()
	{
		if (m_isStopped || Time.time < m_stopTime)
			return;

		m_isStopped = true;
		foreach (ParticleSystem system in m_systems)
		{
			if (system != null)
				system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		}
	}
}
