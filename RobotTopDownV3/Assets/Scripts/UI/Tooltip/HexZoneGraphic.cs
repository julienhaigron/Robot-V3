using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class HexZoneGraphic : MaskableGraphic
{
	private const int c_halfColumns = 6;
	private const int c_halfRows = 3;
	private static readonly Vector2Int[] s_directions = { new(1, -1), new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1) };
	private static readonly float s_sqrt3 = Mathf.Sqrt(3f);

	[SerializeField] private Color m_emptyColor = new(1f, 1f, 1f, .05f);
	[SerializeField] private Color m_hitColor = new(1f, .55f, .26f, .8f);
	[SerializeField] private Color m_casterColor = new(.36f, .78f, 1f, 1f);
	[SerializeField] private Color m_targetColor = new(1f, 1f, 1f, .9f);
	[SerializeField, Range(.5f, 1f)] private float m_cellFill = .86f;

	private readonly HashSet<Vector2Int> m_hits = new();
	private readonly List<Vector2Int> m_cells = new();
	private Vector2Int? m_caster;
	private Vector2Int? m_target;

	public void SetZone ( ActionKeyword _keyword )
	{
		m_hits.Clear();
		m_caster = null;
		m_target = null;
		BuildCells();

		switch (_keyword.type)
		{
			case ActionKeywordType.Blast:
				m_caster = new Vector2Int(-5, 0);
				m_target = Vector2Int.zero;
				AddWithin(m_target.Value, 0, _keyword.value);
				break;
			case ActionKeywordType.Pulse:
				m_caster = Vector2Int.zero;
				AddWithin(m_caster.Value, Mathf.Max(0, _keyword.minValue), _keyword.value);
				break;
			case ActionKeywordType.Ray:
				m_caster = new Vector2Int(-c_halfColumns, 0);
				m_target = new Vector2Int(-c_halfColumns + Mathf.Min(_keyword.value, 2 * c_halfColumns - 1), 0);
				foreach (Vector2Int cell in m_cells)
					if (cell.y == 0 && cell.x > m_caster.Value.x && cell.x <= m_target.Value.x)
						m_hits.Add(cell);
				break;
			case ActionKeywordType.Cone:
				m_caster = new Vector2Int(-5, 0);
				foreach (Vector2Int cell in ConeCells(m_caster.Value, 1, Mathf.Min(_keyword.value, 8), !_keyword.isWide))
					if (Distance(cell, m_caster.Value) >= Mathf.Max(1, _keyword.minValue))
						m_hits.Add(cell);
				break;
			case ActionKeywordType.Arc:
				m_caster = new Vector2Int(-2, 0);
				m_target = new Vector2Int(-1, 0);
				float limit = _keyword.isWide ? 60.5f : 30.5f;
				foreach (Vector2Int cell in m_cells)
				{
					int distance = Distance(cell, m_target.Value);
					if (distance >= 1 && distance <= _keyword.value && Angle(m_target.Value, cell) <= limit)
						m_hits.Add(cell);
				}
				break;
			case ActionKeywordType.Chain:
				m_caster = new Vector2Int(-5, 0);
				m_target = new Vector2Int(-1, 0);
				m_hits.Add(m_target.Value);
				Vector2Int[] bounces = { new(1, -1), new(0, 2), new(2, 0) };
				for (int i = 0; i < Mathf.Min(_keyword.value, bounces.Length); i++)
					m_hits.Add(bounces[i]);
				break;
		}

		SetVerticesDirty();
	}

	protected override void OnPopulateMesh ( VertexHelper _vertexHelper )
	{
		_vertexHelper.Clear();
		if (m_cells.Count == 0)
			return;

		Rect rect = GetPixelAdjustedRect();
		float width = s_sqrt3 * (2 * c_halfColumns + 1.5f);
		float height = 1.5f * 2 * c_halfRows + 2f;
		float size = Mathf.Min(rect.width / width, rect.height / height);
		Vector2 origin = new(rect.xMin + size * s_sqrt3 * (c_halfColumns + .5f), rect.center.y);

		foreach (Vector2Int cell in m_cells)
		{
			Color cellColor = m_caster == cell ? m_casterColor : m_target == cell ? m_targetColor : m_hits.Contains(cell) ? m_hitColor : m_emptyColor;
			AddHexagon(_vertexHelper, origin + ToPixel(cell, size), size * m_cellFill, cellColor * color);
		}
	}

	private void BuildCells ()
	{
		m_cells.Clear();
		for (int row = -c_halfRows; row <= c_halfRows; row++)
			for (int column = -c_halfColumns; column <= c_halfColumns; column++)
				m_cells.Add(new Vector2Int(column - (row - (row & 1)) / 2, row));
	}

	private void AddWithin ( Vector2Int _center, int _min, int _max )
	{
		foreach (Vector2Int cell in m_cells)
		{
			int distance = Distance(cell, _center);
			if (distance >= _min && distance <= _max)
				m_hits.Add(cell);
		}
	}

	private static List<Vector2Int> ConeCells ( Vector2Int _from, int _orientation, int _max, bool _isThin )
	{
		int left = (_orientation + 5) % 6;
		int right = (_orientation + 1) % 6;
		Vector2Int origin = _from + s_directions[_orientation];
		List<Vector2Int> result = new() { origin };
		List<Vector2Int> previous = new() { origin };
		HashSet<Vector2Int> seen = new() { origin };
		Vector2Int leftest = origin;
		Vector2Int rightest = origin;

		for (int cursor = 0; cursor < _max - 1; cursor++)
		{
			List<Vector2Int> next = new();
			void Push ( Vector2Int _cell )
			{
				if (seen.Add(_cell))
					next.Add(_cell);
			}

			if (_isThin)
			{
				if (cursor % 2 == 0)
				{
					leftest += s_directions[_orientation];
					rightest += s_directions[_orientation];
				}
				else
				{
					leftest += s_directions[left];
					Push(leftest);
					rightest += s_directions[right];
					Push(rightest);
				}
				foreach (Vector2Int cell in previous)
					Push(cell + s_directions[_orientation]);
			}
			else if (cursor == 0)
			{
				Vector2Int a = previous[0] + s_directions[left];
				Vector2Int c = previous[0] + s_directions[right];
				Push(a);
				Push(previous[0] + s_directions[_orientation]);
				Push(c);
				leftest = a;
				rightest = c;
			}
			else
			{
				Vector2Int leftStep = leftest + s_directions[left];
				Push(leftStep);
				leftest = leftStep + s_directions[left];
				Push(leftest);
				Push(leftStep + s_directions[_orientation]);

				Vector2Int rightStep = rightest + s_directions[right];
				Push(rightStep);
				rightest = rightStep + s_directions[right];
				Push(rightest);
				Push(rightStep + s_directions[_orientation]);

				foreach (Vector2Int cell in previous)
					Push(cell + s_directions[_orientation]);
			}

			result.AddRange(next);
			previous = next;
		}

		return result;
	}

	private static int Distance ( Vector2Int _a, Vector2Int _b )
	{
		int dq = _a.x - _b.x;
		int dr = _a.y - _b.y;
		return (Mathf.Abs(dq) + Mathf.Abs(dq + dr) + Mathf.Abs(dr)) / 2;
	}

	private static float Angle ( Vector2Int _from, Vector2Int _to )
	{
		Vector2 delta = ToPixel(_to, 1f) - ToPixel(_from, 1f);
		return Mathf.Abs(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
	}

	private static Vector2 ToPixel ( Vector2Int _cell, float _size )
	{
		return new Vector2(_size * s_sqrt3 * (_cell.x + _cell.y * .5f), -_size * 1.5f * _cell.y);
	}

	private static void AddHexagon ( VertexHelper _vertexHelper, Vector2 _center, float _radius, Color _color )
	{
		int start = _vertexHelper.currentVertCount;
		_vertexHelper.AddVert(_center, _color, Vector4.zero);
		for (int i = 0; i < 6; i++)
		{
			float angle = Mathf.Deg2Rad * (60f * i - 30f);
			_vertexHelper.AddVert(_center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius, _color, Vector4.zero);
		}
		for (int i = 0; i < 6; i++)
			_vertexHelper.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 6);
	}
}
