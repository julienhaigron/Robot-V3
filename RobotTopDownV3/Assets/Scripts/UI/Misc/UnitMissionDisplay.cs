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
	[SerializeField] private BaseButton m_selectBtn;
	[SerializeField] private GameObject m_selectGO;
	[SerializeField] private GameObject m_isDamagedGO;

	private int m_index;
	public int Index => m_index;
	private EntitySavedData m_data;
	public EntitySavedData Data => m_data;
	private bool m_isSelected;

	private void Awake ()
	{
		m_openConfigPanelBtn.onClick += OnClickOpenConfigPanel;
		m_selectBtn.onClick += OnClickSelect;
	}

	public void Init(EntitySavedData _data, int _index, bool _isSelected )
	{
		m_data = _data;
		m_index = _index;

		m_nameTMP.text = _data.name;
		m_isSelected = _isSelected;
		m_selectGO.SetActive(_isSelected);
		m_isDamagedGO.SetActive(_data != null && _data.IsDamaged());
	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
	}

	private void OnClickSelect ()
	{
		if (UIManager.Instance.currentPanel is MissionPanel or TournamentPanel)
			return;

		if (!m_isSelected && m_data.CanAddToSquad())
		{
			m_isSelected = true;
			GameDatas.current.currentPlayerSave.squadUnitsIndex.Add(m_index);
		}
		else if (m_isSelected)
		{
			m_isSelected = false;
			GameDatas.current.currentPlayerSave.squadUnitsIndex.Remove(m_index);
		}
		m_selectGO.SetActive(m_isSelected);

		HubManager.Instance.RefreshSquadEntities();
		UIManager.Instance.GetPanel<HangarPanel>().RefreshTexts();

	}

	private void OnClickOpenConfigPanel ()
	{
		bool isInMissionPanel = UIManager.Instance.currentPanel is MissionPanel or TournamentPanel;
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_data, isInMissionPanel);
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		onAnyUnitHovered?.Invoke(this);
	}
}
