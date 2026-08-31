using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HorizontalLayoutGroup dans lequel chaque enfant occupe un "slot" de largeur identique,
/// le bloc de slots utilises restant aligne selon le ChildAlignment (centre par defaut).
///
/// Le HorizontalLayoutGroup standard ne sait pas faire ca :
///  - ChildForceExpandWidth = true  -> les enfants s'etalent sur toute la largeur, leurs
///    positions bougent des qu'un enfant est desactive.
///  - ChildForceExpandWidth = false -> les enfants sont colles les uns aux autres, l'ecart
///    depend de leur largeur propre.
/// Ici la largeur de slot est constante, donc l'ecart entre elements ne change jamais :
/// seul le bloc entier se recentre quand le nombre d'enfants actifs varie.
///
/// ChildForceExpandWidth est ignore sur l'axe horizontal (il irait contre le principe des
/// slots fixes). L'axe vertical se comporte exactement comme un HorizontalLayoutGroup.
/// </summary>
[AddComponentMenu("Layout/Uniform Horizontal Layout Group", 151)]
public class UniformHorizontalLayoutGroup : HorizontalLayoutGroup
{
	public enum SlotWidthMode
	{
		/// <summary>Largeur du slot = largeur de l'enfant le plus large.</summary>
		WidestChild = 0,
		/// <summary>Largeur du slot fixee a la main.</summary>
		Fixed = 1,
		/// <summary>La largeur du parent est divisee en SlotCount slots (les slots vides restent reserves visuellement au centrage).</summary>
		SplitParent = 2,
	}

	[SerializeField] private SlotWidthMode m_slotWidthMode = SlotWidthMode.WidestChild;
	[SerializeField] private float m_slotWidth = 100f;
	[SerializeField] private int m_slotCount = 8;
	[SerializeField] private bool m_shrinkSlotsToFit = true;

	public SlotWidthMode slotWidthMode { get { return m_slotWidthMode; } set { SetProperty(ref m_slotWidthMode, value); } }
	public float slotWidth { get { return m_slotWidth; } set { SetProperty(ref m_slotWidth, value); } }
	public int slotCount { get { return m_slotCount; } set { SetProperty(ref m_slotCount, value); } }
	public bool shrinkSlotsToFit { get { return m_shrinkSlotsToFit; } set { SetProperty(ref m_shrinkSlotsToFit, value); } }

	public override void CalculateLayoutInputHorizontal ()
	{
		// Remplit rectChildren (enfants actifs, hors LayoutElement.ignoreLayout).
		base.CalculateLayoutInputHorizontal();

		int count = rectChildren.Count;

		if (count == 0)
		{
			SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1, 0);
			return;
		}

		float preferred = padding.horizontal + GetContentWidth(GetSlotWidth(), count);
		float min = m_shrinkSlotsToFit ? padding.horizontal + (count - 1) * spacing : preferred;

		SetLayoutInputForAxis(min, preferred, -1, 0);
	}

	public override void SetLayoutHorizontal ()
	{
		int count = rectChildren.Count;

		if (count == 0)
			return;

		float slot = GetSlotWidth();
		float available = rectTransform.rect.size.x - padding.horizontal;

		if (m_shrinkSlotsToFit && GetContentWidth(slot, count) > available)
			slot = Mathf.Max(0f, (available - (count - 1) * spacing) / count);

		float pos = GetStartOffset(0, GetContentWidth(slot, count));
		float alignmentInSlot = GetAlignmentOnAxis(0);

		for (int i = 0; i < count; i++)
		{
			RectTransform child = rectChildren[reverseArrangement ? count - 1 - i : i];
			float scaleFactor = childScaleWidth ? child.localScale.x : 1f;

			if (childControlWidth)
				SetChildAlongAxisWithScale(child, 0, pos, slot, scaleFactor);
			else
				SetChildAlongAxisWithScale(child, 0, pos + (slot - child.sizeDelta.x * scaleFactor) * alignmentInSlot, scaleFactor);

			pos += slot + spacing;
		}
	}

	private float GetContentWidth ( float _slotWidth, int _count )
	{
		return _count * _slotWidth + (_count - 1) * spacing;
	}

	private float GetSlotWidth ()
	{
		switch (m_slotWidthMode)
		{
			case SlotWidthMode.Fixed:
				return Mathf.Max(0f, m_slotWidth);

			// Attention : ce mode lit la largeur du RectTransform. A n'utiliser que si ce
			// GameObject a une largeur propre (ancres / sizeDelta), pas s'il est lui-meme
			// dimensionne par un layout parent.
			case SlotWidthMode.SplitParent:
			{
				int slots = Mathf.Max(1, m_slotCount);
				float available = rectTransform.rect.size.x - padding.horizontal - (slots - 1) * spacing;
				return Mathf.Max(0f, available / slots);
			}

			default:
			{
				float widest = 0f;

				for (int i = 0; i < rectChildren.Count; i++)
				{
					RectTransform child = rectChildren[i];
					widest = Mathf.Max(widest, childControlWidth ? LayoutUtility.GetPreferredSize(child, 0) : child.sizeDelta.x);
				}

				return widest;
			}
		}
	}
}
