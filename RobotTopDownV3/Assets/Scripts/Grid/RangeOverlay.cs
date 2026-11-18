using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RangeOverlay : MonoBehaviour
{
	private const string k_materialResourcePath = "RangeOverlay";
	private const float k_heightAboveGround = 0.01f;
	private const int k_bytesPerTile = 4;
	private const int k_rangeChannel = 0;
	private const int k_aoeChannel = 1;
	private const int k_targetChannel = 2;
	private const int k_visionChannel = 3;
	private const byte k_visionValue = 192;
	private const byte k_wallFlag = 32;
	private const string k_wallKeyword = "RANGE_OVERLAY_WALLS";
	private const float k_wallVolumeHeight = 4f;

	private static readonly int s_rangeDataID = Shader.PropertyToID("_RangeData");
	private static readonly int s_rangeGridID = Shader.PropertyToID("_RangeGrid");
	private static readonly int s_rangeColorID = Shader.PropertyToID("_RangeColor");
	private static readonly int s_aoeColorID = Shader.PropertyToID("_AoEColor");
	private static readonly int s_targetColorID = Shader.PropertyToID("_TargetColor");
	private static readonly int s_visionTypeID = Shader.PropertyToID("_VisionType");
	private static readonly int s_visionCenterID = Shader.PropertyToID("_VisionCenter");
	private static readonly int s_overlayCullID = Shader.PropertyToID("_OverlayCull");
	private static readonly int s_overlayZTestID = Shader.PropertyToID("_OverlayZTest");
	private static readonly int s_radarWaveRateID = Shader.PropertyToID("_RadarWaveRate");
	private static readonly int s_radarWaveCountID = Shader.PropertyToID("_RadarWaveCount");
	private static readonly int s_radarPingCenterID = Shader.PropertyToID("_RadarPingCenter");
	private static readonly int s_radarPingTimingID = Shader.PropertyToID("_RadarPingTiming");

	private static RangeOverlay s_instance;

	private Material m_material;
	private MeshFilter m_meshFilter;
	private MeshRenderer m_renderer;
	private Material m_wallMaterial;
	private MeshFilter m_wallMeshFilter;
	private MeshRenderer m_wallRenderer;
	private MaterialPropertyBlock m_propertyBlock;
	private Texture2D m_rangeData;
	private byte[] m_rangeBytes;
	private bool m_isDirty;

	private readonly List<Tile> m_rangeTiles = new();
	private Color m_rangeColor;
	private bool m_isRangeHidden;
	private readonly List<Tile> m_previewTiles = new();
	private Color m_previewColor;
	private readonly List<Tile> m_aoeTiles = new();
	private Color m_aoeColor;
	private readonly List<Tile> m_targetTiles = new();
	private Color m_targetColor;
	private readonly List<Tile> m_visionTiles = new();
	private NeuronalMembraneEquipmentData.VisionTypes m_visionType;
	private Vector4 m_visionCenter = new(0f, 0f, 1f, 0f);
	private Tile m_visionOrigin;

	public static void SetVisionPreview ( IEnumerable<Tile> _tiles, NeuronalMembraneEquipmentData.VisionTypes _visionType, Tile _origin )
	{
		RangeOverlay overlay = GetOrCreate();
		if (overlay == null)
			return;

		overlay.m_visionType = _visionType;
		overlay.m_visionOrigin = _origin;
		overlay.Replace(overlay.m_visionTiles, _tiles);
		overlay.m_visionCenter = overlay.GetVisionCenter(_origin);
	}

	private Vector4 GetVisionCenter ( Tile _origin )
	{
		if (_origin == null)
			return new Vector4(0f, 0f, 1f, 0f);

		Vector3 center = transform.InverseTransformPoint(_origin.transform.position);
		float radius = 0f;
		foreach (Tile tile in m_visionTiles)
		{
			if (tile == null)
				continue;

			Vector3 position = transform.InverseTransformPoint(tile.transform.position);
			radius = Mathf.Max(radius, new Vector2(position.x - center.x, position.z - center.z).magnitude);
		}

		return new Vector4(center.x, center.z, radius + Tile.innerRadius, 0f);
	}

	public static void ClearVisionPreview ()
	{
		if (s_instance != null)
			s_instance.Replace(s_instance.m_visionTiles, null);
	}

	public static void SetRange ( IEnumerable<Tile> _tiles, Color _color )
	{
		RangeOverlay overlay = GetOrCreate();
		if (overlay == null)
			return;

		overlay.m_rangeColor = _color;
		overlay.Replace(overlay.m_rangeTiles, _tiles);
	}

	public static void ClearRange ()
	{
		if (s_instance != null)
			s_instance.Replace(s_instance.m_rangeTiles, null);
	}

	public static void SetRangeHidden ( bool _isHidden )
	{
		if (s_instance == null || s_instance.m_isRangeHidden == _isHidden)
			return;

		s_instance.m_isRangeHidden = _isHidden;
		s_instance.m_isDirty = true;
	}

	public static void SetPreview ( IEnumerable<Tile> _tiles, Color _color )
	{
		RangeOverlay overlay = GetOrCreate();
		if (overlay == null)
			return;

		overlay.m_previewColor = _color;
		overlay.Replace(overlay.m_previewTiles, _tiles);
	}

	public static void ClearPreview ()
	{
		if (s_instance != null)
			s_instance.Replace(s_instance.m_previewTiles, null);
	}

	public static void SetAoEPreview ( IEnumerable<Tile> _tiles, Color _color )
	{
		RangeOverlay overlay = GetOrCreate();
		if (overlay == null)
			return;

		overlay.m_aoeColor = _color;
		overlay.Replace(overlay.m_aoeTiles, _tiles);
	}

	public static void ClearAoEPreview ()
	{
		if (s_instance != null)
			s_instance.Replace(s_instance.m_aoeTiles, null);
	}

	public static void SetTargets ( IEnumerable<Tile> _tiles, Color _color )
	{
		RangeOverlay overlay = GetOrCreate();
		if (overlay == null)
			return;

		overlay.m_targetColor = _color;
		overlay.Replace(overlay.m_targetTiles, _tiles);
	}

	public static void ClearTargets ()
	{
		if (s_instance != null)
			s_instance.Replace(s_instance.m_targetTiles, null);
	}

	public static void ClearAll ()
	{
		if (s_instance == null)
			return;

		s_instance.Replace(s_instance.m_rangeTiles, null);
		s_instance.Replace(s_instance.m_previewTiles, null);
		s_instance.Replace(s_instance.m_aoeTiles, null);
		s_instance.Replace(s_instance.m_targetTiles, null);
		s_instance.Replace(s_instance.m_visionTiles, null);
	}

	private static RangeOverlay GetOrCreate ()
	{
		if (s_instance != null)
			return s_instance;

		if (GridManager.Instance == null || GridManager.Instance.Tiles == null || GridManager.Instance.Tiles.Length == 0)
			return null;

		Material material = Resources.Load<Material>(k_materialResourcePath);
		if (material == null)
		{
			Debug.LogError("RangeOverlay: missing material Resources/" + k_materialResourcePath);
			return null;
		}

		GameObject overlayObject = new GameObject("RangeOverlay");
		overlayObject.transform.SetParent(GridManager.Instance.transform, false);
		s_instance = overlayObject.AddComponent<RangeOverlay>();
		s_instance.Init(material, GridManager.Instance.GridData.width, GridManager.Instance.GridData.height);
		return s_instance;
	}

	private void Init ( Material _material, int _width, int _height )
	{
		m_material = _material;
		m_rangeBytes = new byte[_width * _height * k_bytesPerTile];
		m_rangeData = new Texture2D(_width, _height, TextureFormat.RGBA32, false, true)
		{
			name = "RangeOverlayData",
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};

		m_meshFilter = gameObject.AddComponent<MeshFilter>();
		m_meshFilter.sharedMesh = BuildGridQuad(_width, _height);

		m_renderer = gameObject.AddComponent<MeshRenderer>();
		m_renderer.sharedMaterial = _material;
		ConfigureRenderer(m_renderer);

		GameObject wallObject = new GameObject("RangeOverlayWalls");
		wallObject.transform.SetParent(transform, false);
		m_wallMeshFilter = wallObject.AddComponent<MeshFilter>();
		m_wallMeshFilter.sharedMesh = BuildWallVolume(_width, _height);
		m_wallMaterial = new Material(_material) { name = "RangeOverlayWalls" };
		SyncWallMaterial();
		m_wallRenderer = wallObject.AddComponent<MeshRenderer>();
		m_wallRenderer.sharedMaterial = m_wallMaterial;
		ConfigureRenderer(m_wallRenderer);

		Camera mainCamera = Camera.main;
		if (mainCamera != null)
			mainCamera.depthTextureMode |= DepthTextureMode.Depth;

		m_propertyBlock = new MaterialPropertyBlock();
		m_propertyBlock.SetTexture(s_rangeDataID, m_rangeData);
		m_propertyBlock.SetVector(s_rangeGridID, new Vector4(_width, _height, Tile.innerRadius, Tile.outerRadius));

		m_isRangeHidden = PlayerController.Instance != null && PlayerController.Instance.AreInteractableOutlinesHidden;
	}

	private static void ConfigureRenderer ( MeshRenderer _renderer )
	{
		_renderer.shadowCastingMode = ShadowCastingMode.Off;
		_renderer.receiveShadows = false;
		_renderer.lightProbeUsage = LightProbeUsage.Off;
		_renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		_renderer.enabled = false;
	}

	private void SyncWallMaterial ()
	{
		m_wallMaterial.CopyPropertiesFromMaterial(m_material);
		m_wallMaterial.EnableKeyword(k_wallKeyword);
		m_wallMaterial.SetFloat(s_overlayCullID, (float)CullMode.Front);
		m_wallMaterial.SetFloat(s_overlayZTestID, (float)CompareFunction.Always);
	}

	private static void GetGridBounds ( int _width, int _height, out float _minX, out float _maxX, out float _minZ, out float _maxZ )
	{
		_minX = -Tile.innerRadius - Tile.outerRadius;
		_maxX = (_width - 0.5f) * Tile.innerRadius * 2f + Tile.innerRadius + Tile.outerRadius;
		_minZ = -Tile.outerRadius * 2f;
		_maxZ = (_height - 1) * Tile.outerRadius * 1.5f + Tile.outerRadius * 2f;
	}

	private static Mesh BuildWallVolume ( int _width, int _height )
	{
		GetGridBounds(_width, _height, out float minX, out float maxX, out float minZ, out float maxZ);

		Mesh mesh = new Mesh { name = "RangeOverlayWallVolume" };
		mesh.vertices = new Vector3[]
		{
			new Vector3(minX, 0f, minZ),
			new Vector3(maxX, 0f, minZ),
			new Vector3(maxX, k_wallVolumeHeight, minZ),
			new Vector3(minX, k_wallVolumeHeight, minZ),
			new Vector3(minX, 0f, maxZ),
			new Vector3(maxX, 0f, maxZ),
			new Vector3(maxX, k_wallVolumeHeight, maxZ),
			new Vector3(minX, k_wallVolumeHeight, maxZ)
		};
		mesh.triangles = new int[]
		{
			0, 3, 2, 0, 2, 1,
			5, 6, 7, 5, 7, 4,
			4, 7, 3, 4, 3, 0,
			1, 2, 6, 1, 6, 5,
			1, 5, 4, 1, 4, 0,
			3, 7, 6, 3, 6, 2
		};
		mesh.RecalculateBounds();
		return mesh;
	}

	private static Mesh BuildGridQuad ( int _width, int _height )
	{
		GetGridBounds(_width, _height, out float minX, out float maxX, out float minZ, out float maxZ);

		Mesh mesh = new Mesh { name = "RangeOverlayQuad" };
		mesh.vertices = new Vector3[]
		{
			new Vector3(minX, k_heightAboveGround, minZ),
			new Vector3(minX, k_heightAboveGround, maxZ),
			new Vector3(maxX, k_heightAboveGround, maxZ),
			new Vector3(maxX, k_heightAboveGround, minZ)
		};
		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		mesh.RecalculateBounds();
		return mesh;
	}

	private void Replace ( List<Tile> _layer, IEnumerable<Tile> _tiles )
	{
		if (_tiles == null && _layer.Count == 0)
			return;

		_layer.Clear();
		if (_tiles != null)
			_layer.AddRange(_tiles);

		m_isDirty = true;
	}

	private void LateUpdate ()
	{
		if (m_isDirty)
			Redraw();
#if UNITY_EDITOR
		else if (m_wallRenderer.enabled)
		{
			SyncWallMaterial();
			UpdateRadarPing(true);
		}
#endif
	}

	private void Redraw ()
	{
		m_isDirty = false;
		System.Array.Clear(m_rangeBytes, 0, m_rangeBytes.Length);

		bool hasPreview = m_previewTiles.Count > 0;
		List<Tile> rangeTiles = hasPreview ? m_previewTiles : m_isRangeHidden ? null : m_rangeTiles;

		bool isVisible = WriteLayer(rangeTiles, k_rangeChannel, true, byte.MaxValue);
		isVisible |= WriteLayer(m_aoeTiles, k_aoeChannel, false, byte.MaxValue);
		isVisible |= WriteLayer(m_targetTiles, k_targetChannel, false, byte.MaxValue);
		bool hasVision = WriteLayer(m_isRangeHidden ? null : m_visionTiles, k_visionChannel, false, k_visionValue);
		isVisible |= hasVision;

		if (hasVision)
			WriteWallFlags();

		if (isVisible)
		{
			m_rangeData.SetPixelData(m_rangeBytes, 0);
			m_rangeData.Apply(false);
			m_propertyBlock.SetColor(s_rangeColorID, hasPreview ? m_previewColor : m_rangeColor);
			m_propertyBlock.SetColor(s_aoeColorID, m_aoeColor);
			m_propertyBlock.SetColor(s_targetColorID, m_targetColor);
			m_propertyBlock.SetFloat(s_visionTypeID, (int)m_visionType);
			m_propertyBlock.SetVector(s_visionCenterID, m_visionCenter);
			m_renderer.SetPropertyBlock(m_propertyBlock);
			m_wallRenderer.SetPropertyBlock(m_propertyBlock);
		}

		if (hasVision)
			SyncWallMaterial();

		m_renderer.enabled = isVisible;
		m_wallRenderer.enabled = hasVision;
		UpdateRadarPing(hasVision);
	}

	private void UpdateRadarPing ( bool _hasVision )
	{
		if (!_hasVision || m_visionType != NeuronalMembraneEquipmentData.VisionTypes.Radar || m_visionOrigin == null)
		{
			Shader.SetGlobalVector(s_radarPingCenterID, Vector4.zero);
			return;
		}

		Vector3 origin = m_visionOrigin.transform.position;
		float waveCount = Mathf.Max(1f, Mathf.Round(m_material.GetFloat(s_radarWaveCountID)));
		Shader.SetGlobalVector(s_radarPingCenterID, new Vector4(origin.x, origin.z, m_visionCenter.z * transform.lossyScale.x, 1f));
		Shader.SetGlobalVector(s_radarPingTimingID, new Vector4(m_material.GetFloat(s_radarWaveRateID), waveCount, 0f, 0f));
	}

	private void WriteWallFlags ()
	{
		if (GridManager.Instance == null || GridManager.Instance.Tiles == null)
			return;

		foreach (Tile tile in GridManager.Instance.Tiles)
		{
			if (tile == null || !IsStandingWall(tile))
				continue;

			int index = tile.coordinates.ID * k_bytesPerTile + k_visionChannel;
			if (index >= 0 && index < m_rangeBytes.Length)
				m_rangeBytes[index] |= k_wallFlag;
		}
	}

	private static bool IsStandingWall ( Tile _tile )
	{
		if (_tile.Wall == null || _tile.Wall.Health <= 0)
			return false;

		return _tile.GroundType == TileGroundType.Wall || _tile.GroundType == TileGroundType.Cover || _tile.IsSabotagePoint;
	}

	private bool WriteLayer ( List<Tile> _tiles, int _channel, bool _isFloorOnly, byte _value )
	{
		if (_tiles == null)
			return false;

		bool hasTile = false;
		foreach (Tile tile in _tiles)
		{
			if (tile == null || (_isFloorOnly && !IsFloor(tile)))
				continue;

			int index = tile.coordinates.ID * k_bytesPerTile + _channel;
			if (index < 0 || index >= m_rangeBytes.Length)
				continue;

			m_rangeBytes[index] |= _value;
			hasTile = true;
		}

		return hasTile;
	}

	private static bool IsFloor ( Tile _tile )
	{
		if (_tile.GroundType == TileGroundType.Void)
			return false;

		return _tile.GroundType != TileGroundType.Wall || _tile.Wall == null || _tile.Wall.Health <= 0;
	}

	private void OnDestroy ()
	{
		if (s_instance == this)
		{
			s_instance = null;
			Shader.SetGlobalVector(s_radarPingCenterID, Vector4.zero);
		}

		if (m_rangeData != null)
			Destroy(m_rangeData);

		if (m_meshFilter != null && m_meshFilter.sharedMesh != null)
			Destroy(m_meshFilter.sharedMesh);

		if (m_wallMeshFilter != null && m_wallMeshFilter.sharedMesh != null)
			Destroy(m_wallMeshFilter.sharedMesh);

		if (m_wallMaterial != null)
			Destroy(m_wallMaterial);
	}
}
