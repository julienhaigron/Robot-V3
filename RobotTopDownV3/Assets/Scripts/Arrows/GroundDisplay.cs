using System.Collections.Generic;
using UnityEngine;

public abstract class GroundDisplay : PoolElement
{
	private const float c_cornerCut = .25f;
	private const int c_cornerSegments = 4;
	protected const float c_stripEndInHead = .73f;
	private static readonly int s_opacityID = Shader.PropertyToID("_Opacity");

	[Header("Strip")]
	[SerializeField] private MeshFilter m_stripFilter;
	[SerializeField] private MeshRenderer m_stripRenderer;
	[SerializeField] private Material m_material;
	[SerializeField] private Material m_ghostMaterial;
	[SerializeField] private float m_stripHalfWidth = .165f;
	[SerializeField, Range(0f, 1f)] private float m_chevrons = 1f;

	[Header("Head")]
	[SerializeField] private SpriteRenderer m_head;
	[SerializeField] private float m_headLength = .3f;

	protected readonly List<Vector3> m_points = new();

	private Color m_color = Color.white;
	private bool m_isGhost;

	protected GroundStrip Strip { get; private set; }
	protected MeshRenderer StripRenderer => m_stripRenderer;
	protected SpriteRenderer Head => m_head;
	protected float StripHalfWidth => m_stripHalfWidth;
	protected float HeadLength => m_headLength;
	protected Color MainColor => Faded(m_color);

	protected virtual void Awake ()
	{
		Strip = new GroundStrip(m_stripFilter);
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();
		Strip?.Release();
	}

	protected void SetStyle ( Color _color, bool _isGhost )
	{
		m_color = _color;
		m_isGhost = _isGhost;
		m_stripRenderer.sharedMaterial = _isGhost ? m_ghostMaterial : m_material;
	}

	protected Color Faded ( Color _color )
	{
		if (m_isGhost)
			_color.a *= m_ghostMaterial.GetFloat(s_opacityID);
		return _color;
	}

	protected static Vector3 OnGround ( Vector3 _position )
	{
		return GroundStrip.OnGround(_position);
	}

	protected static bool IsOnGrid ( Entity _entity )
	{
		GridManager grid = GridManager.Instance;
		int tileID = _entity.Displacement.Coordinates.ID;
		return grid != null && grid.Tiles != null && tileID >= 0 && tileID < grid.Tiles.Length;
	}

	protected static void PlaceFlat ( SpriteRenderer _renderer, Vector3 _position, Vector3 _direction, Color _color )
	{
		_renderer.enabled = true;
		_renderer.color = _color;
		_renderer.transform.SetPositionAndRotation(OnGround(_position), Quaternion.LookRotation(Vector3.down, _direction));
	}

	protected void ShowHead ( Vector3 _tip, Vector3 _direction )
	{
		PlaceFlat(m_head, _tip, _direction, MainColor);
	}

	protected void HideHead ()
	{
		m_head.enabled = false;
	}

	protected void SetFilletedPath ( IReadOnlyList<Vector3> _corners, float _startTrim, float _endTrim )
	{
		int last = _corners.Count - 1;
		m_points.Clear();
		m_points.Add(Vector3.MoveTowards(_corners[0], _corners[1], _startTrim));
		for (int i = 1; i < last; i++)
		{
			Vector3 corner = _corners[i];
			Vector3 entry = Vector3.MoveTowards(corner, _corners[i - 1], c_cornerCut);
			Vector3 exit = Vector3.MoveTowards(corner, _corners[i + 1], c_cornerCut);
			for (int step = 0; step <= c_cornerSegments; step++)
			{
				float t = step / (float)c_cornerSegments;
				m_points.Add(Vector3.Lerp(Vector3.Lerp(entry, corner, t), Vector3.Lerp(corner, exit, t), t));
			}
		}
		m_points.Add(Vector3.MoveTowards(_corners[last], _corners[last - 1], _endTrim));
	}

	protected void DrawStrip ()
	{
		Strip.Clear();
		Strip.Add(m_points, m_stripHalfWidth, m_chevrons, m_color);
		Strip.Apply();
	}

	protected void ClearStrip ()
	{
		m_points.Clear();
		Strip.Clear();
		Strip.Apply();
	}
}
