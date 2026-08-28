using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EndLevelEntityDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_nameTMP;
    [SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, DamagedSlotDisplay> m_mainComponentSlots;
    [SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, SubDamagedSlotContainer> m_subComponentSlots;

    [System.Serializable]
    public class SubDamagedSlotContainer
    {
        public List<DamagedSlotDisplay> slots = new();
    }

    public void Init( EntitySavedData _data )
	{
        m_nameTMP.text = _data.name;

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
            if (i >= _data.auxiliar.Length)
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


}
