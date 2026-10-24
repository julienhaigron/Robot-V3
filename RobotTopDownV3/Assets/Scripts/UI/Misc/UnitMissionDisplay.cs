using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class UnitMissionDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public static System.Action<UnitMissionDisplay> onAnyUnitHovered;
	public static System.Action<UnitMissionDisplay> onAnySquadToggled;

	[SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private Image m_mainIcon;
	[SerializeField] private Image m_toggleBGImg;
	[SerializeField] private Image m_toggleImg;
	[SerializeField] private BaseButton m_selectBtn;
	[SerializeField] private BaseButton m_openConfigPanelBtn;
	[SerializeField] private BaseButton m_toggleSquadBtn;
	[SerializeField] private GameObject m_toggleSquadGO;
	[SerializeField] private GameObject m_isDamagedGO;
	[SerializeField] private GameObject m_hoveredGO;

	[SerializeField] private Color m_selectedMainColor = new(0.039215688f, 1f, 0f, 1f);
	[SerializeField] private Color m_selectedSubColor = new(0.4745098f, 0.6784314f, 0.27058825f, 0.9019608f);
	[SerializeField] private Color m_unselectedMainColor = new(1f, 0f, 0.039215688f, 1f);
	[SerializeField] private Color m_unselectedSubColor = new(0.6784314f, 0.27058825f, 0.30588236f, 0.9019608f);

	private int m_index;
	public int Index => m_index;
	private EntitySavedData m_data;
	public EntitySavedData Data => m_data;
	private bool m_isInSquad;
	public bool IsInSquad => m_isInSquad;

	private void Awake ()
	{
		m_openConfigPanelBtn.onClick += OnClickOpenConfigPanel;
		m_selectBtn.onClick += OnClickSelect;
		m_toggleSquadBtn.onClick += OnClickToggleSquad;
	}

	//display every unit of the player inventory, instantiate missing displays if the player owns more units than there are displays
	public static void RefreshPlayerUnits ( List<UnitMissionDisplay> _displays )
	{
		if (_displays == null || _displays.Count == 0)
			return;

		GameDatas.PlayerSave playerSave = GameDatas.current.currentPlayerSave;

		while (_displays.Count < playerSave.allBuiltUnits.Count)
		{
			UnitMissionDisplay lastDisplay = _displays[_displays.Count - 1];
			UnitMissionDisplay newDisplay = Instantiate(lastDisplay, lastDisplay.transform.parent);
			newDisplay.transform.SetSiblingIndex(lastDisplay.transform.GetSiblingIndex() + 1);
			_displays.Add(newDisplay);
		}

		List<int> unitIndices = playerSave.GetUnitIndicesSquadFirst();
		for (int i = 0; i < _displays.Count; i++)
		{
			if (i < unitIndices.Count)
			{
				int unitIndex = unitIndices[i];
				_displays[i].Show();
				_displays[i].Init(playerSave.allBuiltUnits[unitIndex], unitIndex, playerSave.squadUnitsIndex.Contains(unitIndex));
			}
			else
				_displays[i].Hide();
		}
	}

	public void Init ( EntitySavedData _data, int _index, bool _isInSquad, bool _canToggleSquad = true )
	{
		m_data = _data;
		m_index = _index;

		m_nameTMP.text = _data.name;
		m_isInSquad = _isInSquad;
		m_isDamagedGO.SetActive(_data != null && _data.IsDamaged());

		m_toggleSquadGO.SetActive(_canToggleSquad);
		RefreshToggleInteractability();
		RefreshToggleVisual(true);
	}

	public void RefreshToggleInteractability ()
	{
		if (m_data == null)
			return;

		m_toggleSquadBtn.SetInteractability(m_isInSquad || m_data.CanAddToSquad());
	}

	private void RefreshToggleVisual ( bool _isInstant )
	{
		RectTransform toggleRect = m_toggleSquadBtn.transform as RectTransform;
		toggleRect.DOKill();
		m_toggleBGImg.DOKill();
		m_toggleImg.DOKill();

		float toggleX = m_isInSquad ? -7.5f : 7.5f;
		Color bgColor = m_isInSquad ? m_selectedSubColor : m_unselectedSubColor;
		Color toggleColor = m_isInSquad ? m_selectedMainColor : m_unselectedMainColor;

		if (_isInstant)
		{
			toggleRect.anchoredPosition = new Vector2(toggleX, toggleRect.anchoredPosition.y);
			m_toggleBGImg.color = bgColor;
			m_toggleImg.color = toggleColor;
		}
		else
		{
			toggleRect.DOAnchorPosX(toggleX, .3f).SetEase(Ease.OutExpo);
			m_toggleBGImg.DOColor(bgColor, .3f).SetEase(Ease.OutExpo);
			m_toggleImg.DOColor(toggleColor, .3f).SetEase(Ease.OutExpo);
		}
	}

	public void Show ()
	{
		SetHovered(false);
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		SetHovered(false);
		gameObject.SetActive(false);
	}

	//clicking on the unit focuses it in the details panel (needed when there is no hover, ex: touch)
	private void OnClickSelect ()
	{
		onAnyUnitHovered?.Invoke(this);
	}

	private void OnClickToggleSquad ()
	{
		if (m_isInSquad)
		{
			m_isInSquad = false;
			GameDatas.current.currentPlayerSave.squadUnitsIndex.Remove(m_index);
		}
		else if (m_data.CanAddToSquad())
		{
			m_isInSquad = true;
			GameDatas.current.currentPlayerSave.squadUnitsIndex.Add(m_index);
		}
		else
			return;

		RefreshToggleVisual(false);
		HubManager.Instance.RefreshSquadEntities();
		onAnySquadToggled?.Invoke(this);
	}

	private void OnClickOpenConfigPanel ()
	{
		bool isInMissionPanel = UIManager.Instance.currentPanel is MissionPanel or TournamentPanel;
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_data, isInMissionPanel);
	}

	public void OnPointerEnter ( PointerEventData eventData )
	{
		SetHovered(true);
		onAnyUnitHovered?.Invoke(this);
	}

	public void OnPointerExit ( PointerEventData eventData )
	{
		SetHovered(false);
	}

	public void SetHovered ( bool _isHovered )
	{
		if (m_hoveredGO != null)
			m_hoveredGO.SetActive(_isHovered && UIManager.Instance.currentPanel is MissionPanel or TournamentPanel);
	}
}
