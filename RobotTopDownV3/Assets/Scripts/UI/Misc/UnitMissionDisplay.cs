using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitMissionDisplay : MonoBehaviour, IPointerEnterHandler
{
	public static System.Action<UnitMissionDisplay> onAnyUnitHovered;

	[SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private Image m_mainIcon;
    [SerializeField] private Image m_subIcon;
	[SerializeField] private BaseButton m_openConfigPanelBtn;

	private EntitySavedData m_data;
	public EntitySavedData Data => m_data;

	private void Awake ()
	{
		m_openConfigPanelBtn.onClick += OnClickOpenConfigPanel;
	}

	public void Init(EntitySavedData _data)
	{
		m_data = _data;

		m_nameTMP.text = _data.name;
       /* m_mainIcon.sprite = _data.icon;
        m_subIcon.sprite = */
    }

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}

	private void OnClickOpenConfigPanel ()
	{
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_data, true);
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		onAnyUnitHovered?.Invoke(this);
	}
}
