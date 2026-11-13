using UnityEngine;

public class HubTonemapping : MonoBehaviour
{
	private static readonly int s_exposureId = Shader.PropertyToID("_Exposure");

	[SerializeField] private Shader m_shader;
	[SerializeField] private float m_exposure = 0f;

	private Material m_material;
	private AgXTonemap m_tonemap;
	private bool m_didStart;

	private void Start ()
	{
		m_didStart = true;
		Attach();
	}

	private void OnEnable ()
	{
		if (m_didStart)
			Attach();
	}

	private void Attach ()
	{
		Camera camera = CameraManager.Instance != null ? CameraManager.Instance.Camera : Camera.main;
		if (camera == null || m_shader == null)
			return;

		m_material = new Material(m_shader) { hideFlags = HideFlags.HideAndDontSave };
		ApplyExposure();

		m_tonemap = camera.gameObject.AddComponent<AgXTonemap>();
		m_tonemap.Init(m_material);
	}

	private void OnDisable ()
	{
		if (m_tonemap != null)
			Destroy(m_tonemap);
		if (m_material != null)
			Destroy(m_material);

		m_tonemap = null;
		m_material = null;
	}

	private void OnValidate ()
	{
		ApplyExposure();
	}

	private void ApplyExposure ()
	{
		if (m_material != null)
			m_material.SetFloat(s_exposureId, Mathf.Pow(2f, m_exposure));
	}
}
