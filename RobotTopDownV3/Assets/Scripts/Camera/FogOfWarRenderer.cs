using UnityEngine;

public class FogOfWarRenderer : Singleton<FogOfWarRenderer>
{
	public Camera fogCamera;
	public Material fogApplyMaterial; 
	public Camera mainCamera;

	[SerializeField] private float m_topDownHeight = 50f;
	[SerializeField] private float m_gridPadding = 2f;

	private RenderTexture m_fogMask;
	private RenderTexture m_visibleMask;
	private Shader m_fogShader;
	private bool m_dirty = true;

	private Vector2 m_gridOrigin;
	private float m_gridSize;
	private bool m_gridConfigured = false;

	void Start ()
	{
		fogCamera.transform.SetParent(null, true);

		mainCamera.depthTextureMode |= DepthTextureMode.Depth;
		int maskResolution = 1024;

		m_fogMask = new RenderTexture(maskResolution, maskResolution, 16);

		m_visibleMask = new RenderTexture(maskResolution, maskResolution, 16, RenderTextureFormat.R8);
		m_visibleMask.filterMode = FilterMode.Point;
		m_visibleMask.wrapMode = TextureWrapMode.Clamp;

		fogApplyMaterial.SetTexture("_FogMask", m_fogMask);
		fogApplyMaterial.SetTexture("_VisibleMask", m_visibleMask);

		m_fogShader = Shader.Find("Custom/FogOfWar_Mask");
		ConfigureTopDownFogCamera();
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
		m_gridOrigin = new Vector2(gridCenter.x - fogCamera.orthographicSize, gridCenter.z - fogCamera.orthographicSize);

		fogApplyMaterial.SetVector("_FogGridOrigin", m_gridOrigin);
		fogApplyMaterial.SetFloat("_FogGridSize", m_gridSize);

		m_gridConfigured = true;
	}

	public void MarkDirty ()
	{
		if (!m_gridConfigured)
			ConfigureTopDownFogCamera();

		m_dirty = true;
	}

	void LateUpdate ()
	{
		Matrix4x4 view = mainCamera.worldToCameraMatrix;
		Matrix4x4 proj = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false);
		Matrix4x4 vp = proj * view;

		Shader.SetGlobalMatrix("_FogMainCamInvVP", vp.inverse);

		if (!m_dirty)
			return;
		m_dirty = false;

		if (m_fogMask == null || m_visibleMask == null)
			return;

		fogCamera.targetTexture = m_fogMask;
		fogCamera.RenderWithShader(m_fogShader, "");

		fogCamera.targetTexture = m_visibleMask;
		fogCamera.RenderWithShader(m_fogShader, "");

		fogCamera.targetTexture = m_fogMask;
	}
}