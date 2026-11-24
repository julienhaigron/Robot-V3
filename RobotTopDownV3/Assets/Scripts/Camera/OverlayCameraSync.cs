using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OverlayCameraSync : MonoBehaviour
{
	[SerializeField] private Camera m_source;

	private Camera m_camera;

	private void Awake ()
	{
		m_camera = GetComponent<Camera>();
	}

	private void OnPreCull ()
	{
		if (m_source == null)
			return;

		transform.SetPositionAndRotation(m_source.transform.position, m_source.transform.rotation);
		m_camera.fieldOfView = m_source.fieldOfView;
		m_camera.nearClipPlane = m_source.nearClipPlane;
		m_camera.farClipPlane = m_source.farClipPlane;
	}
}
