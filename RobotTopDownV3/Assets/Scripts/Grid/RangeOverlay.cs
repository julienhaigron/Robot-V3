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

	private static readonly int s_rangeDataID = Shader.PropertyToID("_RangeData");
	private static readonly int s_rangeGridID = Shader.PropertyToID("_RangeGrid");
	private static readonly int s_rangeColorID = Shader.PropertyToID("_RangeColor");
	private static readonly int s_aoeColorID = Shader.PropertyToID("_AoEColor");
	private static readonly int s_targetColorID = Shader.PropertyToID("_TargetColor");
	private static readonly int s_visionTypeID = Shader.PropertyToID("_VisionType");

	private static RangeOverlay s_instance;

	private MeshFilter m_meshFilter;
	private MeshRenderer m_renderer;
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

	public static void SetVisionPreview ( IEnumerable<Tile> _tiles, NeuronalMembraneEquipmentData.VisionTypes _visionType )
	{
		RangeOverlay overlay = GetOrCreate();
		if (overlay == null)
			return;

		overlay.m_visionType = _visionType;
		overlay.Replace(overlay.m_visionTiles, _tiles);
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
		m_renderer.shadowCastingMode = ShadowCastingMode.Off;
		m_renderer.receiveShadows = false;
		m_renderer.lightProbeUsage = LightProbeUsage.Off;
		m_renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		m_renderer.enabled = false;

		m_propertyBlock = new MaterialPropertyBlock();
		m_propertyBlock.SetTexture(s_rangeDataID, m_rangeData);
		m_propertyBlock.SetVector(s_rangeGridID, new Vector4(_width, _height, Tile.innerRadius, Tile.outerRadius));

		m_isRangeHidden = PlayerController.Instance != null && PlayerController.Instance.AreInteractableOutlinesHidden;
	}

	private static Mesh BuildGridQuad ( int _width, int _height )
	{
		float minX = -Tile.innerRadius - Tile.outerRadius;
		float maxX = (_width - 0.5f) * Tile.innerRadius * 2f + Tile.innerRadius + Tile.outerRadius;
		float minZ = -Tile.outerRadius * 2f;
		float maxZ = (_height - 1) * Tile.outerRadius * 1.5f + Tile.outerRadius * 2f;

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
	}

	private void Redraw ()
	{
		m_isDirty = false;
		System.Array.Clear(m_rangeBytes, 0, m_rangeBytes.Length);

		bool hasPreview = m_previewTiles.Count > 0;
		List<Tile> rangeTiles = hasPreview ? m_previewTiles : m_isRangeHidden ? null : m_rangeTiles;

		bool isVisible = WriteLayer(rangeTiles, k_rangeChannel, true);
		isVisible |= WriteLayer(m_aoeTiles, k_aoeChannel, false);
		isVisible |= WriteLayer(m_targetTiles, k_targetChannel, false);
		isVisible |= WriteLayer(m_isRangeHidden ? null : m_visionTiles, k_visionChannel, false);

		if (isVisible)
		{
			m_rangeData.SetPixelData(m_rangeBytes, 0);
			m_rangeData.Apply(false);
			m_propertyBlock.SetColor(s_rangeColorID, hasPreview ? m_previewColor : m_rangeColor);
			m_propertyBlock.SetColor(s_aoeColorID, m_aoeColor);
			m_propertyBlock.SetColor(s_targetColorID, m_targetColor);
			m_propertyBlock.SetFloat(s_visionTypeID, (int)m_visionType);
			m_renderer.SetPropertyBlock(m_propertyBlock);
		}

		m_renderer.enabled = isVisible;
	}

	private bool WriteLayer ( List<Tile> _tiles, int _channel, bool _isFloorOnly )
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

			m_rangeBytes[index] = byte.MaxValue;
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
			s_instance = null;

		if (m_rangeData != null)
			Destroy(m_rangeData);

		if (m_meshFilter != null && m_meshFilter.sharedMesh != null)
			Destroy(m_meshFilter.sharedMesh);
	}
}
