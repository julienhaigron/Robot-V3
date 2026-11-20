using System.Collections.Generic;
using UnityEngine;

public abstract class GroundOrderDisplay : PoolElement
{
	private const float c_groundHeight = .04f;
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

	private Mesh m_mesh;
	private readonly List<Vector3> m_vertices = new();
	private readonly List<Vector2> m_uvs = new();
	private readonly List<Vector2> m_stripData = new();
	private readonly List<Color> m_colors = new();
	private readonly List<int> m_triangles = new();
	protected readonly List<Vector3> m_points = new();

	private Color m_color = Color.white;
	private bool m_isGhost;

	protected float HeadLength => m_headLength;
	protected Color MainColor => Faded(m_color);

	private void Awake ()
	{
		m_mesh = new Mesh { name = name };
		m_mesh.MarkDynamic();
		m_stripFilter.sharedMesh = m_mesh;
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();
		Destroy(m_mesh);
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
		return new Vector3(_position.x, _position.y + c_groundHeight, _position.z);
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
		m_vertices.Clear();
		m_uvs.Clear();
		m_stripData.Clear();
		m_colors.Clear();
		m_triangles.Clear();

		Transform space = m_stripFilter.transform;
		Color color = QualitySettings.activeColorSpace == ColorSpace.Linear ? m_color.linear : m_color;
		Vector2 stripData = new(m_stripHalfWidth, m_chevrons);
		int last = m_points.Count - 1;
		float along = 0f;
		for (int i = 0; i <= last; i++)
		{
			if (i > 0)
				along += Vector3.Distance(m_points[i - 1], m_points[i]);

			Vector3 tangent = m_points[Mathf.Min(i + 1, last)] - m_points[Mathf.Max(i - 1, 0)];
			Vector3 side = new Vector3(tangent.z, 0f, -tangent.x).normalized * m_stripHalfWidth;
			Vector3 point = OnGround(m_points[i]);
			m_vertices.Add(space.InverseTransformPoint(point + side));
			m_vertices.Add(space.InverseTransformPoint(point - side));
			m_uvs.Add(new Vector2(along, 1f));
			m_uvs.Add(new Vector2(along, -1f));
			m_stripData.Add(stripData);
			m_stripData.Add(stripData);
			m_colors.Add(color);
			m_colors.Add(color);

			if (i == 0)
				continue;

			int vertex = i * 2;
			m_triangles.Add(vertex - 2);
			m_triangles.Add(vertex);
			m_triangles.Add(vertex - 1);
			m_triangles.Add(vertex - 1);
			m_triangles.Add(vertex);
			m_triangles.Add(vertex + 1);
		}

		m_mesh.Clear();
		m_mesh.SetVertices(m_vertices);
		m_mesh.SetUVs(0, m_uvs);
		m_mesh.SetUVs(1, m_stripData);
		m_mesh.SetColors(m_colors);
		m_mesh.SetTriangles(m_triangles, 0);
		m_mesh.RecalculateBounds();
	}

	protected void ClearStrip ()
	{
		m_points.Clear();
		m_mesh.Clear();
	}
}
