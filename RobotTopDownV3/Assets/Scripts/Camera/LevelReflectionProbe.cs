using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ReflectionProbe))]
public class LevelReflectionProbe : MonoBehaviour
{
	[SerializeField] private bool m_captureOnEnable;
	[SerializeField] private float m_captureHeight = 1.5f;
	[SerializeField] private float m_horizontalMargin = 4f;
	[SerializeField] private float m_boxHeight = 8f;
	[SerializeField] private float m_depthBelowFloor = 1f;

	private ReflectionProbe m_probe;
	private Scene m_ownerScene;

	private ReflectionProbe Probe => m_probe != null ? m_probe : m_probe = GetComponent<ReflectionProbe>();

	private void OnEnable ()
	{
		if (m_captureOnEnable)
			Probe.RenderProbe();
	}

	private void OnDestroy ()
	{
		SceneManager.sceneUnloaded -= OnSceneUnloaded;
	}

	public void CaptureArea ( Bounds _area, Scene _ownerScene )
	{
		float floor = _area.min.y;
		transform.SetPositionAndRotation(new Vector3(_area.center.x, floor + m_captureHeight, _area.center.z), Quaternion.identity);
		Probe.size = new Vector3(_area.size.x + 2f * m_horizontalMargin, m_boxHeight, _area.size.z + 2f * m_horizontalMargin);
		Probe.center = new Vector3(0f, m_boxHeight * .5f - m_depthBelowFloor - m_captureHeight, 0f);

		m_ownerScene = _ownerScene;
		SceneManager.sceneUnloaded -= OnSceneUnloaded;
		SceneManager.sceneUnloaded += OnSceneUnloaded;

		gameObject.SetActive(true);
		Probe.RenderProbe();
	}

	private void OnSceneUnloaded ( Scene _scene )
	{
		if (_scene != m_ownerScene)
			return;

		SceneManager.sceneUnloaded -= OnSceneUnloaded;
		gameObject.SetActive(false);
	}
}
