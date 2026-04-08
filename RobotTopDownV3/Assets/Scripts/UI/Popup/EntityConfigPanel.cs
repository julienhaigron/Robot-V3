using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntityConfigPanel : AUIPanel
{

	[SerializeField] private TextMeshProUGUI m_texte;
	[SerializeField] private BaseButton m_closeBtn;
	[SerializeField] private BaseButton m_upgradeHangarBtn;

	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, ComponentSlot> componentSlotDictionary;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, ComponentDisplayGrid> gridDictionary;

	private EntitySavedData m_entityData;

	private void Awake ()
	{
		m_closeBtn.onClick += OnClickClose;
		m_upgradeHangarBtn.onClick += OnClickOpenUpgradePopup;

		componentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemAdded += item => m_entityData.frame = item.SavedData;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemRemoved += item => m_entityData.frame = null;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemAdded += item => m_entityData.reactor = item.SavedData;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemRemoved += item => m_entityData.reactor = null;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemAdded += item => m_entityData.brain = item.SavedData;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemRemoved += item => m_entityData.brain = null;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemAdded += item => m_entityData.neuronalMembrane = item.SavedData;
		componentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemRemoved += item => m_entityData.neuronalMembrane = null;

		foreach (ComponentDisplayGrid grid in gridDictionary.Values)
		{
			grid.onItemAdded += item => GameDatas.current.currentPlayerSave.AddEquipmentToInventory(item.ComponentData);
			grid.onItemRemoved += item => GameDatas.current.currentPlayerSave.RemoveEquipmentFromInventory(item.SavedData);
		}
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
		RefreshVisual();
	}

	public void Init ( EntitySavedData _entity )
	{
		m_entityData = _entity;
		m_texte.text = _entity.name;

		componentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].Init(gridDictionary[EntityEquipmentData.EquipmentType.Frame], _entity, _entity.frame
			, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Frame, ComponentDisplay.DisplayMode.Hangar);
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].Init(gridDictionary[EntityEquipmentData.EquipmentType.Reactor], _entity, _entity.reactor
			, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Reactor, ComponentDisplay.DisplayMode.Hangar);
		componentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].Init(gridDictionary[EntityEquipmentData.EquipmentType.Brain], _entity, _entity.brain
			, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Brain, ComponentDisplay.DisplayMode.Hangar);
		componentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].Init(gridDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane], _entity, _entity.neuronalMembrane
			, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.NeuronalMembrane, ComponentDisplay.DisplayMode.Hangar);

		gridDictionary[EntityEquipmentData.EquipmentType.Frame].Init(componentSlotDictionary[EntityEquipmentData.EquipmentType.Frame], _entity
			, null, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Frame, ComponentDisplay.DisplayMode.Hangar);
		gridDictionary[EntityEquipmentData.EquipmentType.Reactor].Init(componentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor], _entity
			, null, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Reactor, ComponentDisplay.DisplayMode.Hangar);
		gridDictionary[EntityEquipmentData.EquipmentType.Brain].Init(componentSlotDictionary[EntityEquipmentData.EquipmentType.Brain], _entity
			, null, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.Brain, ComponentDisplay.DisplayMode.Hangar);
		gridDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].Init(componentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane], _entity
			, null, item => item.GetData<EntityEquipmentData>().GetEquipmentType() == EntityEquipmentData.EquipmentType.NeuronalMembrane, ComponentDisplay.DisplayMode.Hangar);
	}

	private void OnClickClose ()
	{
		foreach (ComponentDisplayGrid grid in gridDictionary.Values)
			grid.Cleanup();
		foreach (ComponentSlot slot in componentSlotDictionary.Values)
			slot.Cleanup();
		
		UIManager.Instance.OpenPanel<HangarPanel>();
	}

	private void OnClickOpenUpgradePopup ()
	{
		UIManager.Instance.OpenPopup<StructureUpgradePopup>().Init(StructureUpgradePopup.StructureType.Hangar);
	}

	private void RefreshVisual ()
	{
		
	}

}
