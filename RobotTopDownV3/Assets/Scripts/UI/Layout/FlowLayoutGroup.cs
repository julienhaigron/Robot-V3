using UnityEngine;
using UnityEngine.UI;

public class FlowLayoutGroup : LayoutGroup
{
	[SerializeField] private float m_horizontalSpacing = 6f;
	[SerializeField] private float m_verticalSpacing = 6f;

	public override void CalculateLayoutInputHorizontal ()
	{
		base.CalculateLayoutInputHorizontal();

		float widest = 0f;
		foreach (RectTransform child in rectChildren)
			widest = Mathf.Max(widest, LayoutUtility.GetMinWidth(child));
		SetLayoutInputForAxis(padding.horizontal + widest, padding.horizontal + widest, -1f, 0);
	}

	public override void CalculateLayoutInputVertical ()
	{
		float height = Arrange(false);
		SetLayoutInputForAxis(height, height, -1f, 1);
	}

	public override void SetLayoutHorizontal ()
	{
		Arrange(true);
	}

	public override void SetLayoutVertical ()
	{
		Arrange(true);
	}

	private float Arrange ( bool _apply )
	{
		float width = rectTransform.rect.width - padding.horizontal;
		float x = 0f;
		float y = 0f;
		float rowHeight = 0f;

		foreach (RectTransform child in rectChildren)
		{
			float childWidth = Mathf.Min(LayoutUtility.GetPreferredWidth(child), width);
			float childHeight = LayoutUtility.GetPreferredHeight(child);
			if (x > 0f && x + childWidth > width)
			{
				x = 0f;
				y += rowHeight + m_verticalSpacing;
				rowHeight = 0f;
			}

			if (_apply)
			{
				SetChildAlongAxis(child, 0, padding.left + x, childWidth);
				SetChildAlongAxis(child, 1, padding.top + y, childHeight);
			}

			x += childWidth + m_horizontalSpacing;
			rowHeight = Mathf.Max(rowHeight, childHeight);
		}

		return padding.vertical + y + rowHeight;
	}
}
