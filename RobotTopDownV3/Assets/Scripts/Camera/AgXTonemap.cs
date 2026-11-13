using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AgXTonemap : MonoBehaviour
{
	private Material m_material;

	public void Init ( Material _material )
	{
		m_material = _material;
	}

	private void OnRenderImage ( RenderTexture _source, RenderTexture _destination )
	{
		if (m_material == null)
		{
			Graphics.Blit(_source, _destination);
			return;
		}

		Graphics.Blit(_source, _destination, m_material);
	}
}
