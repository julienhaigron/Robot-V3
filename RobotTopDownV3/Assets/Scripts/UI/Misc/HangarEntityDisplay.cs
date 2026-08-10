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

        m_mainComponentSlots[EntityEquipmentData.EquipmentType.Frame].Init(_data.FrameData.icon, _data.frame.isDamaged);
        m_mainComponentSlots[EntityEquipmentData.EquipmentType.Brain].Init(_data.BrainData.icon, _data.brain.isDamaged);
        m_mainComponentSlots[EntityEquipmentData.EquipmentType.Reactor].Init(_data.ReactorData.icon, _data.reactor.isDamaged);
        m_mainComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].Init(_data.NeuronalMembraneData.icon, _data.neuronalMembrane.isDamaged);

        for (int i = 0; i < m_subComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
            if (i >= _data.arms.Length)
                m_subComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Hide();
            else
                m_subComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(_data.arms[i].GetData<EntityEquipmentData>().icon, _data.arms[i].isDamaged);
        }

        for (int i = 0; i < m_subComponentSlots[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
            if(i >= _data.auxiliar.Length)
                m_subComponentSlots[EntityEquipmentData.EquipmentType.Frame].slots[i].Hide();
            else
                m_subComponentSlots[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(_data.auxiliar[i].GetData<EntityEquipmentData>().icon, _data.auxiliar[i].isDamaged);
		}

        for (int i = 0; i < m_subComponentSlots[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
            if (i >= _data.chipsets.Length)
                m_subComponentSlots[EntityEquipmentData.EquipmentType.Brain].slots[i].Hide();
            else
                m_subComponentSlots[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(_data.chipsets[i].GetData<EntityEquipmentData>().icon, _data.chipsets[i].isDamaged);
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


}
