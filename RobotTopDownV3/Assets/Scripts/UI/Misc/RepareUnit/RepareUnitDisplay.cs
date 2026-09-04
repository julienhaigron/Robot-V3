using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;


public class RepareUnitDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public static System.Action<RepareUnitDisplay> onDisplayHovered;

	[SerializeField] private CanvasGroup m_canvasGroup;
	[SerializeField] private Image m_icon;
	[SerializeField] private Image m_corpIcon;
	[SerializeField] private TextMeshProUGUI m_descriptionTMP;
	[SerializeField] private GameObject m_damagedGO;

	private EntitySavedData m_savedData;
	public EntitySavedData SavedData => m_savedData;

	public RepareUnitContainer CurrentContainer;

	private float m_lastClickTime;
	private bool m_isEmpty;

	public void Init ( EntitySavedData _unitSavedData, bool _isEmpty )
	{
		m_isEmpty = _isEmpty;
		m_savedData = _unitSavedData;
		if (_unitSavedData != null)
		{
			/*m_componentTypeIcon.sprite = GameAssets.current.ui.componentIcons[m_componentData.GetEquipmentType()];
			m_corpIcon.sprite = GameAssets.current.ui.corporationsIcons[m_componentData.faction];*/
			if(_unitSavedData.FrameData != null)
				m_icon.sprite = _unitSavedData.FrameData.icon;
			m_damagedGO.SetActive(_unitSavedData.IsDamaged());
		}
		else
			m_damagedGO.SetActive(false);

		if (m_isEmpty)
			return;

		m_corpIcon.gameObject.SetActive(true);
		m_descriptionTMP.gameObject.SetActive(true);
		m_descriptionTMP.text = "Réparer Composant";
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
		if (m_isEmpty)
			return;

		transform.position = eventData.position;
	}

	public void OnEndDrag ( PointerEventData eventData )
	{
		if (m_isEmpty)
			return;

		m_canvasGroup.blocksRaycasts = true;

		if (transform.parent == UIManager.Instance.TopLayer)
		{
			ReturnToOrigin();
		}
	}

	public void OnPointerClick ( PointerEventData eventData )
	{
		if (m_isEmpty)
			return;

		if (Time.time - m_lastClickTime < GameConfig.current.ui.doubleClickDelay)
		{
			OnDoubleClick();
		}

		m_lastClickTime = Time.time;
	}

	private void OnDoubleClick ()
	{
		if (m_savedData == null)
			return;

		if (CurrentContainer != null && CurrentContainer.LinkedContainer != null && CurrentContainer.LinkedContainer.IsValid(this))
		{
			CurrentContainer.RemoveFromOrigin(this);
			CurrentContainer.LinkedContainer.RegisterInteraction(this);
		}
		else if (CurrentContainer != null && CurrentContainer.LinkedContainer == null && UIManager.Instance.currentPanel is RepairStationPanel repairPanel)
		{
			RepareUnitContainer appropriateContainer = repairPanel.GetFreeContainer();
			if (appropriateContainer == null)
				return;

			appropriateContainer.RemoveFromOrigin(this);
			appropriateContainer.RegisterInteraction(this);
		}
	}

	public void ReturnToOrigin ()
	{
		if (CurrentContainer != null)
		{
			transform.SetParent(CurrentContainer.DisplayParent);
			transform.localPosition = Vector3.zero;

			transform.SetAsFirstSibling();
		}
	}

	#endregion

	#region Tooltip

	public void OnPointerEnter ( PointerEventData eventData )
	{
		onDisplayHovered?.Invoke(this);
		//ToolTipManager.Instance.Show(m_componentData.displayName, null);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		//ToolTipManager.Instance.Hide();
	}

	#endregion

}
