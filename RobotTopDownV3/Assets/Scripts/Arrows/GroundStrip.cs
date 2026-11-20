using System.Collections.Generic;
using UnityEngine;

public sealed class GroundStrip
{
	public const float c_groundHeight = .04f;
	private const float c_degreesPerSample = 6f;

	private readonly Mesh m_mesh;
	private readonly Transform m_space;
	private readonly List<Vector3> m_vertices = new();
	private readonly List<Vector2> m_uvs = new();
	private readonly List<Vector2> m_stripData = new();
	private readonly List<Color> m_colors = new();
	private readonly List<int> m_triangles = new();
	private readonly List<Vector3> m_arc = new();

	public GroundStrip ( MeshFilter _filter )
	{
		m_space = _filter.transform;
		m_mesh = new Mesh { name = _filter.name };
		m_mesh.MarkDynamic();
		_filter.sharedMesh = m_mesh;
	}

	public static Vector3 OnGround ( Vector3 _position )
	{
		return new Vector3(_position.x, _position.y + c_groundHeight, _position.z);
	}

	public void Release ()
	{
		Object.Destroy(m_mesh);
	}

	public void Clear ()
	{
		m_vertices.Clear();
		m_uvs.Clear();
		m_stripData.Clear();
		m_colors.Clear();
		m_triangles.Clear();
	}

	public void Add ( IReadOnlyList<Vector3> _points, float _halfWidth, float _chevrons, Color _color )
	{
		Color color = QualitySettings.activeColorSpace == ColorSpace.Linear ? _color.linear : _color;
		Vector2 stripData = new(_halfWidth, _chevrons);
		int first = m_vertices.Count;
		int last = _points.Count - 1;
		float along = 0f;
		for (int i = 0; i <= last; i++)
		{
			if (i > 0)
				along += Vector3.Distance(_points[i - 1], _points[i]);

			Vector3 tangent = _points[Mathf.Min(i + 1, last)] - _points[Mathf.Max(i - 1, 0)];
			Vector3 side = new Vector3(tangent.z, 0f, -tangent.x).normalized * _halfWidth;
			Vector3 point = OnGround(_points[i]);
			m_vertices.Add(m_space.InverseTransformPoint(point + side));
			m_vertices.Add(m_space.InverseTransformPoint(point - side));
			m_uvs.Add(new Vector2(along, 1f));
			m_uvs.Add(new Vector2(along, -1f));
			m_stripData.Add(stripData);
			m_stripData.Add(stripData);
			m_colors.Add(color);
			m_colors.Add(color);

			if (i == 0)
				continue;

			int vertex = first + i * 2;
			m_triangles.Add(vertex - 2);
			m_triangles.Add(vertex);
			m_triangles.Add(vertex - 1);
			m_triangles.Add(vertex - 1);
			m_triangles.Add(vertex);
			m_triangles.Add(vertex + 1);
		}
	}

	public void AddArc ( Vector3 _centre, float _radius, float _fromYaw, float _toYaw, float _halfWidth, Color _color )
	{
		int samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(_toYaw - _fromYaw) / c_degreesPerSample));
		m_arc.Clear();
		for (int i = 0; i <= samples; i++)
			m_arc.Add(_centre + Quaternion.Euler(0f, Mathf.Lerp(_fromYaw, _toYaw, i / (float)samples), 0f) * Vector3.forward * _radius);
		Add(m_arc, _halfWidth, 0f, _color);
	}

	public void Apply ()
	{
		m_mesh.Clear();
		m_mesh.SetVertices(m_vertices);
		m_mesh.SetUVs(0, m_uvs);
		m_mesh.SetUVs(1, m_stripData);
		m_mesh.SetColors(m_colors);
		m_mesh.SetTriangles(m_triangles, 0);
		m_mesh.RecalculateBounds();
	}
}
