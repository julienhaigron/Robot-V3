using UnityEngine;
using UnityEngine.UI;

public class ChargeGaugeGraphic : MaskableGraphic
{
	public enum Shape { Ring, Bar }

	private const float c_maxStepAngle = .1f;

	[SerializeField] private Shape m_shape;
	[SerializeField, Min(.5f)] private float m_thickness = 3.5f;
	[SerializeField, Min(0f)] private float m_gap = 2f;
	[SerializeField] private Color m_emptyColor = new(.17f, .19f, .31f, 1f);
	[SerializeField] private Color m_chargeColor = new(.91f, .7f, .23f, 1f);
	[SerializeField] private Color m_partialColor = new(.48f, .37f, .13f, 1f);
	[SerializeField] private Color m_activeColor = new(.18f, .9f, 1f, 1f);

	private int m_segments = 1;
	private int m_filled;
	private float m_partial;
	private bool m_isActive;

	public void Show ( SpecialAbilityState _ability, int _absoluteTick )
	{
		int remaining = _ability.GetRemainingActiveTicks(_absoluteTick);
		m_isActive = remaining > 0;
		m_segments = Mathf.Max(1, m_isActive ? _ability.WindowLength : _ability.data.abilityChargeMax);
		m_filled = m_isActive ? remaining : _ability.Charges;
		m_partial = m_isActive || _ability.IsFull ? 0f : (float)_ability.PointsTowardNextCharge / _ability.data.abilityRatio;
		SetVerticesDirty();
	}

	protected override void OnPopulateMesh ( VertexHelper _vh )
	{
		_vh.Clear();
		Rect rect = GetPixelAdjustedRect();
		Color fillColor = m_isActive ? m_activeColor : m_chargeColor;

		for (int i = 0; i < m_segments; i++)
		{
			if (i < m_filled)
				AddSegment(_vh, rect, i, 0f, 1f, fillColor);
			else if (i == m_filled && m_partial > 0f)
			{
				AddSegment(_vh, rect, i, 0f, m_partial, m_partialColor);
				AddSegment(_vh, rect, i, m_partial, 1f, m_emptyColor);
			}
			else
				AddSegment(_vh, rect, i, 0f, 1f, m_emptyColor);
		}
	}

	private void AddSegment ( VertexHelper _vh, Rect _rect, int _index, float _from, float _to, Color _color )
	{
		if (m_shape == Shape.Bar)
		{
			float width = (_rect.width - m_gap * (m_segments - 1)) / m_segments;
			float x = _rect.xMin + _index * (width + m_gap);
			AddQuad(_vh, new Vector2(x + _from * width, _rect.yMin), new Vector2(x + _to * width, _rect.yMin)
				, new Vector2(x + _to * width, _rect.yMax), new Vector2(x + _from * width, _rect.yMax), _color);
			return;
		}

		float outer = Mathf.Min(_rect.width, _rect.height) * .5f;
		float inner = Mathf.Max(0f, outer - m_thickness);
		float gapAngle = m_segments > 1 ? m_gap / outer : 0f;
		float segmentAngle = (Mathf.PI * 2f - gapAngle * m_segments) / m_segments;
		float start = Mathf.PI * .5f - gapAngle * .5f - _index * (segmentAngle + gapAngle);
		float span = (_to - _from) * segmentAngle;
		int steps = Mathf.Max(1, Mathf.CeilToInt(span / c_maxStepAngle));

		for (int s = 0; s < steps; s++)
		{
			float a0 = start - (_from * segmentAngle + span * s / steps);
			float a1 = start - (_from * segmentAngle + span * (s + 1) / steps);
			Vector2 d0 = new(Mathf.Cos(a0), Mathf.Sin(a0));
			Vector2 d1 = new(Mathf.Cos(a1), Mathf.Sin(a1));
			AddQuad(_vh, _rect.center + d0 * inner, _rect.center + d0 * outer, _rect.center + d1 * outer, _rect.center + d1 * inner, _color);
		}
	}

	private void AddQuad ( VertexHelper _vh, Vector2 _a, Vector2 _b, Vector2 _c, Vector2 _d, Color _color )
	{
		Color32 vertexColor = _color * color;
		int index = _vh.currentVertCount;
		_vh.AddVert(_a, vertexColor, Vector2.zero);
		_vh.AddVert(_b, vertexColor, Vector2.zero);
		_vh.AddVert(_c, vertexColor, Vector2.zero);
		_vh.AddVert(_d, vertexColor, Vector2.zero);
		_vh.AddTriangle(index, index + 1, index + 2);
		_vh.AddTriangle(index, index + 2, index + 3);
	}
}
