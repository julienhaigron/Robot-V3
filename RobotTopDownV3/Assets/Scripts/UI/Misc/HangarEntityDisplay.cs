using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class HangarEntityDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, DamagedSlotDisplay> m_mainComponentSlots;
	[SerializeField] private DamagedSlotDisplay[] m_subSlots;
	[SerializeField] private BaseButton m_selectBtn;
	[SerializeField] private BaseButton m_configBtn;
	[SerializeField] private Image m_toggleBGImg;
	[SerializeField] private Image m_toggleImg;

	[SerializeField] private Color m_selectedMainColor;
	[SerializeField] private Color m_selectedSubColor;
	[SerializeField] private Color m_unselectedMainColor;
	[SerializeField] private Color m_unselectedSubColor;

	private EntitySavedData m_savedData;
	private int m_index;
	private bool m_isSelected;

	[System.Serializable]
	public class SubDamagedSlotContainer
	{
		public List<DamagedSlotDisplay> slots = new();
	}

	private void Awake ()
	{
		m_selectBtn.onClick += OnClickSelect;
		m_configBtn.onClick += OnClickConfig;
	}

	public void Init ( EntitySavedData _data, int _index, bool _isSelected )
	{
		m_savedData = _data;
		m_index = _index;
		m_nameTMP.text = _data.name;
		m_isSelected = _isSelected;

		bool hasBrokenSub = _data.auxiliar != null && _data.auxiliar.Length > 0 && _data.auxiliar.Any(e => e.isDamaged);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Frame], _data.frame, hasBrokenSub);
		hasBrokenSub = _data.chipsets != null && _data.chipsets.Length > 0 && _data.chipsets.Any(e => e.isDamaged);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Brain], _data.brain, hasBrokenSub);
		hasBrokenSub = _data.arms != null && _data.arms.Length > 0 && _data.arms.Any(e => e.isDamaged);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane], _data.neuronalMembrane, hasBrokenSub);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Reactor], _data.reactor, false);

		List<GameDatas.PlayerSave.Component> subComponents = _data.GetAllSubEquipments();
		int count = 0;
		for (int i = 0; count < m_subSlots.Length; i++)
		{
			if (count >= subComponents.Count || subComponents.Count <= i)
			{
				m_subSlots[count].Hide();
				count++;
			}
			else if (!subComponents[i].isDamaged)
				continue;
			else
			{
				m_subSlots[count].Init(subComponents[i].GetData<EntityEquipmentData>().icon, subComponents[i].isDamaged, false);
				count++;
			}
		}

		RefreshToggleVisual(true);
	}

	private void RefreshToggleVisual ( bool _isInstant )
	{
		if (_isInstant)
		{
			(m_selectBtn.transform as RectTransform).DOAnchorPosX(m_isSelected ? -7.5f : 7.5f, 0f);
			m_toggleBGImg.color = m_isSelected ? m_selectedSubColor : m_unselectedSubColor;
			m_toggleImg.color = m_isSelected ? m_selectedMainColor : m_unselectedMainColor;
			return;
		}
		else
		{
			(m_selectBtn.transform as RectTransform).DOAnchorPosX(m_isSelected ? -7.5f : 7.5f, .5f).SetEase(Ease.OutExpo);
			m_toggleBGImg.DOColor(m_isSelected ? m_selectedSubColor : m_unselectedSubColor, .5f).SetEase(Ease.OutExpo);
			m_toggleImg.DOColor(m_isSelected ? m_selectedMainColor : m_unselectedMainColor, .5f).SetEase(Ease.OutExpo);
		}
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
		if (!m_isSelected && m_savedData.CanAddToSquad())
		{
			m_isSelected = true;
			GameDatas.current.currentPlayerSave.squadUnitsIndex.Add(m_index);
		}
		else if (m_isSelected)
		{
			m_isSelected = false;
			GameDatas.current.currentPlayerSave.squadUnitsIndex.Remove(m_index);
		}

		HubManager.Instance.RefreshSquadEntities();
		UIManager.Instance.GetPanel<HangarPanel>().RefreshTexts();

		RefreshToggleVisual(false);
	}

	private void OnClickConfig ()
	{
		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_savedData, false);
	}

	private static void InitComponentSlot ( DamagedSlotDisplay _slot, GameDatas.PlayerSave.Component _component, bool hasDamagedSub )
	{
		EntityEquipmentData componentData = _component == null ? null : _component.GetData<EntityEquipmentData>();

		if (componentData == null)
			_slot.Hide();
		else
			_slot.Init(componentData.icon, _component.isDamaged, hasDamagedSub);
	}
}
