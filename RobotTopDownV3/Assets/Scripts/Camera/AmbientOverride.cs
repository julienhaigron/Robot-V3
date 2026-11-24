using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class AmbientOverride : MonoBehaviour
{
	[SerializeField] private Color m_ambientColor = new(.38f, .42f, .5f);

	private SphericalHarmonicsL2 m_previousProbe;
	private Scene m_overriddenScene;

	public static SphericalHarmonicsL2 CreateFlatProbe ( Color _color )
	{
		SphericalHarmonicsL2 probe = new();
		probe.AddAmbientLight(QualitySettings.activeColorSpace == ColorSpace.Linear ? _color.linear : _color);
		return probe;
	}

	private void OnEnable ()
	{
		m_overriddenScene = SceneManager.GetActiveScene();
		m_previousProbe = RenderSettings.ambientProbe;
		RenderSettings.ambientProbe = CreateFlatProbe(m_ambientColor);
	}

	private void OnDisable ()
	{
		if (SceneManager.GetActiveScene() == m_overriddenScene)
			RenderSettings.ambientProbe = m_previousProbe;
	}
}
