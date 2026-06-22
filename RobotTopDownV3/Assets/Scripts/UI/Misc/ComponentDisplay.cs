using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class ComponentDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public static System.Action<ComponentDisplay> onDisplayHovered;

	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private Image m_componentIcon;
	[SerializeField] private GameObject m_priceBackground;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;
	[SerializeField] private BaseButton m_rerollBtn;

	private EntityEquipmentData m_componentData;
	public EntityEquipmentData ComponentData => m_componentData;
	private GameDatas.PlayerSave.Equipment m_savedData;
	public GameDatas.PlayerSave.Equipment SavedData => m_savedData;

	public ComponentContainer CurrentContainer;

	private float m_lastClickTime;

	public enum DisplayMode { Hangar, RepairStation, RecyclingStation, ShopBuying, ShopSelling }

	public void Init ( EntitySavedData _unitData, GameDatas.PlayerSave.Equipment _componentSavedData, DisplayMode _displayMode )
	{
		m_savedData = _componentSavedData;
		m_componentData = _componentSavedData.GetData<EntityEquipmentData>();
		m_componentIcon.sprite = GameAssets.current.ui.componentIcons[m_componentData.GetEquipmentType()];
		m_corpIcon.sprite = GameAssets.current.ui.corporationsIcons[m_componentData.faction];
		m_icon.sprite = m_componentData.icon;

		if(m_rerollBtn != null)
			m_rerollBtn.onClick = OnClickReroll;

		switch (_displayMode)
		{
			case DisplayMode.Hangar:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(true);
				m_corpIcon.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(false);
				m_descriptionTMP.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(false);
				if (m_rerollBtn != null)
					m_rerollBtn.gameObject.SetActive(false);
				break;
			case DisplayMode.RepairStation:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(false);
				m_corpIcon.gameObject.SetActive(true);
				m_priceBackground.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetPrice().Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				m_descriptionTMP.text = "Réparer Composant";
				if (m_rerollBtn != null)
					m_rerollBtn.gameObject.SetActive(false);
				break;
			case DisplayMode.RecyclingStation:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(false);
				m_corpIcon.gameObject.SetActive(true);
				m_priceBackground.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetSellingPrice().Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				m_descriptionTMP.text = "Recycler Composant";
				if (m_rerollBtn != null)
					m_rerollBtn.gameObject.SetActive(false);
				break;
			case DisplayMode.ShopBuying:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(true);
				m_corpIcon.gameObject.SetActive(true);
				m_priceBackground.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetPrice().Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				m_descriptionTMP.text = "Acheter Composant";
				if (m_rerollBtn != null)
					m_rerollBtn.gameObject.SetActive(true);
				break;
			case DisplayMode.ShopSelling:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(false);
				m_componentIcon.gameObject.SetActive(false);
				m_corpIcon.gameObject.SetActive(false);
				m_priceBackground.gameObject.SetActive(true);
				m_priceBackground.gameObject.SetActive(false);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetSellingPrice().Item2.ToString();
				m_descriptionTMP.gameObject.SetActive(false);
				if (m_rerollBtn != null)
					m_rerollBtn.gameObject.SetActive(false);
				break;
		}
	}

	private void OnClickReroll ()
	{
		UIManager.Instance.GetPanel<ShopPanel>().RerollItem(this);
	}

	#region Interactions
	public void OnBeginDrag ( PointerEventData eventData )
	{
		transform.SetParent(UIManager.Instance.TopLayer);
		transform.SetAsLastSibling();

		m_canvasGroup.blocksRaycasts = false;
	}

	public void OnDrag ( PointerEventData eventData )
	{
		transform.position = eventData.position;
	}

	public void OnEndDrag ( PointerEventData eventData )
	{
		m_canvasGroup.blocksRaycasts = true;

		if (transform.parent == UIManager.Instance.TopLayer)
		{
			ReturnToOrigin();
		}
	}

	public void OnPointerClick ( PointerEventData eventData )
	{
		if (Time.time - m_lastClickTime < GameConfig.current.ui.doubleClickDelay)
		{
			OnDoubleClick();
		}

		m_lastClickTime = Time.time;
	}

	private void OnDoubleClick ()
	{
		if (m_componentData == null)
			return;

		if (CurrentContainer != null && CurrentContainer.LinkedContainer != null && CurrentContainer.LinkedContainer.IsValid(this))
		{
			CurrentContainer.RemoveFromOrigin(this);
			CurrentContainer.LinkedContainer.RegisterInteraction(this);
		}
		else if(CurrentContainer != null && CurrentContainer.LinkedContainer == null && UIManager.Instance.currentPanel is EntityConfigPanel entityConfigPanel)
		{
			ComponentContainer appropriateContainer = entityConfigPanel.GetFreeContainer(m_componentData.GetEquipmentType());
			if (appropriateContainer == null)
				return;

			appropriateContainer.RemoveFromOrigin(this);
			appropriateContainer.LinkedContainer.RegisterInteraction(this);
		}
		else if(CurrentContainer != null && CurrentContainer.LinkedContainer == null && UIManager.Instance.currentPanel is RecyclePanel recyclePanel)
		{
			ComponentContainer appropriateContainer = recyclePanel.GetFreeContainer();
			if (appropriateContainer == null)
				return;

			appropriateContainer.RemoveFromOrigin(this);
			appropriateContainer.LinkedContainer.RegisterInteraction(this);
		}
		else if(CurrentContainer != null && CurrentContainer.LinkedContainer == null && UIManager.Instance.currentPanel is RepairStationPanel repairPanel)
		{
			ComponentContainer appropriateContainer = repairPanel.GetFreeContainer();
			if (appropriateContainer == null)
				return;

			appropriateContainer.RemoveFromOrigin(this);
			appropriateContainer.LinkedContainer.RegisterInteraction(this);
		}
	}

	public void ReturnToOrigin ()
	{
		if (CurrentContainer != null)
		{
			transform.SetParent(CurrentContainer.DisplayParent);
			transform.localPosition = Vector3.zero;
		}
	}

	#endregion

	#region Tooltip

	public void OnPointerEnter ( PointerEventData eventData )
	{ 
		onDisplayHovered?.Invoke(this);
		ToolTipManager.Instance.Show(m_componentData.displayName, null);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		ToolTipManager.Instance.Hide();
	}

	#endregion

}
