using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;

public class EntityConfigPanel : AUIPanel
{

	[SerializeField] private TMP_InputField m_unitNameInputField;

	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, ComponentSlot> m_mainComponentSlotDictionary;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, SubSlotContainer> m_subComponentSlotDictionary;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	[Title("Inventory")]
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, BaseButton> m_componentTypeFilterBtnDictionary = new();

	[Title("Unit")]
	[SerializeField] private BaseButton m_renameBtn;
	[SerializeField] private StatDisplay[] m_unitStatDisplays;

	private List<EntityEquipmentData.EquipmentType> m_displayedEquipmentTypes = new();
	private EntitySavedData m_entityData;

	private System.Func<GameDatas.PlayerSave.Equipment, bool> InventoryGridPredicate => item => item != null && item.TryGetData(out EntityEquipmentData _data) 
		&& m_displayedEquipmentTypes.Contains(_data.GetEquipmentType());

	[System.Serializable]
	public class SubSlotContainer
	{
		public List<ComponentSlot> slots = new();
	}

	private void Awake ()
	{
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemAdded += (container, item) => m_entityData.frame = item.SavedData;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemRemoved += ( container, item ) => m_entityData.frame = null;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemAdded += ( container, item ) => m_entityData.reactor = item.SavedData;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemRemoved += ( container, item ) => m_entityData.reactor = null;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemAdded += ( container, item ) => m_entityData.brain = item.SavedData;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemRemoved += ( container, item ) => m_entityData.brain = null;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemAdded += ( container, item ) => m_entityData.neuronalMembrane = item.SavedData;
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemRemoved += ( container, item ) => m_entityData.neuronalMembrane = null;

		for(int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i];
			slot.onItemAdded += (ComponentContainer container, ComponentDisplay display) => OnItemAddedOnSlot(display, EntityEquipmentData.EquipmentType.Frame);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSlot(display, EntityEquipmentData.EquipmentType.Frame);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i];
			slot.onItemAdded += (ComponentContainer container,  ComponentDisplay display ) => OnItemAddedOnSlot(display, EntityEquipmentData.EquipmentType.Brain);
			slot.onItemRemoved += (ComponentContainer container,  ComponentDisplay display ) => OnItemRemovedOnSlot(display, EntityEquipmentData.EquipmentType.Brain);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i];
			slot.onItemAdded += (ComponentContainer container,  ComponentDisplay display ) => OnItemAddedOnSlot(display, EntityEquipmentData.EquipmentType.NeuronalMembrane);
			slot.onItemRemoved += (ComponentContainer container,  ComponentDisplay display ) => OnItemRemovedOnSlot(display, EntityEquipmentData.EquipmentType.NeuronalMembrane);
		}
		

		m_inventoryGrid.onItemAdded += ( container, item ) => GameDatas.current.currentPlayerSave.AddEquipmentToInventory(item.ComponentData);
		m_inventoryGrid.onItemRemoved += ( container, item ) => GameDatas.current.currentPlayerSave.RemoveEquipmentFromInventory(item.SavedData);

		foreach (KeyValuePair<EntityEquipmentData.EquipmentType, BaseButton> pair in m_componentTypeFilterBtnDictionary)
			pair.Value.onClick = () => OnToggleComponentType(pair.Key);
		m_displayedEquipmentTypes.AddRange(m_componentTypeFilterBtnDictionary.Keys);

		m_unitNameInputField.onSubmit.AddListener((string s) => OnInputFieldChange());
		m_unitNameInputField.onEndTextSelection.AddListener((string s, int i, int j) => OnInputFieldChange());
		m_renameBtn.onClick += OnClickRenameBtn;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
	}

	protected override void OnHideFinished ()
	{
		m_inventoryGrid.Cleanup();
		foreach (ComponentSlot slot in m_mainComponentSlotDictionary.Values)
			slot.Cleanup();

		foreach (SubSlotContainer slotContainer in m_subComponentSlotDictionary.Values)
			foreach (ComponentSlot slot in slotContainer.slots)
				slot.Cleanup();
			
		base.OnHideFinished();
	}

	public void Init ( EntitySavedData _entity )
	{
		m_entityData = _entity;
		m_unitNameInputField.text = _entity.name;

		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].Init(m_inventoryGrid, _entity, _entity.frame
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Frame, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].Init(m_inventoryGrid, _entity, _entity.brain
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Brain, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].Init(m_inventoryGrid, _entity, _entity.neuronalMembrane
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.NeuronalMembrane, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].Init(m_inventoryGrid, _entity, _entity.reactor
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Reactor, ComponentDisplay.DisplayMode.Hangar);

		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
			if (_entity.FrameData.auxiliarSlotAvailable > i)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, _entity, _entity.auxiliar.Length <= i ? null : _entity.auxiliar[i],
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type) 
					&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
			}
			else
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
			if (_entity.BrainData.chipsetSlotAvailable > i)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, _entity, _entity.chipsets.Length <= i ? null : _entity.chipsets[i],
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Hangar);
			}
			else
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
			if (_entity.NeuronalMembraneData.equipmentSlotAvailable > i)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, _entity, _entity.arms.Length <= i ? null : _entity.arms[i],
					item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
					&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Hangar);
			}
			else
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
		}

		m_inventoryGrid.Init(null, _entity, null, InventoryGridPredicate , ComponentDisplay.DisplayMode.Hangar);
		RefreshVisual();
	}

	public ComponentContainer GetFreeContainer( EntityEquipmentData.EquipmentType _type )
	{
		if (!m_subComponentSlotDictionary.ContainsKey(_type))
			return null;

		foreach(ComponentSlot slot in m_subComponentSlotDictionary[_type].slots)
		{
			if (slot.CurrentDisplay == null)
				return slot;
		}

		return null;
	}

	private void RefreshVisual ()
	{
		//set unit stats
		SerializableDictionary<EntityEquipmentData.StatBonus.StatType, EntityEquipmentData.StatDescription> statsDescriptions = m_entityData.GetStatsDesciptions();
		List<EntityEquipmentData.StatBonus.StatType> keys = statsDescriptions.Keys.ToList();
		for (int i = 0; i < m_unitStatDisplays.Length; i++)
		{
			if (keys.Count <= i)
				m_unitStatDisplays[i].gameObject.SetActive(false);
			else
			{
				m_unitStatDisplays[i].gameObject.SetActive(true);
				m_unitStatDisplays[i].Init(statsDescriptions[keys[i]]);
			}
		}
	}


	#region Callbacks

	private void OnItemAddedOnSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Equipment> newArray = m_entityData.auxiliar.ToList();
				newArray.Add(_display.SavedData);
				m_entityData.auxiliar = newArray.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Equipment> newArray2 = m_entityData.chipsets.ToList();
				newArray2.Add(_display.SavedData);
				m_entityData.chipsets = newArray2.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Equipment> newArray3 = m_entityData.arms.ToList();
				newArray3.Add(_display.SavedData);
				m_entityData.arms = newArray3.ToArray();
				break;
		}
	}

	private void OnItemRemovedOnSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Equipment> newArray = m_entityData.auxiliar.ToList();
				newArray.Remove(_display.SavedData);
				m_entityData.auxiliar = newArray.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Equipment> newArray2 = m_entityData.chipsets.ToList();
				newArray2.Remove(_display.SavedData);
				m_entityData.chipsets = newArray2.ToArray();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Equipment> newArray3 = m_entityData.arms.ToList();
				newArray3.Remove(_display.SavedData);
				m_entityData.arms = newArray3.ToArray();
				break;
		}
	}

	private void OnToggleComponentType ( EntityEquipmentData.EquipmentType _type )
	{
		if (m_displayedEquipmentTypes.Contains(_type))
		{
			m_componentTypeFilterBtnDictionary[_type].Image.color = Color.red;
			m_displayedEquipmentTypes.Remove(_type);
		}
		else
		{
			m_componentTypeFilterBtnDictionary[_type].Image.color = Color.white;
			m_displayedEquipmentTypes.Add(_type);
		}

		m_inventoryGrid.RefreshPredicate(InventoryGridPredicate);
	}

	private void OnInputFieldChange ()
	{
		m_entityData.name = m_unitNameInputField.text;
	}

	private void OnClickRenameBtn ()
	{
		m_unitNameInputField.Select();
	}

	#endregion

}
