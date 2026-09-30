using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EndLevelEntityDisplay : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI m_nameTMP;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, DamagedSlotDisplay> m_mainComponentSlots;

	[System.Serializable]
	public class SubDamagedSlotContainer
	{
		public List<DamagedSlotDisplay> slots = new();
	}

	public void Init ( EntitySavedData _data )
	{
		m_nameTMP.text = _data.name;

		bool hasBrokenSub = _data.auxiliar != null && _data.auxiliar.Length > 0 && _data.auxiliar.Any(e => e.isDamaged);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Frame], _data.frame, hasBrokenSub);
		hasBrokenSub = _data.chipsets != null && _data.chipsets.Length > 0 && _data.chipsets.Any(e => e.isDamaged);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Brain], _data.brain, hasBrokenSub);
		hasBrokenSub = _data.arms != null && _data.arms.Length > 0 && _data.arms.Any(e => e.isDamaged);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.NeuronalMembrane], _data.neuronalMembrane, hasBrokenSub);
		InitComponentSlot(m_mainComponentSlots[EntityEquipmentData.EquipmentType.Reactor], _data.reactor, false);

	}

	public void Show ()
	{
		gameObject.SetActive(true);
	}

	public void Hide ()
	{
		gameObject.SetActive(false);
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
