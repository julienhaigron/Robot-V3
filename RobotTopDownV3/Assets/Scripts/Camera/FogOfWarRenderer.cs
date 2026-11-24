using UnityEngine;

public class FogOfWarRenderer : Singleton<FogOfWarRenderer>
{
	private const int c_blurPass = 0;
	private const int c_mixPass = 1;

	private static readonly int s_softMaskID = Shader.PropertyToID("_FogSoftMask");
	private static readonly int s_previousSoftMaskID = Shader.PropertyToID("_FogSoftMaskPrevious");
	private static readonly int s_maskBlendID = Shader.PropertyToID("_FogMaskBlend");
	private static readonly int s_visionLayersID = Shader.PropertyToID("_FogVisionLayers");
	private static readonly int s_gridOriginID = Shader.PropertyToID("_FogGridOrigin");
	private static readonly int s_gridSizeID = Shader.PropertyToID("_FogGridSize");
	private static readonly int s_mainCamInvVPID = Shader.PropertyToID("_FogMainCamInvVP");
	private static readonly int s_blurStepID = Shader.PropertyToID("_BlurStep");
	private static readonly int s_blendTargetID = Shader.PropertyToID("_BlendTarget");
	private static readonly int s_blendAmountID = Shader.PropertyToID("_BlendAmount");

	public Camera fogCamera;
	public Camera mainCamera;
	[SerializeField] private Material m_maskBlitMaterial;

	[SerializeField] private float m_topDownHeight = 50f;
	[SerializeField] private float m_gridPadding = 2f;
	[SerializeField] private int m_maskResolution = 1024;
	[SerializeField] private float m_edgeBlur = 0.25f;
	[SerializeField] private float m_transitionDuration = 0.3f;
	[SerializeField] private float m_visionLayersFadeDuration = 0.25f;

	private RenderTexture m_rawMask;
	private RenderTexture m_softMask;
	private RenderTexture m_previousSoftMask;
	private Shader m_maskShader;
	private bool m_dirty = true;
	private bool m_hasSoftMask;
	private float m_maskBlend = 1f;
	private float m_visionLayers;

	private float m_gridSize;
	private GridManager m_configuredGrid;

	public bool AreVisionLayersShown => GameDatas.current.app.fogVisionLayers;

	void Start ()
	{
		fogCamera.transform.SetParent(null, true);
		mainCamera.depthTextureMode |= DepthTextureMode.Depth;

		m_rawMask = new RenderTexture(m_maskResolution, m_maskResolution, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		m_softMask = CreateSoftMask();
		m_previousSoftMask = CreateSoftMask();
		m_maskShader = Shader.Find("Custom/FogOfWar_Mask");

		m_visionLayers = AreVisionLayersShown ? 1f : 0f;
		Shader.SetGlobalFloat(s_visionLayersID, m_visionLayers);
		ConfigureTopDownFogCamera();
	}

	void OnDestroy ()
	{
		Destroy(m_rawMask);
		Destroy(m_softMask);
		Destroy(m_previousSoftMask);
	}

	private RenderTexture CreateSoftMask ()
	{
		return new RenderTexture(m_maskResolution / 2, m_maskResolution / 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
		{
			wrapMode = TextureWrapMode.Clamp,
			filterMode = FilterMode.Bilinear
		};
	}

	public void ConfigureTopDownFogCamera ()
	{
		if (GridManager.Instance == null || GridManager.Instance.Tiles == null || GridManager.Instance.Tiles.Length == 0)
			return;

		float minX = float.MaxValue, maxX = float.MinValue;
		float minZ = float.MaxValue, maxZ = float.MinValue;

		foreach (Tile tile in GridManager.Instance.Tiles)
		{
			Vector3 pos = tile.transform.position;
			if (pos.x < minX) minX = pos.x;
			if (pos.x > maxX) maxX = pos.x;
			if (pos.z < minZ) minZ = pos.z;
			if (pos.z > maxZ) maxZ = pos.z;
		}

		minX -= Tile.outerRadius;
		maxX += Tile.outerRadius;
		minZ -= Tile.outerRadius;
		maxZ += Tile.outerRadius;

		float gridWidth = maxX - minX;
		float gridDepth = maxZ - minZ;

		Vector3 gridCenter = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

		fogCamera.transform.position = gridCenter + Vector3.up * m_topDownHeight;
		fogCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

		fogCamera.orthographic = true;
		fogCamera.orthographicSize = Mathf.Max(gridWidth, gridDepth) * 0.5f + m_gridPadding;
		fogCamera.aspect = 1f;
		fogCamera.clearFlags = CameraClearFlags.SolidColor;
		fogCamera.backgroundColor = Color.black;
		fogCamera.cullingMask = 1 << LayerMask.NameToLayer("FOW");

		m_gridSize = fogCamera.orthographicSize * 2f;
		Shader.SetGlobalVector(s_gridOriginID, new Vector2(gridCenter.x - fogCamera.orthographicSize, gridCenter.z - fogCamera.orthographicSize));
		Shader.SetGlobalFloat(s_gridSizeID, m_gridSize);

		m_configuredGrid = GridManager.Instance;
		m_hasSoftMask = false;
	}

	public void MarkDirty ()
	{
		if (m_configuredGrid != GridManager.Instance)
			ConfigureTopDownFogCamera();

		m_dirty = true;
	}

	public void SetVisionLayersShown ( bool _isShown )
	{
		GameDatas.current.app.fogVisionLayers = _isShown;
		ApplicationManager.Instance.SaveApplication();
	}

	void LateUpdate ()
	{
		Matrix4x4 viewProjection = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix;
		Shader.SetGlobalMatrix(s_mainCamInvVPID, viewProjection.inverse);

		if (m_dirty && m_configuredGrid != null)
			RenderSoftMask();

		AdvanceFades();
	}

	private void RenderSoftMask ()
	{
		m_dirty = false;
		if (m_hasSoftMask)
			KeepShownMaskAsPrevious();

		fogCamera.targetTexture = m_rawMask;
		fogCamera.RenderWithShader(m_maskShader, "");

		RenderTexture horizontalPass = RenderTexture.GetTemporary(m_softMask.descriptor);
		Graphics.Blit(m_rawMask, m_softMask);
		float blurStep = m_edgeBlur * 0.5f / m_gridSize;
		Shader.SetGlobalVector(s_blurStepID, new Vector4(blurStep, 0f));
		Graphics.Blit(m_softMask, horizontalPass, m_maskBlitMaterial, c_blurPass);
		Shader.SetGlobalVector(s_blurStepID, new Vector4(0f, blurStep));
		Graphics.Blit(horizontalPass, m_softMask, m_maskBlitMaterial, c_blurPass);
		RenderTexture.ReleaseTemporary(horizontalPass);

		m_maskBlend = m_hasSoftMask ? 0f : 1f;
		m_hasSoftMask = true;
		Shader.SetGlobalTexture(s_softMaskID, m_softMask);
		Shader.SetGlobalTexture(s_previousSoftMaskID, m_previousSoftMask);
		Shader.SetGlobalFloat(s_maskBlendID, m_maskBlend);
	}

	private void KeepShownMaskAsPrevious ()
	{
		if (m_maskBlend >= 1f)
		{
			(m_previousSoftMask, m_softMask) = (m_softMask, m_previousSoftMask);
			return;
		}

		RenderTexture shownMask = RenderTexture.GetTemporary(m_softMask.descriptor);
		Shader.SetGlobalTexture(s_blendTargetID, m_softMask);
		Shader.SetGlobalFloat(s_blendAmountID, m_maskBlend);
		Graphics.Blit(m_previousSoftMask, shownMask, m_maskBlitMaterial, c_mixPass);
		Graphics.Blit(shownMask, m_previousSoftMask);
		RenderTexture.ReleaseTemporary(shownMask);
	}

	private void AdvanceFades ()
	{
		if (m_maskBlend < 1f)
		{
			m_maskBlend = m_transitionDuration > 0f ? Mathf.Min(1f, m_maskBlend + Time.deltaTime / m_transitionDuration) : 1f;
			Shader.SetGlobalFloat(s_maskBlendID, m_maskBlend);
		}

		float targetLayers = AreVisionLayersShown ? 1f : 0f;
		if (m_visionLayers != targetLayers)
		{
			m_visionLayers = m_visionLayersFadeDuration > 0f ? Mathf.MoveTowards(m_visionLayers, targetLayers, Time.deltaTime / m_visionLayersFadeDuration) : targetLayers;
			Shader.SetGlobalFloat(s_visionLayersID, m_visionLayers);
		}
	}
}
