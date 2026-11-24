using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RepareUnitDisplay : GestureDisplay
{
	public static System.Action<RepareUnitDisplay> onDisplayHovered;

	[Header("Row (inventory)")]
	[SerializeField] private GameObject m_rowRoot;
	[SerializeField] private Image m_rowIcon;
	[SerializeField] private TextMeshProUGUI m_rowNameTMP;
	[SerializeField] private Image m_factionIcon;
	[SerializeField] private GameObject m_outOfServiceGO;
	[SerializeField] private Image[] m_damagedIcons;
	[SerializeField] private TextMeshProUGUI m_moreDamagedTMP;
	[SerializeField] private TextMeshProUGUI m_necessaryPriceTMP;
	[SerializeField] private TextMeshProUGUI m_fullPriceTMP;

	[Header("Compact (slot, drag)")]
	[SerializeField] private GameObject m_compactRoot;
	[SerializeField] private Image m_compactIcon;
	[SerializeField] private TextMeshProUGUI m_compactNameTMP;
	[SerializeField] private GameObject m_compactDamagedGO;
	[SerializeField] private Vector2 m_compactSize = new(100f, 100f);

	[SerializeField] private Color m_damagedIconColor = new(.91f, .27f, .24f);

	private EntitySavedData m_savedData;
	public EntitySavedData SavedData => m_savedData;

	public override object Content => m_savedData;
	public override bool IsPlaceholder => m_savedData == null;

	public void Init ( EntitySavedData _unitSavedData )
	{
		m_savedData = _unitSavedData;
		Refresh();
		ApplyLayout();
	}

	private void Refresh ()
	{
		bool hasUnit = m_savedData != null;
		UnitRepairInfo info = hasUnit ? new UnitRepairInfo(m_savedData) : default;
		int damagedCount = hasUnit ? info.parts.Count : 0;

		Sprite icon = hasUnit && m_savedData.FrameData != null ? m_savedData.FrameData.icon : null;
		m_rowIcon.sprite = icon;
		m_compactIcon.sprite = icon;
		m_rowNameTMP.text = hasUnit ? m_savedData.name : string.Empty;
		m_compactNameTMP.text = m_rowNameTMP.text;
		m_factionIcon.sprite = hasUnit ? GameAssets.current.ui.corporationsIcons[m_savedData.GetDominentFaction(out _)] : null;
		m_outOfServiceGO.SetActive(hasUnit && info.IsOutOfService);
		m_compactDamagedGO.SetActive(damagedCount > 0);

		for (int i = 0; i < m_damagedIcons.Length; i++)
		{
			bool isShown = i < damagedCount;
			m_damagedIcons[i].gameObject.SetActive(isShown);
			if (isShown)
			{
				m_damagedIcons[i].sprite = GameAssets.current.ui.componentIcons[info.parts[i].Type];
				m_damagedIcons[i].color = m_damagedIconColor;
			}
		}

		int hiddenCount = damagedCount - m_damagedIcons.Length;
		m_moreDamagedTMP.gameObject.SetActive(hiddenCount > 0);
		m_moreDamagedTMP.text = "+" + hiddenCount;

		m_necessaryPriceTMP.text = hasUnit && info.IsOutOfService ? info.necessaryPrice.ToString() : "—";
		m_fullPriceTMP.text = damagedCount > 0 ? info.fullPrice.ToString() : "—";
	}

	private void OnTransformParentChanged ()
	{
		ApplyLayout();
	}

	private void ApplyLayout ()
	{
		bool isCompact = GetComponentInParent<RepareUnitDisplayGrid>(true) == null;
		m_rowRoot.SetActive(!isCompact);
		m_compactRoot.SetActive(isCompact);
		if (isCompact)
			((RectTransform)transform).sizeDelta = m_compactSize;
	}

	protected override void OnHoverChanged ( bool _isHovered )
	{
		if (_isHovered)
			onDisplayHovered?.Invoke(this);
	}
}
