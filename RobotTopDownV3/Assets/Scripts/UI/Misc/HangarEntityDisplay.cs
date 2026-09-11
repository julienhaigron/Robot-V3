using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HangarEntityDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, DamagedSlotDisplay> m_mainComponentSlots;
    [SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, SubDamagedSlotContainer> m_subComponentSlots;
    [SerializeField] private BaseButton m_selectBtn;
    [SerializeField] private GameObject m_selectGO;

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
    }

	public void Init( EntitySavedData _data, int _index, bool _isSelected )
	{
        m_savedData = _data;
        m_index = _index;
        m_nameTMP.text = _data.name;
        m_isSelected = _isSelected;
        m_selectGO.SetActive(_isSelected);

        InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Frame], _data.frame);
        InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Brain], _data.brain);
        InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Reactor], _data.reactor);
        InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane], _data.neuronalMembrane);

        for (int i = 0; i < m_subComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
            if (_data.arms == null || i >= _data.arms.Length)
                m_subComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Hide();
            else
                InitComponentSlot(m_subComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i], _data.arms[i]);
        }

        for (int i = 0; i < m_subComponentSlots[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
            if (_data.auxiliar == null || i >= _data.auxiliar.Length)
                m_subComponentSlots[EntityEquipmentData.EquipmentType.Frame].slots[i].Hide();
            else
                InitComponentSlot(m_subComponentSlots[EntityEquipmentData.EquipmentType.Frame].slots[i], _data.auxiliar[i]);
		}

        for (int i = 0; i < m_subComponentSlots[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
            if (_data.chipsets == null || i >= _data.chipsets.Length)
                m_subComponentSlots[EntityEquipmentData.EquipmentType.Brain].slots[i].Hide();
            else
                InitComponentSlot(m_subComponentSlots[EntityEquipmentData.EquipmentType.Brain].slots[i], _data.chipsets[i]);
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
        m_selectGO.SetActive(m_isSelected);

        HubManager.Instance.RefreshSquadEntities();
        UIManager.Instance.GetPanel<HangarPanel>().RefreshTexts();

    }



	private static void InitComponentSlot ( DamagedSlotDisplay _slot, GameDatas.PlayerSave.Component _component )
	{
		EntityEquipmentData componentData = _component == null ? null : _component.GetData<EntityEquipmentData>();

		if (componentData == null)
			_slot.Hide();
		else
			_slot.Init(componentData.icon, _component.isDamaged);
	}
}
