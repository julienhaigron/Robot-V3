using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityComponentConfigPanel : AUIPanel
{
	private static GameDatas.PlayerSave.Component GetFrameSubComponentAt ( EntitySavedData _data, int _slotIndex )
	{
		FrameEquipmentData frameData = _data == null || _data.frame == null ? null : _data.frame.GetData<FrameEquipmentData>();
		if (frameData == null || _data.auxiliar == null)
			return null;

		bool isArmourSlot = _slotIndex < frameData.armouringSlotAvailable;
		EntityEquipmentData.EquipmentType wantedType = isArmourSlot ? EntityEquipmentData.EquipmentType.Armor : EntityEquipmentData.EquipmentType.Occultor;
		int wantedRank = isArmourSlot ? _slotIndex : _slotIndex - frameData.armouringSlotAvailable;

		int rank = 0;
		foreach (GameDatas.PlayerSave.Component component in _data.auxiliar)
		{
			if (component == null || !component.TryGetData(out EntityEquipmentData data) || data.GetEquipmentType() != wantedType)
				continue;

			if (rank == wantedRank)
				return component;

			rank++;
		}

		return null;
	}

	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private ComponentSlot[] m_slots;
	[SerializeField] private ComponentDisplayGrid m_subPartGrid;
	[SerializeField] private BaseButton m_upgradeHangarBtn;

	private EntitySavedData m_currentEntity;
	private GameDatas.PlayerSave.Component m_currentEquipment; 

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		m_upgradeHangarBtn.onClick += OnClickOpenUpgradePopup;

		m_subPartGrid.onItemAdded += ( container, item ) => GameDatas.current.currentPlayerSave.AddEquipmentToInventory(item.SavedData);
		m_subPartGrid.onItemRemoved += ( container, item ) => GameDatas.current.currentPlayerSave.RemoveEquipmentFromInventory(item.SavedData);

		foreach(ComponentSlot slot in m_slots)
		{
			slot.onItemAdded += OnItemAddedOnSlot;
			slot.onItemRemoved += OnItemRemovedOnSlot;
		}
	}

	public void Init(EntitySavedData _data, GameDatas.PlayerSave.Component _equipmentData )
	{
		m_currentEntity = _data;
		m_currentEquipment = _equipmentData;

		switch (_equipmentData.GetData<EntityEquipmentData>().GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				FrameEquipmentData chassisData = _data.frame == null ? null : _data.frame.GetData<FrameEquipmentData>();
				if (chassisData == null)
					break;

				
				m_subPartGrid.Init(m_slots[0], _data, _data.frame
					, item => item != null && item.TryGetData(out EntityEquipmentData _data) && (_data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Armor || _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Occultor)
					, ComponentDisplay.DisplayMode.Hangar);

				for (int i = 0; i < m_slots.Length; i++)
				{
					if (chassisData.armouringSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, GetFrameSubComponentAt(_data, i)
							, item => item != null && item.TryGetData(out EntityEquipmentData _data) && (_data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Armor)
							, ComponentDisplay.DisplayMode.Hangar);
					}
					else if(chassisData.armouringSlotAvailable + chassisData.occultorSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, GetFrameSubComponentAt(_data, i)
							, item => item != null && item.TryGetData(out EntityEquipmentData _data) && (_data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Occultor)
							, ComponentDisplay.DisplayMode.Hangar);
					}
					else
						m_slots[i].gameObject.SetActive(false);
				}
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				BrainEquipmentData brainData = _data.brain == null ? null : _data.brain.GetData<BrainEquipmentData>();
				if (brainData == null)
					break;


				m_subPartGrid.Init(m_slots[0], _data, _data.brain
					, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset
					, ComponentDisplay.DisplayMode.Hangar);

				for (int i = 0; i < m_slots.Length; i++)
				{
					if (brainData.chipsetSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, _data.chipsets == null || _data.chipsets.Length <= i ? null : _data.chipsets[i]
							, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset
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
				NeuronalMembraneEquipmentData neuronalMembraneData = _data.neuronalMembrane == null ? null : _data.neuronalMembrane.GetData<NeuronalMembraneEquipmentData>();
				if (neuronalMembraneData == null)
					break;


				m_subPartGrid.Init(m_slots[0], _data, _data.neuronalMembrane
					, item => item != null && item.TryGetData(out EntityEquipmentData _data) && (_data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Weapon || _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Tool)
					, ComponentDisplay.DisplayMode.Hangar);

				for (int i = 0; i < m_slots.Length; i++)
				{
					if (neuronalMembraneData.equipmentSlotAvailable > i)
					{
						m_slots[i].gameObject.SetActive(true);
						m_slots[i].Init(m_subPartGrid, _data, _data.arms == null || _data.arms.Length <= i ? null : _data.arms[i]
							, item => item != null && item.TryGetData(out EntityEquipmentData _data) && (_data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Weapon || _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Tool)
							, ComponentDisplay.DisplayMode.Hangar);
					}
					else
						m_slots[i].gameObject.SetActive(false);
				}
				break;
		}

		
	}

	private void OnItemAddedOnSlot(ComponentContainer _container, ComponentDisplay _display )
	{
		switch (m_currentEquipment.GetData<EntityEquipmentData>().GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Component> newArray = m_currentEntity.auxiliar.ToList();
				newArray.Add(_display.SavedData);
				m_currentEntity.auxiliar = newArray.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Component> newArray2 = m_currentEntity.chipsets.ToList();
				newArray2.Add(_display.SavedData);
				m_currentEntity.chipsets = newArray2.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Component> newArray3 = m_currentEntity.arms.ToList();
				newArray3.Add(_display.SavedData);
				m_currentEntity.arms = newArray3.ToArray();
				break;
		}
	}

	private void OnItemRemovedOnSlot ( ComponentContainer _container, ComponentDisplay _display )
	{
		switch (m_currentEquipment.GetData<EntityEquipmentData>().GetEquipmentType())
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Component> newArray = m_currentEntity.auxiliar.ToList();
				newArray.Remove(_display.SavedData);
				m_currentEntity.auxiliar = newArray.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Component> newArray2 = m_currentEntity.chipsets.ToList();
				newArray2.Remove(_display.SavedData);
				m_currentEntity.chipsets = newArray2.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Component> newArray3 = m_currentEntity.arms.ToList();
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

		UIManager.Instance.OpenPanel<EntityConfigPanel>().Init(m_currentEntity, false);
	}

	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Hangar);
	}
}
