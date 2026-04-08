using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityComponentConfigPanel : AUIPanel
{
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private ComponentSlot[] m_slots;
	[SerializeField] private ComponentDisplayGrid m_subPartGrid;
	[SerializeField] private BaseButton m_upgradeHangarBtn;

	private EntitySavedData m_currentEntity;
	private GameDatas.PlayerSave.Equipment m_currentEquipment; 

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		m_upgradeHangarBtn.onClick += OnClickOpenUpgradePopup;

		m_subPartGrid.onItemAdded += item => GameDatas.current.currentPlayerSave.AddEquipmentToInventory(item.ComponentData);
		m_subPartGrid.onItemRemoved += item => GameDatas.current.currentPlayerSave.RemoveEquipmentFromInventory(item.SavedData);

		foreach(ComponentSlot slot in m_slots)
		{
			slot.onItemAdded += OnItemAddedOnSlot;
			slot.onItemRemoved += OnItemRemovedOnSlot;
		}
	}

	public void Init(EntitySavedData _data, GameDatas.PlayerSave.Equipment _equipmentData )
	{
		m_currentEntity = _data;
		m_currentEquipment = _equipmentData;

		switch (_equipmentData.GetData<EntityEquipmentData>().GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				FrameEquipmentData chassisData = GameAssets.current.equipments[_data.frame.dataID] as FrameEquipmentData;
				
				m_subPartGrid.Init(m_slots[0], _data, _data.frame
					, item => (item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Armor || item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Occultor)
					, ComponentDisplay.DisplayMode.Hangar);

				for (int i = 0; i < m_slots.Length; i++)
				{
					if (chassisData.auxiliarSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, _data.auxiliar.Length <= i ? null : _data.auxiliar[i]
							, item => (item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Armor || item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Occultor)
							, ComponentDisplay.DisplayMode.Hangar);
					}
					else
						m_slots[i].gameObject.SetActive(false);
				}
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				BrainEquipmentData brainData = GameAssets.current.equipments[_data.brain.dataID] as BrainEquipmentData;

				m_subPartGrid.Init(m_slots[0], _data, _data.brain
					, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset
					, ComponentDisplay.DisplayMode.Hangar);

				for (int i = 0; i < m_slots.Length; i++)
				{
					if (brainData.chipsetSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, _data.chipsets.Length <= i ? null : _data.chipsets[i]
							, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset
							, ComponentDisplay.DisplayMode.Hangar);
					}
					else
						m_slots[i].gameObject.SetActive(false);
				}
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no sub parts
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				NeuronalMembraneEquipmentData neuronalMembraneData = GameAssets.current.equipments[_data.neuronalMembrane.dataID] as NeuronalMembraneEquipmentData;

				m_subPartGrid.Init(m_slots[0], _data, _data.neuronalMembrane
					, item => (item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Weapon || item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Tool)
					, ComponentDisplay.DisplayMode.Hangar);

				for (int i = 0; i < m_slots.Length; i++)
				{
					if (neuronalMembraneData.equipmentSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, _data.arms.Length <= i ? null : _data.arms[i]
							, item => (item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Weapon || item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Tool)
							, ComponentDisplay.DisplayMode.Hangar);
					}
					else
						m_slots[i].gameObject.SetActive(false);
				}
				break;
		}

		
	}

	private void OnItemAddedOnSlot(ComponentDisplay _display )
	{
		switch (m_currentEquipment.GetData<EntityEquipmentData>().GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Equipment> newArray = m_currentEntity.auxiliar.ToList();
				newArray.Add(_display.SavedData);
				m_currentEntity.auxiliar = newArray.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Equipment> newArray2 = m_currentEntity.chipsets.ToList();
				newArray2.Add(_display.SavedData);
				m_currentEntity.chipsets = newArray2.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Equipment> newArray3 = m_currentEntity.arms.ToList();
				newArray3.Add(_display.SavedData);
				m_currentEntity.arms = newArray3.ToArray();
				break;
		}
	}

	private void OnItemRemovedOnSlot ( ComponentDisplay _display )
	{
		switch (m_currentEquipment.GetData<EntityEquipmentData>().GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Equipment> newArray = m_currentEntity.auxiliar.ToList();
				newArray.Remove(_display.SavedData);
				m_currentEntity.auxiliar = newArray.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Equipment> newArray2 = m_currentEntity.chipsets.ToList();
				newArray2.Remove(_display.SavedData);
				m_currentEntity.chipsets = newArray2.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Equipment> newArray3 = m_currentEntity.arms.ToList();
				newArray3.Remove(_display.SavedData);
				m_currentEntity.arms = newArray3.ToArray();
				break;
		}
	}

	private void OnClickClose ()
	{
		m_subPartGrid.Cleanup();
		foreach (ComponentSlot slot in m_slots)
			slot.Cleanup();

		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_currentEntity);
	}

	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Hangar);
	}
}
