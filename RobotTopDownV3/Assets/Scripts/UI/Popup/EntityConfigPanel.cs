using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;

public class EntityConfigPanel : AUIPanel
{


	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, ComponentSlot> m_mainComponentSlotDictionary;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, SubSlotContainer> m_subComponentSlotDictionary;
	[SerializeField] private ComponentFullDisplay m_hoveredComponentFullInfoDisplay;

	[Title("Inventory")]
	[SerializeField] private ComponentDisplayGrid m_inventoryGrid;
	[SerializeField] private SerializableDictionary<EntityEquipmentData.EquipmentType, BaseButton> m_componentTypeFilterBtnDictionary = new();

	[Title("Unit")]
	[SerializeField] private TMP_InputField m_unitNameInputField;
	[SerializeField] private BaseButton m_renameBtn;
	[SerializeField] private ActionButton[] m_actionBtns;
	[SerializeField] private StatDisplay[] m_unitStatDisplays;
	[SerializeField] private Image m_dominentCorpoIcon;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_displayStaticStatsFilter;
	[SerializeField] private EntityEquipmentData.SecondaryStat.StatType[] m_displayConditionalStatsFilter;

	private List<EntityEquipmentData.EquipmentType> m_displayedEquipmentTypes = new();
	private EntitySavedData m_entityData;
	private bool m_doesComeFromMissionPanel;
	public bool DoesComeFromMissionPanel => m_doesComeFromMissionPanel;
	private bool m_isNewUnit = false;

	private System.Func<GameDatas.PlayerSave.Component, bool> InventoryGridPredicate => item => item != null && item.TryGetData(out EntityEquipmentData _data)
		&& m_displayedEquipmentTypes.Contains(_data.GetEquipmentType()) && !item.isDamaged;

	[System.Serializable]
	public class SubSlotContainer
	{
		public List<ComponentSlot> slots = new();
	}

	private void Awake ()
	{
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.Frame);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.Frame);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.Reactor);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.Reactor);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.Brain);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.Brain);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemAdded += ( container, item ) => OnItemAddedOnMainSlot(item, EntityEquipmentData.EquipmentType.NeuronalMembrane);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].onItemRemoved += ( container, item ) => OnItemRemovedOnMainSlot(item, EntityEquipmentData.EquipmentType.NeuronalMembrane);

		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i];
			slot.onItemAdded += ( ComponentContainer container, ComponentDisplay display ) => OnItemAddedOnSubSlot(display, EntityEquipmentData.EquipmentType.Frame);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSubSlot(display, EntityEquipmentData.EquipmentType.Frame);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i];
			slot.onItemAdded += ( ComponentContainer container, ComponentDisplay display ) => OnItemAddedOnSubSlot(display, EntityEquipmentData.EquipmentType.Brain);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSubSlot(display, EntityEquipmentData.EquipmentType.Brain);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
			ComponentSlot slot = m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i];
			slot.onItemAdded += ( ComponentContainer container, ComponentDisplay display ) => OnItemAddedOnSubSlot(display, EntityEquipmentData.EquipmentType.NeuronalMembrane);
			slot.onItemRemoved += ( ComponentContainer container, ComponentDisplay display ) => OnItemRemovedOnSubSlot(display, EntityEquipmentData.EquipmentType.NeuronalMembrane);
		}

		m_inventoryGrid.onItemAdded += ( container, item ) => { GameDatas.current.currentPlayerSave.AddComponentToInventory(item.ComponentData); RefreshVisuals(); };
		m_inventoryGrid.onItemRemoved += ( container, item ) => { GameDatas.current.currentPlayerSave.RemoveEquipmentFromInventory(item.SavedData); RefreshVisuals(); };

		foreach (KeyValuePair<EntityEquipmentData.EquipmentType, BaseButton> pair in m_componentTypeFilterBtnDictionary)
			pair.Value.onClick = () => OnToggleComponentType(pair.Key);
		m_displayedEquipmentTypes.AddRange(m_componentTypeFilterBtnDictionary.Keys);

		m_unitNameInputField.onSubmit.AddListener(( string s ) => OnInputFieldChange());
		m_unitNameInputField.onEndTextSelection.AddListener(( string s, int i, int j ) => OnInputFieldChange());
		m_renameBtn.onClick += OnClickRenameBtn;
	}

	protected override void OnShowStarted ()
	{
		base.OnShowStarted();
	}

	protected override void OnHideFinished ()
	{
		if (m_isNewUnit && m_entityData.IsUnitValid())
			GameDatas.current.currentPlayerSave.AddNewUnit(m_entityData, false);

		m_inventoryGrid.Cleanup();
		foreach (ComponentSlot slot in m_mainComponentSlotDictionary.Values)
			slot.Cleanup();

		foreach (SubSlotContainer slotContainer in m_subComponentSlotDictionary.Values)
			foreach (ComponentSlot slot in slotContainer.slots)
				slot.Cleanup();

		base.OnHideFinished();
	}

	public void InitNewUnit ()
	{
		EntitySavedData newUnit = new();
		newUnit.name = "New Unit";
		//HubManager.Instance.AddEntity(GameDatas.current.currentPlayerSave.AddNewUnit(newUnit, false));
		Init(newUnit, false);
		m_isNewUnit = true;
	}

	public void Init ( EntitySavedData _entity, bool _doesComeFromMissionPanel )
	{
		m_isNewUnit = false;
		m_entityData = _entity;
		m_doesComeFromMissionPanel = _doesComeFromMissionPanel;
		m_unitNameInputField.text = _entity.name;

		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].Init(m_inventoryGrid, _entity, _entity.frame
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Frame, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Frame]);
		
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].Init(m_inventoryGrid, _entity, _entity.brain
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Brain, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Brain]);
		
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].Init(m_inventoryGrid, _entity, _entity.neuronalMembrane
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.NeuronalMembrane, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.NeuronalMembrane]);
		
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].Init(m_inventoryGrid, _entity, _entity.reactor
			, item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Reactor, ComponentDisplay.DisplayMode.Hangar);
		m_mainComponentSlotDictionary[EntityEquipmentData.EquipmentType.Reactor].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Reactor]);

		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
		{
			if (_entity.FrameData != null && _entity.FrameData.armouringSlotAvailable > i)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, _entity, _entity.auxiliar != null && _entity.auxiliar.Length > i
				? _entity.auxiliar[i] : null,
				item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
				&& (type == EntityEquipmentData.EquipmentType.Armor), ComponentDisplay.DisplayMode.Hangar);

				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment.isDamaged);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Armor]);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
			}
			else if(_entity.FrameData != null && _entity.FrameData.armouringSlotAvailable + _entity.FrameData.occultorSlotAvailable > i)
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, _entity, _entity.auxiliar != null && _entity.auxiliar.Length > i
				? _entity.auxiliar[i] : null,
				item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
				&& (type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment.isDamaged);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Occultor]);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
			}
			else
			{
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, _entity, _entity.auxiliar != null && _entity.auxiliar.Length > i
				? _entity.auxiliar[i] : null,
				item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
				&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
				//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Equipment.isDamaged);
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
			}
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
		{
			m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, _entity, _entity.chipsets != null && _entity.chipsets.Length > i
				? _entity.chipsets[i] : null,
				item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Hangar);

			m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Chipset]);
			//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Equipment.isDamaged);
			if (_entity.BrainData != null && _entity.BrainData.chipsetSlotAvailable > i)
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
			else
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
		}
		for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
		{
			m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, _entity, _entity.arms != null && _entity.arms.Length > i
				? _entity.arms[i] : null,
				item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
				&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Hangar);

			m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].SetOutlineColor(GameAssets.current.ui.componentColors[EntityEquipmentData.EquipmentType.Weapon]);
			//m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].SetInteractability(m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Equipment != null && !m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Equipment.isDamaged);
			if (_entity.NeuronalMembraneData != null && _entity.NeuronalMembraneData.equipmentSlotAvailable > i)
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
			else
				m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
		}

		m_inventoryGrid.Init(null, _entity, null, InventoryGridPredicate, ComponentDisplay.DisplayMode.Hangar);
		/*foreach(ComponentDisplay display in m_inventoryGrid.Items)
		{
			if(display.SavedData != null && display.SavedData.TryGetData(out EntityEquipmentData data))
				display.SetOutlineColor(GameAssets.current.ui.componentColors[data.GetEquipmentType()]);
		}*/

		m_hoveredComponentFullInfoDisplay.Init(null);
		RefreshVisuals();
	}

	public ComponentContainer GetFreeContainer ( EntityEquipmentData.EquipmentType _type )
	{
		if (!m_subComponentSlotDictionary.ContainsKey(_type))
			return null;

		foreach (ComponentSlot slot in m_subComponentSlotDictionary[_type].slots)
		{
			if (slot.CurrentDisplay == null)
				return slot;
		}

		return null;
	}

	private void RefreshVisuals ()
	{
		//actions
		List<EntityActionEnumID> actions = new();
		foreach (GameDatas.PlayerSave.Component equipmentData in m_entityData.GetAllEquipments())
		{
			if (equipmentData.TryGetData(out EntityEquipmentData data))
				actions.AddRange(data.knownedActions);
		}
		for (int i = 0; i < m_actionBtns.Length; i++)
		{
			if (actions.Count > i)
			{
				m_actionBtns[i].InitEntityConfigPanelMode(actions[i]);
				m_actionBtns[i].SetVisible(_isVisible: true, _isInstant: true);
			}
			else
				m_actionBtns[i].SetVisible(_isVisible: false, _isInstant: true);
		}

		//set unit stats
		SerializableDictionary<EntityEquipmentData.SecondaryStat.StatType, EntityEquipmentData.StatDescription> statsDescriptions = m_entityData.GetStatsDesciptions();
		List<EntityEquipmentData.SecondaryStat.StatType> keys = statsDescriptions.Keys.ToList();
		foreach (EntityEquipmentData.SecondaryStat.StatType stat in keys.ToArray())
		{
			bool conditionalPredicate = m_displayConditionalStatsFilter.Contains(stat)
				&& ((statsDescriptions[stat].floatValue != 0 && (statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Int || statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Percentage || statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.Cell))
				|| (statsDescriptions[stat].Format == EntityEquipmentData.SecondaryStat.StatTypeFormat.String));
			bool staticPredicate = m_displayStaticStatsFilter.Contains(stat);
			if (!conditionalPredicate && !staticPredicate)
				keys.Remove(stat);
		}
		List<EntityEquipmentData.SecondaryStat.StatType> order = GameConfig.current.ui.statsDisplayOrder.ToList();
		keys.OrderByDescending(e => order.IndexOf(e));
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

		//inventory
		//m_inventoryGrid.RefreshPredicate(InventoryGridPredicate);
		m_inventoryGrid.Cleanup();
		for (int i = 0; i < GameConfig.current.game.maxInventoryCapacity; i++)
		{
			if (GameDatas.current.currentPlayerSave.equipmentInventory.Count > i && InventoryGridPredicate(GameDatas.current.currentPlayerSave.equipmentInventory[i]))
				m_inventoryGrid.CreateNewDisplay(null, GameDatas.current.currentPlayerSave.equipmentInventory[i], ComponentDisplay.DisplayMode.Hangar);
			else
			{
				m_inventoryGrid.CreateNewDisplay(null, null, ComponentDisplay.DisplayMode.Empty);
			}
		}
		EntityEquipmentData.EntityFaction dominentCorpo = m_entityData.GetDominentFaction(out float percentage);
		m_dominentCorpoIcon.sprite = GameAssets.current.ui.corporationsIcons[dominentCorpo];
		m_dominentCorpoIcon.color = GameAssets.current.ui.corporationsColors[dominentCorpo];
	}


	#region Callbacks

	private void OnItemAddedOnSubSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Component> newArray = m_entityData.auxiliar.ToList();
				newArray.Add(_display.SavedData);
				m_entityData.auxiliar = newArray.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
				{
					if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.auxiliar != null && m_entityData.auxiliar.Length > i
						? m_entityData.auxiliar[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor), ComponentDisplay.DisplayMode.Hangar);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else if(m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable + m_entityData.FrameData.occultorSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.auxiliar != null && m_entityData.auxiliar.Length > i
						? m_entityData.auxiliar[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.auxiliar != null && m_entityData.auxiliar.Length > i
						? m_entityData.auxiliar[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
					}
				}

				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Component> newArray2 = m_entityData.chipsets.ToList();
				newArray2.Add(_display.SavedData);
				m_entityData.chipsets = newArray2.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.chipsets != null && m_entityData.chipsets.Length > i
						? m_entityData.chipsets[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Hangar);
					if (m_entityData.BrainData != null && m_entityData.BrainData.chipsetSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
				}

				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Component> newArray3 = m_entityData.arms.ToList();
				newArray3.Add(_display.SavedData);
				m_entityData.arms = newArray3.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.arms != null && m_entityData.arms.Length > i
						? m_entityData.arms[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Hangar);
					if (m_entityData.NeuronalMembraneData != null && m_entityData.NeuronalMembraneData.equipmentSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
				}

				break;
		}
		RefreshVisuals();
	}

	private void OnItemRemovedOnSubSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				List<GameDatas.PlayerSave.Component> newArray = m_entityData.auxiliar.ToList();
				newArray.Remove(_display.SavedData);
				m_entityData.auxiliar = newArray.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots.Count; i++)
				{
					if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.auxiliar != null && m_entityData.auxiliar.Length > i
						? m_entityData.auxiliar[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor), ComponentDisplay.DisplayMode.Hangar);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else if (m_entityData.FrameData != null && m_entityData.FrameData.armouringSlotAvailable + m_entityData.FrameData.occultorSlotAvailable > i)
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.auxiliar != null && m_entityData.auxiliar.Length > i
						? m_entityData.auxiliar[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(true);
					}
					else
					{
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.auxiliar != null && m_entityData.auxiliar.Length > i
						? m_entityData.auxiliar[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Armor || type == EntityEquipmentData.EquipmentType.Occultor), ComponentDisplay.DisplayMode.Hangar);
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots[i].gameObject.SetActive(false);
					}
				}

				break;
			case EntityEquipmentData.EquipmentType.Brain:
				List<GameDatas.PlayerSave.Component> newArray2 = m_entityData.chipsets.ToList();
				newArray2.Remove(_display.SavedData);
				m_entityData.chipsets = newArray2.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.chipsets != null && m_entityData.chipsets.Length > i
						? m_entityData.chipsets[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.GetEquipmentType() == EntityEquipmentData.EquipmentType.Chipset, ComponentDisplay.DisplayMode.Hangar);
					if (m_entityData.BrainData != null && m_entityData.BrainData.chipsetSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots[i].gameObject.SetActive(false);
				}

				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				//no interaction possible
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				List<GameDatas.PlayerSave.Component> newArray3 = m_entityData.arms.ToList();
				newArray3.Remove(_display.SavedData);
				m_entityData.arms = newArray3.ToArray();
				for (int i = 0; i < m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots.Count; i++)
				{
					m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].Init(m_inventoryGrid, m_entityData, m_entityData.arms != null && m_entityData.arms.Length > i
						? m_entityData.arms[i] : null,
						item => item != null && item.TryGetData(out EntityEquipmentData _data) && _data.TryGetEquipmentType(out EntityEquipmentData.EquipmentType type)
						&& (type == EntityEquipmentData.EquipmentType.Weapon || type == EntityEquipmentData.EquipmentType.Tool), ComponentDisplay.DisplayMode.Hangar);
					if (m_entityData.NeuronalMembraneData != null && m_entityData.NeuronalMembraneData.equipmentSlotAvailable > i)
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(true);
					else
						m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots[i].gameObject.SetActive(false);
				}

				break;
		}
		RefreshVisuals();
	}

	private void OnItemAddedOnMainSlot(ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				m_entityData.frame = _display.SavedData;
				RefreshVisuals();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				m_entityData.brain = _display.SavedData;
				RefreshVisuals();
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				m_entityData.neuronalMembrane = _display.SavedData;
				RefreshVisuals();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				m_entityData.reactor = _display.SavedData;
				RefreshVisuals();
				break;
		}
	}

	private void OnItemRemovedOnMainSlot ( ComponentDisplay _display, EntityEquipmentData.EquipmentType _type )
	{
		switch (_type)
		{
			case EntityEquipmentData.EquipmentType.Frame:
				m_entityData.frame = null;
				if(m_entityData.auxiliar != null)
				{
					foreach(GameDatas.PlayerSave.Component eq in m_entityData.auxiliar)
					{
						if(eq != null)
							GameDatas.current.currentPlayerSave.AddEquipmentToInventory(eq);
					}
					m_entityData.auxiliar = new GameDatas.PlayerSave.Component[0];

					foreach (ComponentSlot slot in m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Frame].slots)
						slot.Cleanup();
				}

				RefreshVisuals();
				break;
			case EntityEquipmentData.EquipmentType.Brain:
				m_entityData.brain = null;
				if (m_entityData.chipsets != null)
				{
					foreach (GameDatas.PlayerSave.Component eq in m_entityData.chipsets)
					{
						if (eq != null)
							GameDatas.current.currentPlayerSave.AddEquipmentToInventory(eq);
					}
					m_entityData.chipsets = new GameDatas.PlayerSave.Component[0];
					foreach (ComponentSlot slot in m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.Brain].slots)
						slot.Cleanup();
				}

				RefreshVisuals();
				break;
			case EntityEquipmentData.EquipmentType.NeuronalMembrane:
				m_entityData.neuronalMembrane = null;
				if (m_entityData.arms != null)
				{
					foreach (GameDatas.PlayerSave.Component eq in m_entityData.arms)
					{
						if (eq != null)
							GameDatas.current.currentPlayerSave.AddEquipmentToInventory(eq);
					}
					m_entityData.arms = new GameDatas.PlayerSave.Component[0];
					foreach (ComponentSlot slot in m_subComponentSlotDictionary[EntityEquipmentData.EquipmentType.NeuronalMembrane].slots)
						slot.Cleanup();
				}

				RefreshVisuals();
				break;
			case EntityEquipmentData.EquipmentType.Reactor:
				m_entityData.reactor = null; 
				RefreshVisuals();
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

		RefreshVisuals();
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
