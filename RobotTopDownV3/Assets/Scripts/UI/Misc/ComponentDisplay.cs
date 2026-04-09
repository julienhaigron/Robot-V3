using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class ComponentDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private Image m_icon;
	[SerializeField] private TextMeshProUGUI m_titleTMP;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private Image m_componentIcon;
	[SerializeField] private TextMeshProUGUI m_priceTMP;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;

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

		switch (_displayMode)
		{
			case DisplayMode.Hangar:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(true);
				m_corpIcon.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(false);
				m_descriptionTMP.gameObject.SetActive(false);
				break;
			case DisplayMode.RepairStation:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(false);
				m_corpIcon.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetPrice().Item1.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				m_descriptionTMP.text = "Réparer Composant";
				break;
			case DisplayMode.RecyclingStation:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(false);
				m_corpIcon.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetSellingPrice().Item1.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				m_descriptionTMP.text = "Recycler Composant";
				break;
			case DisplayMode.ShopBuying:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(true);
				m_corpIcon.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetPrice().Item1.ToString();
				m_descriptionTMP.gameObject.SetActive(true);
				m_descriptionTMP.text = "Acheter Composant";
				break;
			case DisplayMode.ShopSelling:
				m_titleTMP.text = m_componentData == null ? null : m_componentData.displayName;
				m_titleTMP.gameObject.SetActive(true);
				m_componentIcon.gameObject.SetActive(true);
				m_corpIcon.gameObject.SetActive(true);
				m_priceTMP.gameObject.SetActive(true);
				m_priceTMP.text = m_componentData == null ? null : m_componentData.GetSellingPrice().Item1.ToString();
				m_descriptionTMP.gameObject.SetActive(false);
				break;
		}

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
		if (CurrentContainer != null && CurrentContainer.LinkedContainer != null && CurrentContainer.LinkedContainer.IsValid(this))
		{
			CurrentContainer.RemoveFromOrigin(this);

			CurrentContainer.LinkedContainer.RegisterInteraction(this);
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

}
